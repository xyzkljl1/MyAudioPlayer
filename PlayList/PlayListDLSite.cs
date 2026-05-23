using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace MyAudioPlayer.PlayList
{
    internal class PlayListDLSite : PlayListBase
    {
        public class AFileSet
        {
            public string title = "";
            public List<AFile> files = new List<AFile>();
        }

        public class AFile
        {
            public enum FileType
            {
                OTHER,
                WAV,
                MP3,
                FLAC
            };

            public string title = "";
            public FileInfo fileInfo;
            public FileType type = FileType.OTHER;

            public AFile(FileInfo _fileInfo)
            {
                fileInfo = _fileInfo;
                title = fileInfo.Name;
                var ext = fileInfo.Extension.ToLower();
                if (ext == ".wav" || ext == ".wave")
                    type = FileType.WAV;
                else if (ext == ".mp3" || ext == ".mp4" || ext == ".m4a" || ext == ".avi")
                    type = FileType.MP3;
                else if (ext == ".flac")
                    type = FileType.FLAC;
                else
                    type = FileType.OTHER;
            }
        };

        public class Node
        {
            public enum NodeType
            {
                Default,
                DLSite,
                SingleFile
            };

            public string title = "";
            public DirectoryInfo rootRir = new DirectoryInfo(".");
            public string RJ = "";
            public NodeType type = NodeType.Default;
            public bool IsDLSite() { return type == NodeType.DLSite; }
            public List<AFileSet> fileSets = new List<AFileSet>();
            public bool loaded = false;
            public Task<List<AFileSet>>? loadingTask = null;
        };

        private enum ContextTarget
        {
            None,
            WorkList,
            FileTree
        }

        private enum CursorKind
        {
            None,
            Work,
            FileSet,
            File
        }

        public static Regex workNameRegex = new Regex("^[RVBJ]{0,2}[0-9]{3,8}");
        public static Regex seriesNameRegex = new Regex("^S ");
        private const int ScanPublishBatchSize = 500;
        // Z:\ASMR_ReliableR benchmark, 36,891 works / 566,674 files:
        // sequential 1556s, 4-way 667s, 8-way 612s, 16-way 707s, unlimited 858s.
        // Directory scans are IO-bound; 8 kept the disk busy without flooding the thread pool.
        private const int MaxConcurrentFileSetLoads = 8;
        private static readonly SemaphoreSlim FileSetLoadSemaphore = new SemaphoreSlim(MaxConcurrentFileSetLoads);

        private SplitContainer mainControl = new SplitContainer();
        private ListView worksListView = new ListView();
        private TreeView fileTreeView = new TreeView();
        private DirectoryInfo rootDir;
        private string dlServer;
        private DirectoryInfo favDir;
        private List<Node> nodes = new List<Node>();
        private HttpClient httpClient;
        private ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
        private ToolStripItem contextMenuStripItemDel;
        private ContextTarget contextTarget = ContextTarget.None;
        private int displayedWorkIndex = -1;
        private int currentWorkIndex = -1;
        private int currentFileSetIndex = 0;
        private int currentFileIndex = 0;
        private CursorKind currentCursorKind = CursorKind.None;
        private int fileTreeLoadVersion = 0;
        private bool splitterDistanceInitialized = false;
        private event TreeNodeMouseClickEventHandler? mountedDoubleClickHandlers;

        public PlayListDLSite(string _rootDir, MyFileEditEventHandler _begin, MyFileEditEventHandler _end)
        {
            rootDir = new DirectoryInfo(_rootDir);
            dlServer = Config.DLServerAddress;
            favDir = new DirectoryInfo(Config.DLSiteFavDir);
            Title = "DL-" + rootDir.Name;

            mainControl.Dock = DockStyle.Fill;
            mainControl.Orientation = Orientation.Vertical;
            mainControl.Resize += delegate { InitializeSplitterDistance(); };

            worksListView.Dock = DockStyle.Fill;
            worksListView.View = View.Details;
            worksListView.FullRowSelect = true;
            worksListView.HideSelection = false;
            worksListView.MultiSelect = false;
            worksListView.VirtualMode = true;
            worksListView.Columns.Add("Title", 300);
            worksListView.Columns.Add("RJ", 90);
            worksListView.RetrieveVirtualItem += this.WorksListView_RetrieveVirtualItem;
            worksListView.SelectedIndexChanged += this.WorksListView_SelectedIndexChanged;
            worksListView.DoubleClick += this.WorksListView_DoubleClick;
            worksListView.MouseClick += this.WorksListView_MouseClick;
            worksListView.Resize += delegate { ResizeWorkListColumns(); };

            fileTreeView.Dock = DockStyle.Fill;
            fileTreeView.NodeMouseDoubleClick += this.FileTreeView_NodeMouseDoubleClick;
            fileTreeView.NodeMouseClick += this.FileTreeView_NodeMouseClick;

            mainControl.Panel1.Controls.Add(worksListView);
            mainControl.Panel2.Controls.Add(fileTreeView);

            httpClient = new HttpClient();
            contextMenuStrip.Items.Add("Fav");
            contextMenuStrip.Items.Add("DelPart");
            contextMenuStripItemDel = contextMenuStrip.Items.Add("Del");
            contextMenuStrip.ItemClicked += this.ContextMenuClicked;

            MountFileEditEvent(_begin, _end);
            Task.Run(LoadFiles);
        }

        private void InitializeSplitterDistance()
        {
            if (splitterDistanceInitialized || mainControl.Width < 500)
                return;
            mainControl.SplitterDistance = Math.Min(360, mainControl.Width - mainControl.Panel2MinSize - mainControl.SplitterWidth);
            splitterDistanceInitialized = true;
        }

        private void ResizeWorkListColumns()
        {
            if (worksListView.Columns.Count < 2)
                return;
            worksListView.Columns[1].Width = 90;
            worksListView.Columns[0].Width = Math.Max(120, worksListView.ClientSize.Width - worksListView.Columns[1].Width - 8);
        }

        public override Control GetMainControl()
        {
            return mainControl;
        }

        private void WorksListView_RetrieveVirtualItem(object? sender, RetrieveVirtualItemEventArgs e)
        {
            if (!IsValidWorkIndex(e.ItemIndex))
            {
                e.Item = new ListViewItem("");
                return;
            }

            var node = nodes[e.ItemIndex];
            e.Item = new ListViewItem(new[] { node.title, node.RJ });
        }

        private async void WorksListView_SelectedIndexChanged(object? sender, EventArgs e)
        {
            int selectedIndex = GetSelectedWorkIndex();
            if (selectedIndex < 0)
                return;

            await LoadWorkIntoFileTreeAsync(selectedIndex);
        }

        private void WorksListView_DoubleClick(object? sender, EventArgs e)
        {
            int selectedIndex = GetSelectedWorkIndex();
            if (selectedIndex < 0)
                return;

            SetCurrentToWork(selectedIndex);
            _ = LoadWorkIntoFileTreeAsync(selectedIndex, true);
            RaiseMountedDoubleClickHandlers();
        }

        private void WorksListView_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;

            var item = worksListView.GetItemAt(e.X, e.Y);
            if (item is null)
                return;

            worksListView.SelectedIndices.Clear();
            worksListView.SelectedIndices.Add(item.Index);
            contextTarget = ContextTarget.WorkList;
            EnsureDelContextMenuItemVisible(true);
            contextMenuStrip.Show(worksListView, e.X, e.Y);
        }

        private void FileTreeView_NodeMouseClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;
            if (e.Node == null)
                return;

            fileTreeView.SelectedNode = e.Node;
            contextTarget = ContextTarget.FileTree;
            EnsureDelContextMenuItemVisible(false);
            contextMenuStrip.Show(fileTreeView, e.X, e.Y);
        }

        private void FileTreeView_NodeMouseDoubleClick(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (!TrySetCurrentFromFileTreeNode(e.Node))
                return;

            RaiseMountedDoubleClickHandlers();
        }

        private void ContextMenuClicked(object? sender, ToolStripItemClickedEventArgs e)
        {
            MyFileEditEventArgs tmp_args = new MyFileEditEventArgs();
            if (e.ClickedItem.Text == "Fav")
            {
                RasieFileEditBeginEvent(tmp_args);
                FavContextTarget();
                RasieFileEditEndEvent(tmp_args);
            }
            else if (e.ClickedItem.Text == "Del")
            {
                RasieFileEditBeginEvent(tmp_args);
                DeleteContextTarget();
                RasieFileEditEndEvent(tmp_args);
            }
            else if (e.ClickedItem.Text == "DelPart")
            {
                RasieFileEditBeginEvent(tmp_args);
                DeletePartContextTarget();
                RasieFileEditEndEvent(tmp_args);
            }
        }

        public override void MountDoubleClickEvent(TreeNodeMouseClickEventHandler handler)
        {
            mountedDoubleClickHandlers += handler;
        }

        public override void UnmountDoubleClickEvent(TreeNodeMouseClickEventHandler handler)
        {
            mountedDoubleClickHandlers -= handler;
        }

        public override void MoveCurrent(int offset)
        {
            if (Math.Abs(offset) != 1)
                throw new NotImplementedException();
            if (nodes.Count == 0)
                return;

            if (!IsValidWorkIndex(currentWorkIndex))
            {
                SetCurrentToWork(offset > 0 ? 0 : nodes.Count - 1);
                return;
            }

            if (offset > 0)
                MoveCurrentNext();
            else
                MoveCurrentPrevious();
        }

        public void RefreshMainControl()
        {
            worksListView.VirtualListSize = nodes.Count;
            worksListView.Invalidate();
            _ = LoadWorkIntoFileTreeAsync(displayedWorkIndex);
        }

        private void LoadFiles()
        {
            if (!rootDir.Exists)
                rootDir.Create();
            while (!worksListView.IsHandleCreated) Task.Delay(100).Wait();
            worksListView.Invoke(() =>
            {
                nodes.Clear();
                currentWorkIndex = -1;
                displayedWorkIndex = -1;
                worksListView.VirtualListSize = 0;
                fileTreeView.Nodes.Clear();
            });

            var pendingNodes = new List<Node>();
            LoadFilesImpl(rootDir, pendingNodes);
            FlushPendingNodes(pendingNodes);
        }

        private void LoadFilesImpl(DirectoryInfo dir, List<Node> pendingNodes)
        {
            try
            {
                foreach (var fileInfo in dir.EnumerateFiles())
                {
                    var file = new AFile(fileInfo);
                    if (file.type != AFile.FileType.OTHER)
                        AddPendingNode(CreateSingleFileNode(dir, file), pendingNodes);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }

            var seriesDirs = new List<DirectoryInfo>();
            try
            {
                foreach (var dirInfo in dir.EnumerateDirectories())
                    if (workNameRegex.IsMatch(dirInfo.Name))
                        AddPendingNode(CreateDLSiteNode(dirInfo), pendingNodes);
                    else if (seriesNameRegex.IsMatch(dirInfo.Name))
                        seriesDirs.Add(dirInfo);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }

            foreach (var dirInfo in seriesDirs)
                LoadFilesImpl(dirInfo, pendingNodes);
        }

        private static List<AFileSet> LoadFileSetsFromDir(DirectoryInfo dirInfo)
        {
            var ret = new List<AFileSet>();
            var subDirs = new List<DirectoryInfo>();
            subDirs.Add(dirInfo);
            subDirs.AddRange(dirInfo.GetDirectories("*.*", SearchOption.AllDirectories));
            subDirs.Sort((l, r) => l.FullName.CompareTo(r.FullName));
            var fileMap = new Dictionary<string, List<FileInfo>>();
            {
                var files = dirInfo.GetFiles("*.*", SearchOption.AllDirectories);
                foreach (var file in files)
                {
                    if (!fileMap.ContainsKey(file.Directory!.FullName))
                        fileMap.Add(file.Directory!.FullName, new List<FileInfo>());
                    fileMap[file.Directory!.FullName].Add(file);
                }
            }
            foreach (var subDir in subDirs)
            {
                var fileSets = new Dictionary<AFile.FileType, AFileSet>();
                if (fileMap.ContainsKey(subDir.FullName))
                    foreach (var fileInfo in fileMap[subDir.FullName])
                    {
                        var file = new AFile(fileInfo);
                        var afileType = file.type;
                        if (afileType == AFile.FileType.OTHER)
                            continue;
                        if (!fileSets.ContainsKey(afileType))
                            fileSets.Add(afileType, new AFileSet
                            {
                                title = subDir.Name + "_" + (fileSets.Count + 1).ToString(),
                                files = new List<AFile> { }
                            });
                        fileSets[afileType].files.Add(file);
                    }
                foreach (var fileSet in fileSets.Values)
                {
                    fileSet.files.Sort((l, r) => l.title.CompareTo(r.title));
                    ret.Add(fileSet);
                }
            }
            return ret;
        }

        private static Node CreateSingleFileNode(DirectoryInfo dir, AFile file)
        {
            var node = new Node();
            node.type = Node.NodeType.SingleFile;
            node.title = file.title;
            node.rootRir = dir;
            node.loaded = true;
            node.fileSets.Add(new AFileSet { title = node.title, files = new List<AFile> { file } });
            return node;
        }

        private static Node CreateDLSiteNode(DirectoryInfo dirInfo)
        {
            var node = new Node();
            string workId = workNameRegex.Match(dirInfo.Name).Groups[0].Value;
            node.title = dirInfo.Name;
            node.RJ = workId;
            node.type = Node.NodeType.DLSite;
            node.rootRir = dirInfo;
            node.loaded = false;
            return node;
        }

        private void AddPendingNode(Node node, List<Node> pendingNodes)
        {
            pendingNodes.Add(node);
            if (pendingNodes.Count >= ScanPublishBatchSize)
                FlushPendingNodes(pendingNodes);
        }

        private void FlushPendingNodes(List<Node> pendingNodes)
        {
            if (pendingNodes.Count == 0)
                return;
            var batch = pendingNodes.ToList();
            pendingNodes.Clear();
            worksListView.BeginInvoke(() =>
            {
                nodes.AddRange(batch);
                worksListView.VirtualListSize = nodes.Count;
                worksListView.Invalidate();
            });
        }

        private TreeNode CreateFileSetTreeNode(AFileSet fileSet)
        {
            var secondNode = new TreeNode();
            secondNode.Text = fileSet.title;
            secondNode.Tag = fileSet;
            foreach (var file in fileSet.files)
            {
                var leafNode = new TreeNode();
                leafNode.Text = file.title;
                leafNode.Tag = file;
                secondNode.Nodes.Add(leafNode);
            }
            return secondNode;
        }

        private static Task<List<AFileSet>> StartLoadingNode(Node node)
        {
            if (node.loadingTask != null)
                return node.loadingTask;
            node.loadingTask = LoadFileSetsFromDirAsync(node.rootRir);
            return node.loadingTask;
        }

        private static async Task<List<AFileSet>> LoadFileSetsFromDirAsync(DirectoryInfo dirInfo)
        {
            await FileSetLoadSemaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                return await Task.Run(() => LoadFileSetsFromDir(dirInfo)).ConfigureAwait(false);
            }
            finally
            {
                FileSetLoadSemaphore.Release();
            }
        }

        private async Task LoadWorkIntoFileTreeAsync(int index, bool selectCurrentAfterLoad = false)
        {
            if (!IsValidWorkIndex(index))
                return;

            var node = nodes[index];
            int loadVersion = ++fileTreeLoadVersion;
            displayedWorkIndex = index;

            fileTreeView.BeginUpdate();
            fileTreeView.Nodes.Clear();
            fileTreeView.EndUpdate();

            if (!node.loaded)
            {
                try
                {
                    node.fileSets = await StartLoadingNode(node);
                    node.loaded = true;
                    node.loadingTask = null;
                    RefreshWorkItem(index);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    node.loadingTask = null;
                    return;
                }
            }

            if (loadVersion != fileTreeLoadVersion || !IsSameNodeAtIndex(index, node))
                return;

            fileTreeView.BeginUpdate();
            try
            {
                fileTreeView.Nodes.Clear();
                foreach (var fileSet in node.fileSets)
                    fileTreeView.Nodes.Add(CreateFileSetTreeNode(fileSet));
            }
            finally
            {
                fileTreeView.EndUpdate();
            }

            if (selectCurrentAfterLoad)
                SelectCurrentFileInTree();
        }

        private bool EnsureWorkLoaded(int index)
        {
            if (!IsValidWorkIndex(index))
                return false;
            var node = nodes[index];
            if (node.loaded)
                return true;
            try
            {
                node.fileSets = StartLoadingNode(node).GetAwaiter().GetResult();
                node.loaded = true;
                node.loadingTask = null;
                RefreshWorkItem(index);
                if (displayedWorkIndex == index)
                    BuildFileTree(index);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                node.loadingTask = null;
                return false;
            }
        }

        private void BuildFileTree(int index)
        {
            if (!IsValidWorkIndex(index))
                return;
            displayedWorkIndex = index;
            fileTreeView.BeginUpdate();
            try
            {
                fileTreeView.Nodes.Clear();
                foreach (var fileSet in nodes[index].fileSets)
                    fileTreeView.Nodes.Add(CreateFileSetTreeNode(fileSet));
            }
            finally
            {
                fileTreeView.EndUpdate();
            }
        }

        public override FileInfo? GetCurrentFile()
        {
            if (!IsValidWorkIndex(currentWorkIndex))
                return null;
            if (!EnsureWorkLoaded(currentWorkIndex))
                return null;
            if (!ResolveCurrentFile(out var file))
                return null;
            return file.fileInfo;
        }

        public void DeleteNodePart(TreeNode _node)
        {
            if (_node is null)
                return;
            if (contextTarget == ContextTarget.FileTree && TryGetFileTreeNodeIndexes(_node, out var workIndex, out var fileSetIndex, out var fileIndex))
                DeletePartAt(workIndex, fileSetIndex, fileIndex);
        }

        public void DeleteNode(TreeNode _node)
        {
            DeleteSelectedWork();
        }

        public void FavNode(TreeNode _node)
        {
            FavSelectedWork();
        }

        public override void DeleteCurrentPart()
        {
            if (!IsValidWorkIndex(currentWorkIndex))
                return;
            if (!EnsureWorkLoaded(currentWorkIndex))
                return;
            if (currentCursorKind == CursorKind.File)
                DeletePartAt(currentWorkIndex, currentFileSetIndex, currentFileIndex);
            else
                DeletePartAt(currentWorkIndex, currentFileSetIndex, -1);
        }

        public override void DeleteCurrent()
        {
            DeleteWorkAt(currentWorkIndex);
        }

        public override void FavCurrent()
        {
            FavWorkAt(currentWorkIndex);
        }

        public override string GetCurrentFileDesc()
        {
            var desc = "";
            if (!IsValidWorkIndex(currentWorkIndex))
                return desc;
            if (!EnsureWorkLoaded(currentWorkIndex))
                return desc;

            var node = nodes[currentWorkIndex];
            desc += node.title + "\n";
            if (!ResolveCurrentFile(out var file))
                return desc;
            desc += file.title + "\n";
            desc += GetFileDetail(file.fileInfo);
            return desc;
        }

        public override void OpenLocalSelected()
        {
            int index = GetSelectedOrDisplayedWorkIndex();
            if (!IsValidWorkIndex(index))
                return;
            var dir = nodes[index].rootRir;
            if (!dir.Exists)
                return;
            Process.Start(new ProcessStartInfo(dir.FullName) { UseShellExecute = true });
        }

        public override void OpenWebSelected()
        {
            int index = GetSelectedOrDisplayedWorkIndex();
            if (!IsValidWorkIndex(index))
                return;
            var rj = nodes[index].RJ;
            var url = $"https://www.dlsite.com/maniax/work/=/product_id/{rj}.html";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        public override void SelectCurrent()
        {
            if (!IsValidWorkIndex(currentWorkIndex))
                return;
            worksListView.SelectedIndices.Clear();
            worksListView.SelectedIndices.Add(currentWorkIndex);
            worksListView.EnsureVisible(currentWorkIndex);
            worksListView.Focus();
            _ = LoadWorkIntoFileTreeAsync(currentWorkIndex, true);
        }

        private void MoveCurrentNext()
        {
            if (!EnsureWorkLoaded(currentWorkIndex))
                return;
            var node = nodes[currentWorkIndex];
            if (TryResolveCurrentFileIndexes(node, out var fileSetIndex, out var fileIndex))
            {
                if (TryFindNextFileInWork(node, fileSetIndex, fileIndex + 1, out var nextFileSetIndex, out var nextFileIndex))
                {
                    SetCurrentToFile(currentWorkIndex, nextFileSetIndex, nextFileIndex);
                    return;
                }
            }
            SetCurrentToWork((currentWorkIndex + 1) % nodes.Count);
        }

        private void MoveCurrentPrevious()
        {
            if (!EnsureWorkLoaded(currentWorkIndex))
                return;
            var node = nodes[currentWorkIndex];
            if (TryResolveCurrentFileIndexes(node, out var fileSetIndex, out var fileIndex)
                && TryFindPreviousFileInWork(node, fileSetIndex, fileIndex - 1, out var prevFileSetIndex, out var prevFileIndex))
            {
                SetCurrentToFile(currentWorkIndex, prevFileSetIndex, prevFileIndex);
                return;
            }
            SetCurrentToWork((currentWorkIndex - 1 + nodes.Count) % nodes.Count);
        }

        private bool TryFindNextFileInWork(Node node, int fileSetIndex, int fileIndex, out int nextFileSetIndex, out int nextFileIndex)
        {
            for (int i = Math.Max(0, fileSetIndex); i < node.fileSets.Count; i++)
            {
                int startFile = i == fileSetIndex ? Math.Max(0, fileIndex) : 0;
                if (startFile < node.fileSets[i].files.Count)
                {
                    nextFileSetIndex = i;
                    nextFileIndex = startFile;
                    return true;
                }
            }
            nextFileSetIndex = 0;
            nextFileIndex = 0;
            return false;
        }

        private bool TryFindPreviousFileInWork(Node node, int fileSetIndex, int fileIndex, out int prevFileSetIndex, out int prevFileIndex)
        {
            for (int i = Math.Min(fileSetIndex, node.fileSets.Count - 1); i >= 0; i--)
            {
                int startFile = i == fileSetIndex ? fileIndex : node.fileSets[i].files.Count - 1;
                if (startFile >= 0 && node.fileSets[i].files.Count > 0)
                {
                    prevFileSetIndex = i;
                    prevFileIndex = Math.Min(startFile, node.fileSets[i].files.Count - 1);
                    return true;
                }
            }
            prevFileSetIndex = 0;
            prevFileIndex = 0;
            return false;
        }

        private bool ResolveCurrentFile(out AFile file)
        {
            file = null!;
            if (!IsValidWorkIndex(currentWorkIndex))
                return false;
            var node = nodes[currentWorkIndex];
            if (!TryResolveCurrentFileIndexes(node, out var fileSetIndex, out var fileIndex))
                return false;
            currentFileSetIndex = fileSetIndex;
            currentFileIndex = fileIndex;
            file = node.fileSets[fileSetIndex].files[fileIndex];
            return true;
        }

        private bool TryResolveCurrentFileIndexes(Node node, out int fileSetIndex, out int fileIndex)
        {
            fileSetIndex = currentFileSetIndex;
            fileIndex = currentCursorKind == CursorKind.File ? currentFileIndex : 0;
            if (currentCursorKind == CursorKind.Work)
                fileSetIndex = 0;
            if (fileSetIndex >= 0
                && fileSetIndex < node.fileSets.Count
                && fileIndex >= 0
                && fileIndex < node.fileSets[fileSetIndex].files.Count)
                return true;
            return TryFindNextFileInWork(node, 0, 0, out fileSetIndex, out fileIndex);
        }

        private bool TrySetCurrentFromFileTreeNode(TreeNode? treeNode)
        {
            if (!TryGetFileTreeNodeIndexes(treeNode, out var workIndex, out var fileSetIndex, out var fileIndex))
                return false;
            if (fileIndex >= 0)
                SetCurrentToFile(workIndex, fileSetIndex, fileIndex);
            else
                SetCurrentToFileSet(workIndex, fileSetIndex);
            return true;
        }

        private bool TryGetFileTreeNodeIndexes(TreeNode? treeNode, out int workIndex, out int fileSetIndex, out int fileIndex)
        {
            workIndex = displayedWorkIndex;
            fileSetIndex = -1;
            fileIndex = -1;
            if (!IsValidWorkIndex(workIndex) || treeNode is null)
                return false;
            var node = nodes[workIndex];
            if (treeNode.Tag is AFileSet fileSet)
            {
                fileSetIndex = node.fileSets.IndexOf(fileSet);
                return fileSetIndex >= 0;
            }
            if (treeNode.Tag is AFile file && treeNode.Parent?.Tag is AFileSet parentFileSet)
            {
                fileSetIndex = node.fileSets.IndexOf(parentFileSet);
                if (fileSetIndex < 0)
                    return false;
                fileIndex = node.fileSets[fileSetIndex].files.IndexOf(file);
                return fileIndex >= 0;
            }
            return false;
        }

        private void SetCurrentToWork(int workIndex)
        {
            if (!IsValidWorkIndex(workIndex))
                return;
            currentWorkIndex = workIndex;
            currentFileSetIndex = 0;
            currentFileIndex = 0;
            currentCursorKind = CursorKind.Work;
        }

        private void SetCurrentToFileSet(int workIndex, int fileSetIndex)
        {
            if (!IsValidWorkIndex(workIndex))
                return;
            currentWorkIndex = workIndex;
            currentFileSetIndex = Math.Max(0, fileSetIndex);
            currentFileIndex = 0;
            currentCursorKind = CursorKind.FileSet;
        }

        private void SetCurrentToFile(int workIndex, int fileSetIndex, int fileIndex)
        {
            if (!IsValidWorkIndex(workIndex))
                return;
            currentWorkIndex = workIndex;
            currentFileSetIndex = Math.Max(0, fileSetIndex);
            currentFileIndex = Math.Max(0, fileIndex);
            currentCursorKind = CursorKind.File;
        }

        private void FavContextTarget()
        {
            int index = GetContextWorkIndex();
            FavWorkAt(index);
        }

        private void DeleteContextTarget()
        {
            int index = GetContextWorkIndex();
            DeleteWorkAt(index);
        }

        private void DeletePartContextTarget()
        {
            if (contextTarget == ContextTarget.FileTree
                && TryGetFileTreeNodeIndexes(fileTreeView.SelectedNode, out var workIndex, out var fileSetIndex, out var fileIndex))
            {
                DeletePartAt(workIndex, fileSetIndex, fileIndex);
                return;
            }

            int index = GetContextWorkIndex();
            if (!IsValidWorkIndex(index))
                return;
            if (!EnsureWorkLoaded(index))
                return;
            if (nodes[index].fileSets.Count > 0)
                DeletePartAt(index, 0, -1);
        }

        private void DeleteSelectedWork()
        {
            DeleteWorkAt(GetSelectedOrDisplayedWorkIndex());
        }

        private void FavSelectedWork()
        {
            FavWorkAt(GetSelectedOrDisplayedWorkIndex());
        }

        private void DeleteWorkAt(int index)
        {
            if (!IsValidWorkIndex(index))
                return;

            var node = nodes[index];
            Console.WriteLine($"Delete:{node.rootRir}");
            RemoveWorkAt(index);

            if (node.type == Node.NodeType.SingleFile)
            {
                if (node.fileSets.Count <= 0 || node.fileSets[0].files.Count <= 0)
                    return;
                var fileInfo = node.fileSets[0].files[0].fileInfo;
                if (fileInfo.Exists)
                {
                    var destdir = Path.Combine(rootDir.FullName, "deleted");
                    if (!Directory.Exists(destdir))
                        Directory.CreateDirectory(destdir);
                    fileInfo.MoveTo(Path.Combine(destdir, fileInfo.Name));
                }
            }
            else if (node.type == Node.NodeType.DLSite)
            {
                try
                {
                    var destdir = Path.Combine(rootDir.FullName, "deleted");
                    if (!Directory.Exists(destdir))
                        Directory.CreateDirectory(destdir);
                    Directory.Move(node.rootRir.FullName, Path.Combine(destdir, node.rootRir.Name));
                    MarkEliminated(node);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    throw;
                }
            }
        }

        private void FavWorkAt(int index)
        {
            if (!IsValidWorkIndex(index))
                return;
            if (favDir.FullName == rootDir.FullName)
                return;

            var node = nodes[index];
            Console.WriteLine($"Fav:{node.rootRir}");
            RemoveWorkAt(index);

            try
            {
                if (node.type == Node.NodeType.SingleFile)
                {
                    if (node.fileSets.Count <= 0 || node.fileSets[0].files.Count <= 0)
                        return;
                    var fileInfo = node.fileSets[0].files[0].fileInfo;
                    var dest = Path.Combine(favDir.FullName, fileInfo.Name);
                    if (!File.Exists(dest))
                        fileInfo.MoveTo(dest);
                    else
                        fileInfo.Delete();
                }
                else if (node.type == Node.NodeType.DLSite)
                {
                    var destdir = Path.Combine(favDir.FullName, node.rootRir.Name);
                    if (!Directory.Exists(destdir))
                        Directory.Move(node.rootRir.FullName, destdir);
                    else
                        Directory.Delete(node.rootRir.FullName, true);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
        }

        private void DeletePartAt(int workIndex, int fileSetIndex, int fileIndex)
        {
            if (!IsValidWorkIndex(workIndex))
                return;
            if (!EnsureWorkLoaded(workIndex))
                return;

            var node = nodes[workIndex];
            if (fileSetIndex < 0 || fileSetIndex >= node.fileSets.Count)
                return;

            if (fileIndex < 0)
                DeleteFileSetAt(workIndex, fileSetIndex);
            else
                DeleteFileAt(workIndex, fileSetIndex, fileIndex);
        }

        private void DeleteFileSetAt(int workIndex, int fileSetIndex)
        {
            var node = nodes[workIndex];
            var fileSet = node.fileSets[fileSetIndex];
            Console.WriteLine($"DelPart:{node.rootRir}");

            foreach (var file in fileSet.files)
                if (file.fileInfo.Exists)
                    file.fileInfo.Delete();

            node.fileSets.RemoveAt(fileSetIndex);
            AfterPartDeleted(workIndex);
        }

        private void DeleteFileAt(int workIndex, int fileSetIndex, int fileIndex)
        {
            var node = nodes[workIndex];
            var fileSet = node.fileSets[fileSetIndex];
            if (fileIndex < 0 || fileIndex >= fileSet.files.Count)
                return;

            var file = fileSet.files[fileIndex];
            Console.WriteLine($"DelPart:{file.fileInfo.FullName}");
            if (file.fileInfo.Exists)
                file.fileInfo.Delete();

            fileSet.files.RemoveAt(fileIndex);
            if (fileSet.files.Count <= 0)
                node.fileSets.RemoveAt(fileSetIndex);
            if (node.fileSets.Count <= 0 && node.type == Node.NodeType.DLSite)
                MarkEliminated(node);
            AfterPartDeleted(workIndex);
        }

        private void AfterPartDeleted(int workIndex)
        {
            if (!IsValidWorkIndex(workIndex))
                return;
            if (nodes[workIndex].fileSets.Count <= 0)
            {
                RemoveWorkAt(workIndex);
                return;
            }

            if (displayedWorkIndex == workIndex)
                BuildFileTree(workIndex);
            if (currentWorkIndex == workIndex && !ResolveCurrentFile(out _))
                SetCurrentToFileSet(workIndex, 0);
        }

        private void RemoveWorkAt(int index)
        {
            if (!IsValidWorkIndex(index))
                return;

            nodes.RemoveAt(index);
            worksListView.VirtualListSize = nodes.Count;
            worksListView.Invalidate();

            if (currentWorkIndex == index)
            {
                currentWorkIndex = nodes.Count == 0 ? -1 : Math.Min(index, nodes.Count - 1);
                currentFileSetIndex = 0;
                currentFileIndex = 0;
                currentCursorKind = IsValidWorkIndex(currentWorkIndex) ? CursorKind.Work : CursorKind.None;
            }
            else if (currentWorkIndex > index)
                currentWorkIndex--;

            if (displayedWorkIndex == index)
            {
                displayedWorkIndex = -1;
                fileTreeView.Nodes.Clear();
            }
            else if (displayedWorkIndex > index)
                displayedWorkIndex--;
        }

        private void MarkEliminated(Node node)
        {
            if (string.IsNullOrEmpty(node.RJ))
                return;
            string url = $"{dlServer}/?markEliminated{node.RJ}";
            using (HttpResponseMessage response = httpClient.GetAsync(url).Result)
            {
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                    throw new Exception("DLServer Return non-success");
            }
        }

        private int GetSelectedWorkIndex()
        {
            if (worksListView.SelectedIndices.Count <= 0)
                return -1;
            return worksListView.SelectedIndices[0];
        }

        private int GetSelectedOrDisplayedWorkIndex()
        {
            int selected = GetSelectedWorkIndex();
            if (IsValidWorkIndex(selected))
                return selected;
            return displayedWorkIndex;
        }

        private int GetContextWorkIndex()
        {
            if (contextTarget == ContextTarget.FileTree && IsValidWorkIndex(displayedWorkIndex))
                return displayedWorkIndex;
            return GetSelectedOrDisplayedWorkIndex();
        }

        private bool IsValidWorkIndex(int index)
        {
            return index >= 0 && index < nodes.Count;
        }

        private bool IsSameNodeAtIndex(int index, Node node)
        {
            return IsValidWorkIndex(index) && ReferenceEquals(nodes[index], node);
        }

        private void RefreshWorkItem(int index)
        {
            if (!worksListView.IsHandleCreated || !IsValidWorkIndex(index))
                return;
            worksListView.RedrawItems(index, index, false);
        }

        private void EnsureDelContextMenuItemVisible(bool visible)
        {
            if (visible)
            {
                if (!contextMenuStrip.Items.Contains(contextMenuStripItemDel))
                    contextMenuStrip.Items.Add(contextMenuStripItemDel);
            }
            else
            {
                if (contextMenuStrip.Items.Contains(contextMenuStripItemDel))
                    contextMenuStrip.Items.Remove(contextMenuStripItemDel);
            }
        }

        private void RaiseMountedDoubleClickHandlers()
        {
            mountedDoubleClickHandlers?.Invoke(
                this,
                new TreeNodeMouseClickEventArgs(new TreeNode(), MouseButtons.Left, 2, 0, 0));
        }

        private void SelectCurrentFileInTree()
        {
            if (!IsValidWorkIndex(currentWorkIndex) || displayedWorkIndex != currentWorkIndex)
                return;
            if (!ResolveCurrentFile(out _))
                return;

            if (currentFileSetIndex < 0 || currentFileSetIndex >= fileTreeView.Nodes.Count)
                return;

            var fileSetNode = fileTreeView.Nodes[currentFileSetIndex];
            TreeNode target = fileSetNode;
            if (currentCursorKind == CursorKind.File
                && currentFileIndex >= 0
                && currentFileIndex < fileSetNode.Nodes.Count)
                target = fileSetNode.Nodes[currentFileIndex];

            fileTreeView.SelectedNode = target;
            target.EnsureVisible();
        }

        string GetFileDetail(FileInfo fileInfo)
        {
            var ret = "";
            Shell32.ShellClass shell = new Shell32.ShellClass();
            Shell32.Folder dir = shell.NameSpace(fileInfo.Directory!.FullName);
            Shell32.FolderItem item = dir.ParseName(fileInfo.Name);
            var str = dir.GetDetailsOf(item, 13);
            if (str != "")
                ret += $"作者:{str}\n";
            else
            {
                str = dir.GetDetailsOf(item, 20);
                if (str != "")
                    ret += $"作者:{str}\n";
            }
            str = dir.GetDetailsOf(item, 14);
            if (str != "")
                ret += $"系列:{str}\n";
            str = dir.GetDetailsOf(item, 21);
            if (str != "")
                ret += $"标题:{str}\n";
            return ret;
        }
    }
}

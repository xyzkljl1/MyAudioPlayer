using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MyAudioPlayer.Themes;

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
                else if (ext == ".mp3" || ext == ".mp4" || ext == ".m4a" || ext == ".aac" || ext == ".avi")
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

        private enum CursorKind
        {
            None,
            Work,
            FileSet,
            File
        }

        private enum WorkTreeItemKind
        {
            Series,
            Work,
            FileSet,
            File
        }

        private class WorkTreeItem
        {
            public WorkTreeItemKind kind = WorkTreeItemKind.Series;
            public string title = "";
            public Node? work = null;
            public AFileSet? fileSet = null;
            public AFile? file = null;
            public WorkTreeItem? parent = null;
            public List<WorkTreeItem> children = new List<WorkTreeItem>();
            public bool expanded = false;
            public bool childrenMaterialized = false;
            public bool loading = false;
            public int depth = 0;
            public bool IsWork { get { return kind == WorkTreeItemKind.Work; } }
            public bool IsSeries { get { return kind == WorkTreeItemKind.Series; } }
            public bool IsPart { get { return kind == WorkTreeItemKind.FileSet || kind == WorkTreeItemKind.File; } }
        }

        public static Regex workNameRegex = new Regex("^[RVBJ]{0,2}[0-9]{3,8}");
        public static Regex seriesNameRegex = new Regex("^S ");
        private const int ScanPublishBatchSize = 500;
        // Z:\ASMR_ReliableR benchmark, 36,891 works / 566,674 files:
        // sequential 1556s, 4-way 667s, 8-way 612s, 16-way 707s, unlimited 858s.
        // Directory scans are IO-bound; 8 kept the disk busy without flooding the thread pool.
        private const int MaxConcurrentFileSetLoads = 8;
        private static readonly SemaphoreSlim FileSetLoadSemaphore = new SemaphoreSlim(MaxConcurrentFileSetLoads);

        private ListView worksListView = new ListView();
        private DirectoryInfo rootDir;
        private string dlServer;
        private DirectoryInfo favDir;
        private List<Node> nodes = new List<Node>();
        // Keep the left work tree virtualized; WinForms TreeView becomes sluggish when tens of thousands of works are materialized at once.
        private List<WorkTreeItem> rootItems = new List<WorkTreeItem>();
        private List<WorkTreeItem> visibleItems = new List<WorkTreeItem>();
        private Dictionary<Node, WorkTreeItem> workItems = new Dictionary<Node, WorkTreeItem>();
        private HttpClient httpClient;
        private ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
        private ToolStripItem contextMenuStripItemFav;
        private ToolStripItem contextMenuStripItemDelPart;
        private ToolStripItem contextMenuStripItemDel;
        private ToolStripItem contextMenuStripItemRefresh;
        private PlayerTheme currentTheme = PlayerThemes.Resolve(PlayerThemes.DefaultId);
        private int displayedWorkIndex = -1;
        private int currentWorkIndex = -1;
        private int currentFileSetIndex = 0;
        private int currentFileIndex = 0;
        private CursorKind currentCursorKind = CursorKind.None;
        private readonly SemaphoreSlim reloadSemaphore = new SemaphoreSlim(1, 1);
        private int reloadGeneration = 0;
        private bool resetVirtualViewportOnNextBatch = false;
        private event TreeNodeMouseClickEventHandler? mountedDoubleClickHandlers;
        private const int WM_VSCROLL = 0x0115;
        private const int SB_TOP = 6;

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        public PlayListDLSite(string _rootDir, MyFileEditEventHandler _begin, MyFileEditEventHandler _end)
        {
            rootDir = new DirectoryInfo(_rootDir);
            dlServer = Config.DLServerAddress;
            favDir = new DirectoryInfo(Config.DLSiteFavDir);
            Title = "DL-" + rootDir.Name;

            worksListView.Dock = DockStyle.Fill;
            worksListView.View = View.Details;
            worksListView.FullRowSelect = true;
            worksListView.HideSelection = false;
            worksListView.MultiSelect = false;
            worksListView.VirtualMode = true;
            worksListView.OwnerDraw = true;
            worksListView.HeaderStyle = ColumnHeaderStyle.None;
            worksListView.Columns.Add("", 300);
            worksListView.DrawColumnHeader += this.WorksListView_DrawColumnHeader;
            worksListView.DrawSubItem += this.WorksListView_DrawSubItem;
            worksListView.RetrieveVirtualItem += this.WorksListView_RetrieveVirtualItem;
            worksListView.SelectedIndexChanged += this.WorksListView_SelectedIndexChanged;
            worksListView.DoubleClick += this.WorksListView_DoubleClick;
            worksListView.MouseClick += this.WorksListView_MouseClick;
            worksListView.Resize += delegate { ResizeWorkListColumns(); };

            httpClient = new HttpClient();
            contextMenuStripItemFav = contextMenuStrip.Items.Add("Fav");
            contextMenuStripItemDelPart = contextMenuStrip.Items.Add("DelPart");
            contextMenuStripItemDel = contextMenuStrip.Items.Add("Del");
            contextMenuStrip.Items.Add(new ToolStripSeparator());
            contextMenuStripItemRefresh = contextMenuStrip.Items.Add("Refresh");
            contextMenuStrip.ItemClicked += this.ContextMenuClicked;

            MountFileEditEvent(_begin, _end);
            ReloadFromLocal(false);
        }

        public override void ApplyTheme(PlayerTheme theme)
        {
            currentTheme = theme;
            worksListView.BackColor = theme.ListBackColor;
            worksListView.ForeColor = theme.ListForeColor;
            worksListView.BorderStyle = BorderStyle.None;
            contextMenuStrip.BackColor = theme.SurfaceColor;
            contextMenuStrip.ForeColor = theme.TextColor;
            worksListView.Invalidate();
        }

        private void WorksListView_DrawColumnHeader(object? sender, DrawListViewColumnHeaderEventArgs e)
        {
            using var brush = new SolidBrush(currentTheme.ListBackColor);
            e.Graphics.FillRectangle(brush, e.Bounds);
        }

        private void WorksListView_DrawSubItem(object? sender, DrawListViewSubItemEventArgs e)
        {
            bool selected = e.Item?.Selected == true;
            var bounds = new Rectangle(0, e.Bounds.Top, worksListView.ClientSize.Width, e.Bounds.Height);
            using (var backgroundBrush = new SolidBrush(selected ? currentTheme.ListSelectedBackColor : currentTheme.ListBackColor))
                e.Graphics.FillRectangle(backgroundBrush, bounds);

            using (var linePen = new Pen(Color.FromArgb(60, currentTheme.BorderColor)))
                e.Graphics.DrawLine(linePen, bounds.Left, bounds.Bottom - 1, bounds.Right, bounds.Bottom - 1);

            var textBounds = new Rectangle(e.Bounds.Left + 6, e.Bounds.Top, Math.Max(1, e.Bounds.Width - 12), e.Bounds.Height);
            TextRenderer.DrawText(
                e.Graphics,
                e.SubItem?.Text ?? "",
                worksListView.Font,
                textBounds,
                selected ? currentTheme.ListSelectedForeColor : currentTheme.ListForeColor,
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        private void ResizeWorkListColumns()
        {
            if (worksListView.Columns.Count < 1)
                return;
            worksListView.Columns[0].Width = Math.Max(120, worksListView.ClientSize.Width - 8);
        }

        public override Control GetMainControl()
        {
            return worksListView;
        }

        private void WorksListView_RetrieveVirtualItem(object? sender, RetrieveVirtualItemEventArgs e)
        {
            if (!IsValidVisibleIndex(e.ItemIndex))
            {
                e.Item = new ListViewItem("");
                return;
            }

            var item = visibleItems[e.ItemIndex];
            e.Item = new ListViewItem(GetTreeText(item));
        }

        private void WorksListView_SelectedIndexChanged(object? sender, EventArgs e)
        {
            int selectedIndex = GetSelectedWorkIndex();
            displayedWorkIndex = selectedIndex;
        }

        private async void WorksListView_DoubleClick(object? sender, EventArgs e)
        {
            var item = GetSelectedVisibleItem();
            if (item is null)
                return;

            if (TrySetCurrentFromTreeItem(item))
            {
                if (item.IsWork)
                    _ = EnsureWorkChildrenAsync(item);
                RaiseMountedDoubleClickHandlers();
                return;
            }

            if (CanExpandItem(item))
                await ToggleTreeItemAsync(item);
        }

        private async void WorksListView_MouseClick(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                var item = worksListView.GetItemAt(e.X, e.Y);
                if (item is null)
                    return;

                worksListView.SelectedIndices.Clear();
                worksListView.SelectedIndices.Add(item.Index);
                var clickedTreeItem = GetVisibleItem(item.Index);
                if (clickedTreeItem is null)
                    return;

                if (CanExpandItem(clickedTreeItem))
                    await ToggleTreeItemAsync(clickedTreeItem);
                return;
            }

            if (e.Button != MouseButtons.Right)
                return;

            var clickedItem = worksListView.GetItemAt(e.X, e.Y);
            if (clickedItem is null)
            {
                worksListView.SelectedIndices.Clear();
                ConfigureContextMenu(null);
                contextMenuStrip.Show(worksListView, e.X, e.Y);
                return;
            }

            worksListView.SelectedIndices.Clear();
            worksListView.SelectedIndices.Add(clickedItem.Index);
            var treeItem = GetVisibleItem(clickedItem.Index);
            if (treeItem is null)
                return;

            ConfigureContextMenu(treeItem);
            contextMenuStrip.Show(worksListView, e.X, e.Y);
        }

        private void ContextMenuClicked(object? sender, ToolStripItemClickedEventArgs e)
        {
            if (e.ClickedItem == contextMenuStripItemRefresh)
            {
                ReloadFromLocal(true);
                return;
            }

            MyFileEditEventArgs tmp_args = new MyFileEditEventArgs();
            if (e.ClickedItem == contextMenuStripItemFav)
            {
                RasieFileEditBeginEvent(tmp_args);
                FavContextTarget();
                RasieFileEditEndEvent(tmp_args);
            }
            else if (e.ClickedItem == contextMenuStripItemDel)
            {
                RasieFileEditBeginEvent(tmp_args);
                DeleteContextTarget();
                RasieFileEditEndEvent(tmp_args);
            }
            else if (e.ClickedItem == contextMenuStripItemDelPart)
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

        public override void MoveToFirst()
        {
            if (nodes.Count == 0)
                return;
            SetCurrentToWork(0);
        }

        public void RefreshMainControl()
        {
            RebuildVisibleItems();
            worksListView.Invalidate();
        }

        private void ReloadFromLocal(bool resetViewport)
        {
            int generation = Interlocked.Increment(ref reloadGeneration);
            string? currentRootPath = GetCurrentWorkRootPath();

            Task.Run(() =>
            {
                reloadSemaphore.Wait();
                try
                {
                    LoadFiles(generation, currentRootPath, resetViewport);
                }
                finally
                {
                    reloadSemaphore.Release();
                }
            });
        }

        private string? GetCurrentWorkRootPath()
        {
            if (!IsValidWorkIndex(currentWorkIndex))
                return null;
            return nodes[currentWorkIndex].rootRir.FullName;
        }

        private bool IsCurrentReloadGeneration(int generation)
        {
            return generation == Volatile.Read(ref reloadGeneration);
        }

        private void LoadFiles(int generation, string? currentRootPath, bool resetViewport)
        {
            if (!rootDir.Exists)
                rootDir.Create();
            while (!worksListView.IsHandleCreated) Task.Delay(100).Wait();
            worksListView.Invoke(() =>
            {
                if (!IsCurrentReloadGeneration(generation))
                    return;
                nodes.Clear();
                rootItems.Clear();
                visibleItems.Clear();
                workItems.Clear();
                currentWorkIndex = -1;
                displayedWorkIndex = -1;
                if (resetViewport)
                    ResetNativeScrollToTop();
                worksListView.VirtualListSize = 0;
                resetVirtualViewportOnNextBatch = resetViewport;
                worksListView.Invalidate();
            });
            if (!IsCurrentReloadGeneration(generation))
                return;

            var pendingItems = new List<WorkTreeItem>();
            LoadFilesImpl(rootDir, null, pendingItems, generation);
            FlushPendingItems(pendingItems, generation);
            RestoreCurrentAfterReload(generation, currentRootPath);
        }

        private void LoadFilesImpl(DirectoryInfo dir, WorkTreeItem? parentItem, List<WorkTreeItem> pendingItems, int generation)
        {
            if (!IsCurrentReloadGeneration(generation))
                return;

            try
            {
                foreach (var fileInfo in dir.EnumerateFiles())
                {
                    if (!IsCurrentReloadGeneration(generation))
                        return;
                    var file = new AFile(fileInfo);
                    if (file.type != AFile.FileType.OTHER)
                        AddPendingWorkItem(CreateSingleFileNode(dir, file), parentItem, pendingItems, generation);
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
                {
                    if (!IsCurrentReloadGeneration(generation))
                        return;
                    if (workNameRegex.IsMatch(dirInfo.Name))
                        AddPendingWorkItem(CreateDLSiteNode(dirInfo), parentItem, pendingItems, generation);
                    else if (seriesNameRegex.IsMatch(dirInfo.Name))
                        seriesDirs.Add(dirInfo);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                return;
            }

            foreach (var dirInfo in seriesDirs)
            {
                if (!IsCurrentReloadGeneration(generation))
                    return;
                var seriesItem = CreateSeriesItem(dirInfo.Name, parentItem);
                AddPendingItem(seriesItem, pendingItems, generation);
                LoadFilesImpl(dirInfo, seriesItem, pendingItems, generation);
            }
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
                                title = GetFileSetTitle(dirInfo, subDir),
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

        private static string GetFileSetTitle(DirectoryInfo workDir, DirectoryInfo fileSetDir)
        {
            var title = fileSetDir.Name;
            var parentDir = fileSetDir.Parent;
            if (parentDir is null)
                return title;
            if (IsSameDirectory(fileSetDir, workDir) || IsSameDirectory(parentDir, workDir))
                return title;
            return $"{title} / {parentDir.Name}";
        }

        private static bool IsSameDirectory(DirectoryInfo left, DirectoryInfo right)
        {
            return string.Equals(NormalizeDirectoryPath(left), NormalizeDirectoryPath(right), StringComparison.OrdinalIgnoreCase);
        }

        private static string NormalizeDirectoryPath(DirectoryInfo dir)
        {
            return dir.FullName.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
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

        private static WorkTreeItem CreateSeriesItem(string title, WorkTreeItem? parentItem)
        {
            return new WorkTreeItem
            {
                kind = WorkTreeItemKind.Series,
                title = title,
                parent = parentItem,
                depth = parentItem is null ? 0 : parentItem.depth + 1,
                expanded = false
            };
        }

        private static WorkTreeItem CreateWorkItem(Node node, WorkTreeItem? parentItem)
        {
            return new WorkTreeItem
            {
                kind = WorkTreeItemKind.Work,
                title = node.title,
                work = node,
                parent = parentItem,
                depth = parentItem is null ? 0 : parentItem.depth + 1
            };
        }

        private static WorkTreeItem CreateFileSetItem(AFileSet fileSet, WorkTreeItem parentItem)
        {
            return new WorkTreeItem
            {
                kind = WorkTreeItemKind.FileSet,
                title = fileSet.title,
                fileSet = fileSet,
                parent = parentItem,
                childrenMaterialized = true,
                depth = parentItem.depth + 1
            };
        }

        private static WorkTreeItem CreateFileItem(AFile file, WorkTreeItem parentItem)
        {
            return new WorkTreeItem
            {
                kind = WorkTreeItemKind.File,
                title = file.title,
                file = file,
                parent = parentItem,
                childrenMaterialized = true,
                depth = parentItem.depth + 1
            };
        }

        private void AddTreeItemToUi(WorkTreeItem item)
        {
            if (item.parent is null)
                rootItems.Add(item);
            else
                item.parent.children.Add(item);

            if (item.work is Node node)
            {
                nodes.Add(node);
                workItems[node] = item;
            }
        }

        private void RebuildVisibleItems()
        {
            visibleItems.Clear();
            foreach (var item in rootItems)
                AddVisibleItem(item);
            worksListView.VirtualListSize = visibleItems.Count;
        }

        private void AddVisibleItem(WorkTreeItem item)
        {
            visibleItems.Add(item);
            if (!item.expanded)
                return;
            foreach (var child in item.children)
                AddVisibleItem(child);
        }

        private static string GetTreeText(WorkTreeItem item)
        {
            var marker = GetTreeMarker(item);
            return GetIndent(item) + marker + item.title;
        }

        private static string GetTreeMarker(WorkTreeItem item)
        {
            if (item.loading)
                return "[...] ";
            if (!CanExpandItem(item))
                return "    ";
            return item.expanded ? "[-] " : "[+] ";
        }

        private static string GetIndent(WorkTreeItem item)
        {
            return new string(' ', item.depth * 4);
        }

        private WorkTreeItem? GetVisibleItem(int index)
        {
            if (!IsValidVisibleIndex(index))
                return null;
            return visibleItems[index];
        }

        private WorkTreeItem? GetSelectedVisibleItem()
        {
            if (worksListView.SelectedIndices.Count <= 0)
                return null;
            return GetVisibleItem(worksListView.SelectedIndices[0]);
        }

        private bool IsValidVisibleIndex(int index)
        {
            return index >= 0 && index < visibleItems.Count;
        }

        private int GetWorkIndexForItem(WorkTreeItem item)
        {
            var node = GetWorkForItem(item);
            if (node is null)
                return -1;
            return nodes.IndexOf(node);
        }

        private static Node? GetWorkForItem(WorkTreeItem item)
        {
            var current = item;
            while (current is not null)
            {
                if (current.work is Node node)
                    return node;
                current = current.parent;
            }
            return null;
        }

        private static bool CanExpandItem(WorkTreeItem item)
        {
            if (item.kind == WorkTreeItemKind.File)
                return false;
            if (item.kind == WorkTreeItemKind.Work)
            {
                if (item.work?.type == Node.NodeType.SingleFile)
                    return false;
                return !item.childrenMaterialized || item.children.Count > 0;
            }
            return item.children.Count > 0;
        }

        private async Task ToggleTreeItemAsync(WorkTreeItem item)
        {
            if (!CanExpandItem(item))
                return;

            if (item.kind == WorkTreeItemKind.Work && !item.childrenMaterialized)
            {
                if (!await EnsureWorkChildrenAsync(item))
                    return;
                item.expanded = true;
            }
            else
                item.expanded = !item.expanded;

            RebuildVisibleItems();
            worksListView.Invalidate();
        }

        private async Task<bool> EnsureWorkChildrenAsync(WorkTreeItem item)
        {
            if (!item.IsWork || item.work is null)
                return false;
            if (item.childrenMaterialized)
                return true;
            if (item.loading)
                return false;

            var node = item.work;
            item.loading = true;
            RefreshTreeItem(item);
            try
            {
                if (!node.loaded)
                {
                    node.fileSets = await StartLoadingNode(node);
                    node.loaded = true;
                    node.loadingTask = null;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                node.loadingTask = null;
                return false;
            }
            finally
            {
                item.loading = false;
                RefreshTreeItem(item);
            }

            if (!workItems.TryGetValue(node, out var currentItem) || !ReferenceEquals(currentItem, item))
                return false;

            MaterializeWorkChildren(item);
            return true;
        }

        private void MaterializeWorkChildren(WorkTreeItem item)
        {
            if (!item.IsWork || item.work is null)
                return;

            item.children.Clear();
            if (item.work.type == Node.NodeType.SingleFile)
            {
                item.childrenMaterialized = true;
                return;
            }

            foreach (var fileSet in item.work.fileSets)
            {
                var fileSetItem = CreateFileSetItem(fileSet, item);
                foreach (var file in fileSet.files)
                    fileSetItem.children.Add(CreateFileItem(file, fileSetItem));
                item.children.Add(fileSetItem);
            }
            item.childrenMaterialized = true;
        }

        private void RefreshWorkTreeChildren(int index)
        {
            if (!IsValidWorkIndex(index))
                return;
            if (!workItems.TryGetValue(nodes[index], out var item) || !item.childrenMaterialized)
                return;
            MaterializeWorkChildren(item);
            RebuildVisibleItems();
            worksListView.Invalidate();
        }

        private void RefreshTreeItem(WorkTreeItem item)
        {
            if (!worksListView.IsHandleCreated)
                return;
            int visibleIndex = visibleItems.IndexOf(item);
            if (!IsValidVisibleIndex(visibleIndex))
                return;
            worksListView.RedrawItems(visibleIndex, visibleIndex, false);
        }

        private bool TrySetCurrentFromTreeItem(WorkTreeItem item)
        {
            if (!TryGetTreeItemIndexes(item, out var workIndex, out var fileSetIndex, out var fileIndex))
                return false;

            if (item.kind == WorkTreeItemKind.File)
                SetCurrentToFile(workIndex, fileSetIndex, fileIndex);
            else if (item.kind == WorkTreeItemKind.FileSet)
                SetCurrentToFileSet(workIndex, fileSetIndex);
            else if (item.kind == WorkTreeItemKind.Work)
                SetCurrentToWork(workIndex);
            else
                return false;
            return true;
        }

        private bool TryGetTreeItemIndexes(WorkTreeItem item, out int workIndex, out int fileSetIndex, out int fileIndex)
        {
            workIndex = -1;
            fileSetIndex = -1;
            fileIndex = -1;

            var node = GetWorkForItem(item);
            if (node is null)
                return false;
            workIndex = nodes.IndexOf(node);
            if (!IsValidWorkIndex(workIndex))
                return false;

            if (item.kind == WorkTreeItemKind.Work)
                return true;

            if (item.kind == WorkTreeItemKind.FileSet && item.fileSet is AFileSet fileSet)
            {
                fileSetIndex = node.fileSets.IndexOf(fileSet);
                return fileSetIndex >= 0;
            }

            if (item.kind == WorkTreeItemKind.File
                && item.file is AFile file
                && item.parent?.fileSet is AFileSet parentFileSet)
            {
                fileSetIndex = node.fileSets.IndexOf(parentFileSet);
                if (fileSetIndex < 0)
                    return false;
                fileIndex = node.fileSets[fileSetIndex].files.IndexOf(file);
                return fileIndex >= 0;
            }

            return false;
        }

        private WorkTreeItem? GetCurrentTreeItem()
        {
            if (!IsValidWorkIndex(currentWorkIndex))
                return null;
            var node = nodes[currentWorkIndex];
            if (!workItems.TryGetValue(node, out var workItem))
                return null;
            if (currentCursorKind == CursorKind.Work)
                return workItem;
            if (!workItem.childrenMaterialized)
                MaterializeWorkChildren(workItem);
            if (currentFileSetIndex < 0 || currentFileSetIndex >= node.fileSets.Count)
                return workItem;

            var fileSet = node.fileSets[currentFileSetIndex];
            var fileSetItem = workItem.children.FirstOrDefault(item => ReferenceEquals(item.fileSet, fileSet));
            if (fileSetItem is null || currentCursorKind != CursorKind.File)
                return fileSetItem ?? workItem;
            if (currentFileIndex < 0 || currentFileIndex >= fileSet.files.Count)
                return fileSetItem;

            var file = fileSet.files[currentFileIndex];
            return fileSetItem.children.FirstOrDefault(item => ReferenceEquals(item.file, file)) ?? fileSetItem;
        }

        private void SelectTreeItem(WorkTreeItem item)
        {
            ExpandAncestors(item);
            RebuildVisibleItems();
            int visibleIndex = visibleItems.IndexOf(item);
            if (!IsValidVisibleIndex(visibleIndex))
                return;
            worksListView.SelectedIndices.Clear();
            worksListView.SelectedIndices.Add(visibleIndex);
            worksListView.EnsureVisible(visibleIndex);
            worksListView.Focus();
        }

        private void ExpandAncestors(WorkTreeItem item)
        {
            var parent = item.parent;
            while (parent is not null)
            {
                parent.expanded = true;
                parent = parent.parent;
            }
        }

        private void RemoveWorkTreeItem(Node node)
        {
            if (!workItems.TryGetValue(node, out var item))
                return;
            workItems.Remove(node);
            RemoveTreeItem(item);
            PruneEmptySeries(item.parent);
        }

        private void RemoveTreeItem(WorkTreeItem item)
        {
            if (item.parent is null)
                rootItems.Remove(item);
            else
                item.parent.children.Remove(item);
        }

        private void PruneEmptySeries(WorkTreeItem? item)
        {
            while (item is not null && !item.IsWork && item.children.Count == 0)
            {
                var parent = item.parent;
                RemoveTreeItem(item);
                item = parent;
            }
        }

        private void AddPendingWorkItem(Node node, WorkTreeItem? parentItem, List<WorkTreeItem> pendingItems, int generation)
        {
            AddPendingItem(CreateWorkItem(node, parentItem), pendingItems, generation);
        }

        private void AddPendingItem(WorkTreeItem item, List<WorkTreeItem> pendingItems, int generation)
        {
            if (!IsCurrentReloadGeneration(generation))
                return;
            pendingItems.Add(item);
            if (pendingItems.Count >= ScanPublishBatchSize)
                FlushPendingItems(pendingItems, generation);
        }

        private void FlushPendingItems(List<WorkTreeItem> pendingItems, int generation)
        {
            if (pendingItems.Count == 0)
                return;
            var batch = pendingItems.ToList();
            pendingItems.Clear();
            worksListView.BeginInvoke(() =>
            {
                if (!IsCurrentReloadGeneration(generation))
                    return;
                foreach (var item in batch)
                    AddTreeItemToUi(item);
                RebuildVisibleItems();
                bool resetViewport = resetVirtualViewportOnNextBatch;
                resetVirtualViewportOnNextBatch = false;
                if (resetViewport)
                    RefreshVirtualViewport(true);
                else
                    worksListView.Invalidate();
            });
        }

        private void RefreshVirtualViewport(bool resetToTop)
        {
            int itemCount = worksListView.VirtualListSize;
            if (itemCount <= 0)
            {
                worksListView.Invalidate();
                return;
            }

            if (resetToTop)
            {
                ResetNativeScrollToTop();
                worksListView.EnsureVisible(0);
            }

            int firstVisible = resetToTop ? 0 : GetFirstVisibleIndex();
            int visibleRows = Math.Max(1, worksListView.ClientSize.Height / Math.Max(1, worksListView.Font.Height + 4) + 2);
            int lastVisible = Math.Min(itemCount - 1, firstVisible + visibleRows);
            worksListView.RedrawItems(firstVisible, lastVisible, true);
            worksListView.Update();
        }

        private void ResetNativeScrollToTop()
        {
            if (!worksListView.IsHandleCreated)
                return;
            SendMessage(worksListView.Handle, WM_VSCROLL, new IntPtr(SB_TOP), IntPtr.Zero);
        }

        private int GetFirstVisibleIndex()
        {
            try
            {
                return Math.Clamp(worksListView.TopItem?.Index ?? 0, 0, Math.Max(0, worksListView.VirtualListSize - 1));
            }
            catch (InvalidOperationException)
            {
                return 0;
            }
        }

        private void RestoreCurrentAfterReload(int generation, string? currentRootPath)
        {
            if (string.IsNullOrEmpty(currentRootPath))
                return;

            worksListView.BeginInvoke(() =>
            {
                if (!IsCurrentReloadGeneration(generation))
                    return;

                int index = nodes.FindIndex(node =>
                    string.Equals(node.rootRir.FullName, currentRootPath, StringComparison.OrdinalIgnoreCase));
                if (!IsValidWorkIndex(index))
                    return;
                SetCurrentToWork(index);
                if (workItems.TryGetValue(nodes[index], out var item))
                {
                    SelectTreeItem(item);
                    RefreshVirtualViewport(false);
                }
            });
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
                RefreshWorkTreeChildren(index);
                RefreshWorkItem(index);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                node.loadingTask = null;
                return false;
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
            DeletePartContextTarget();
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

        public override string GetCurrentMiniTitle()
        {
            if (!IsValidWorkIndex(currentWorkIndex))
                return "";
            if (!EnsureWorkLoaded(currentWorkIndex))
                return "";

            var node = nodes[currentWorkIndex];
            if (TryResolveCurrentFileIndexes(node, out var fileSetIndex, out var fileIndex))
            {
                var fileSet = node.fileSets[fileSetIndex];
                return string.Join(
                    " / ",
                    new[] { fileSet.files[fileIndex].title, fileSet.title, node.title }
                        .Where(part => !string.IsNullOrWhiteSpace(part)));
            }

            return node.title;
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
            if (!EnsureWorkLoaded(currentWorkIndex))
                return;
            var item = GetCurrentTreeItem();
            if (item is null)
                return;
            SelectTreeItem(item);
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
            var item = GetSelectedVisibleItem();
            if (item is not null
                && item.IsPart
                && TryGetTreeItemIndexes(item, out var workIndex, out var fileSetIndex, out var fileIndex))
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

            RefreshWorkTreeChildren(workIndex);
            if (currentWorkIndex == workIndex && !ResolveCurrentFile(out _))
                SetCurrentToFileSet(workIndex, 0);
        }

        private void RemoveWorkAt(int index)
        {
            if (!IsValidWorkIndex(index))
                return;

            var node = nodes[index];
            RemoveWorkTreeItem(node);
            nodes.RemoveAt(index);
            RebuildVisibleItems();
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
                displayedWorkIndex = -1;
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
            var item = GetSelectedVisibleItem();
            if (item is null)
                return -1;
            return GetWorkIndexForItem(item);
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
            return GetSelectedOrDisplayedWorkIndex();
        }

        private bool IsValidWorkIndex(int index)
        {
            return index >= 0 && index < nodes.Count;
        }

        private void RefreshWorkItem(int index)
        {
            if (!worksListView.IsHandleCreated || !IsValidWorkIndex(index))
                return;
            if (!workItems.TryGetValue(nodes[index], out var item))
                return;
            int visibleIndex = visibleItems.IndexOf(item);
            if (!IsValidVisibleIndex(visibleIndex))
                return;
            worksListView.RedrawItems(visibleIndex, visibleIndex, false);
        }

        private void ConfigureContextMenu(WorkTreeItem? item)
        {
            bool hasTarget = item is not null && IsValidWorkIndex(GetWorkIndexForItem(item));
            contextMenuStripItemFav.Enabled = hasTarget;
            contextMenuStripItemDelPart.Enabled = hasTarget;
            contextMenuStripItemDel.Enabled = hasTarget;
            EnsureDelContextMenuItemVisible(hasTarget && item!.IsWork);
        }

        private void EnsureDelContextMenuItemVisible(bool visible)
        {
            contextMenuStripItemDel.Visible = visible;
        }

        private void RaiseMountedDoubleClickHandlers()
        {
            mountedDoubleClickHandlers?.Invoke(
                this,
                new TreeNodeMouseClickEventArgs(new TreeNode(), MouseButtons.Left, 2, 0, 0));
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

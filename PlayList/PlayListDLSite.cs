using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
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
                else if (ext == ".mp3" || ext == ".mp4" || ext==".m4a" ||ext==".avi")
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
            public bool loaded = false;//是否已创建子节点
            public Task<List<AFileSet>>? loadingTask = null;
        };
        public static Regex workNameRegex = new Regex("^[RVBJ]{0,2}[0-9]{3,8}");
        public static Regex seriesNameRegex = new Regex("^S ");
        private const string LoadingPlaceholderText = "Loading...";
        private const int AppendBatchSize = 50;
        // Z:\ASMR_ReliableR benchmark, 36,891 works / 566,674 files:
        // sequential 1556s, 4-way 667s, 8-way 612s, 16-way 707s, unlimited 858s.
        // Directory scans are IO-bound; 8 kept the disk busy without flooding the thread pool.
        private const int MaxConcurrentFileSetLoads = 8;
        private static readonly SemaphoreSlim FileSetLoadSemaphore = new SemaphoreSlim(MaxConcurrentFileSetLoads);
        private static readonly object LoadingPlaceholderTag = new object();
        private TreeView treeView = new TreeView();
        private TreeNode? currentNode = null;//当前(正在播放的)曲目，和Selected不同，双击触发
        private DirectoryInfo rootDir;
        private string dlServer;
        private DirectoryInfo favDir;
        private List<Node> nodes = new List<Node>();
        private HttpClient httpClient;
        ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
        ToolStripItem contextMenuStripItemDel;
        public PlayListDLSite(string _rootDir,MyFileEditEventHandler _begin,MyFileEditEventHandler _end)
        {
            rootDir = new DirectoryInfo(_rootDir);
            dlServer = Config.DLServerAddress;
            favDir = new DirectoryInfo(Config.DLSiteFavDir);
            Title = "DL-" + rootDir.Name;
            treeView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            treeView.NodeMouseDoubleClick += this.NodeDoubleClicked;
            treeView.NodeMouseClick += this.NodeClicked;
            treeView.BeforeExpand += this.NodeBeforeExpand;
            //treeView.Scrollable = true;//需要设置tabPage.AutoScroll，而不是treeView.Scrollable
            httpClient = new HttpClient();
            contextMenuStrip.Items.Add("Fav");
            contextMenuStrip.Items.Add("DelPart");
            contextMenuStripItemDel = contextMenuStrip.Items.Add("Del");
            contextMenuStrip.ItemClicked += this.ContextMenuClicked;
            MountFileEditEvent(_begin, _end);
            Task.Run(LoadFiles);
        }
        //TODO: 添加搜索栏
        public override Control GetMainControl()
        {
            return treeView;
        }
        private void NodeDoubleClicked(object? sender, TreeNodeMouseClickEventArgs e)
        {
            currentNode = treeView.SelectedNode;
        }
        private void NodeClicked(object? sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
                return;
            if (e.Node == null)
                return;
            treeView.SelectedNode = e.Node;
            if(e.Node!=null)
            {
                if (e.Node.Tag as Node is not null)//只在顶级节点上显示Del，防止手抖误删
                {
                    if (!contextMenuStrip.Items.Contains(contextMenuStripItemDel))
                        contextMenuStrip.Items.Add(contextMenuStripItemDel);
                }
                else
                {
                    if (contextMenuStrip.Items.Contains(contextMenuStripItemDel))
                        contextMenuStrip.Items.Remove(contextMenuStripItemDel);
                }
                contextMenuStrip.Show(treeView, e.X, e.Y);
            }
        }
        private async void NodeBeforeExpand(object? sender, TreeViewCancelEventArgs e)
        {
            if (e.Node is not null)
                await LoadTreeNodeChildrenAsync(e.Node);
        }
        private void ContextMenuClicked(object? sender, ToolStripItemClickedEventArgs e)
        {
            MyFileEditEventArgs tmp_args=new MyFileEditEventArgs();
            if (e.ClickedItem.Text == "Fav")
            {
                RasieFileEditBeginEvent(tmp_args);
                FavNode(treeView.SelectedNode);
                RasieFileEditEndEvent(tmp_args);
            }
            else if(e.ClickedItem.Text =="Del")
            {
                RasieFileEditBeginEvent(tmp_args);
                DeleteNode(treeView.SelectedNode);
                RasieFileEditEndEvent(tmp_args);
            }
            else if(e.ClickedItem.Text=="DelPart")
            {
                RasieFileEditBeginEvent(tmp_args);
                DeleteNodePart(treeView.SelectedNode);
                RasieFileEditEndEvent(tmp_args);
            }
        }
        public override void MountDoubleClickEvent(TreeNodeMouseClickEventHandler handler)
        {
            treeView.NodeMouseDoubleClick += handler;
        }
        public override void UnmountDoubleClickEvent(TreeNodeMouseClickEventHandler handler)
        {
            treeView.NodeMouseDoubleClick -= handler;
        }
        public override void MoveCurrent(int offset)
        {
            if (Math.Abs(offset) != 1)
                throw new NotImplementedException();
            if (this.currentNode == null)//如果没有选中任何节点，选中第一个
            {
                if (treeView.Nodes.Count == 0)
                    return;
                this.currentNode = treeView.Nodes[0];
                return;
            }
            //先找到当前叶节点
            var currentNode = this.currentNode!;//注意不使用treeView.selectedNode
            EnsureTreeNodeLoaded(currentNode);
            while (currentNode.Nodes.Count > 0 && !IsLoadingPlaceholder(currentNode.Nodes[0]))//
            {
                currentNode = currentNode.Nodes[0];
                EnsureTreeNodeLoaded(currentNode);
            }
            TreeNode nextNode;
            if (offset == 1)
            {
                var parentNode = currentNode;
                while (parentNode != null && parentNode.NextNode == null)//找兄弟节点，如果找不到就找上级的兄弟节点
                    parentNode = parentNode.Parent;
                //找到null说明到头了，直接取列表第一项
                if (parentNode == null)
                    nextNode = treeView.Nodes[0];
                else
                    nextNode = parentNode.NextNode;
            }
            else
            {
                var parentNode = currentNode;
                while (parentNode != null && parentNode.PrevNode == null)//找兄弟节点，如果找不到就找上级的兄弟节点
                    parentNode = parentNode.Parent;
                //找到null说明到头了，直接取列表最后一项
                if (parentNode == null)
                    nextNode = treeView.Nodes[treeView.Nodes.Count - 1];
                else
                    nextNode = parentNode.PrevNode;
            }
            this.currentNode = nextNode;
        }
        public void RefreshMainControl()
        {
            treeView.SuspendLayout();
            treeView.Nodes.Clear();
            foreach (var fileNode in this.nodes)
                treeView.Nodes.Add(CreateRootTreeNode(fileNode));
            treeView.ResumeLayout();
        }
        private void LoadFiles()
        {
            if (!rootDir.Exists)
                rootDir.Create();
            //此函数是在MainWindow构造函数里开子线程调用的，如果load文件过快，此时可能窗口句柄尚未创建，因此要等待
            while (!treeView.IsHandleCreated) Task.Delay(100).Wait();
            treeView.Invoke(() =>
            {
                nodes.Clear();
                treeView.Nodes.Clear();
                treeView.Nodes.Add(CreateLoadingNode());
            });//编辑控件需要在主线程，借用treeView的invoke

            var pendingNodes = new List<Node>();
            LoadFilesImpl(rootDir, pendingNodes);
            FlushPendingNodes(pendingNodes);
            treeView.BeginInvoke(() => RemoveLoadingPlaceholders(treeView.Nodes));
            //TODO:sort
        }
        private void LoadFilesImpl(DirectoryInfo dir, List<Node> pendingNodes)
        {
            //注意：GetDirectories/GetFiles很慢，应尽可能并发、减少调用次数
            //同样的查询，合并为一次调用比分多次调用快，但是全部合并为一次在顶层调用仍然很慢，需要拆分并行
            //优化后一次全部加载也很慢，后台执行
            try
            {
                foreach (var fileInfo in dir.EnumerateFiles())//因为singleFile并不常见，就不优化了
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
        private static Node LoadSingleNodeFromDir(DirectoryInfo dirInfo)
        {
            var node = CreateDLSiteNode(dirInfo);
            node.fileSets = LoadFileSetsFromDir(dirInfo);
            node.loaded = true;
            return node;
        }
        private static List<AFileSet> LoadFileSetsFromDir(DirectoryInfo dirInfo)
        {
            var ret = new List<AFileSet>();
            //每个目录下每种类型分别放到一个set里
            //set按目录名排序，set内按文件名排序
            {
                var subDirs = new List<DirectoryInfo>();
                subDirs.Add(dirInfo);
                subDirs.AddRange(dirInfo.GetDirectories("*.*", SearchOption.AllDirectories));
                subDirs.Sort((l, r) => l.FullName.CompareTo(r.FullName));
                var fileMap=new Dictionary<string, List<FileInfo>>();
                {
                    var files = dirInfo.GetFiles("*.*", SearchOption.AllDirectories);
                    foreach(var file in files)
                    {
                        if(!fileMap.ContainsKey(file.Directory!.FullName))
                            fileMap.Add(file.Directory!.FullName, new List<FileInfo>());
                        fileMap[file.Directory!.FullName].Add(file);
                    }
                }
                foreach (var subDir in subDirs)
                {
                    var fileSets = new Dictionary<AFile.FileType, AFileSet>();
                    //foreach (var fileInfo in subDir.GetFiles())
                    if(fileMap.ContainsKey(subDir.FullName))
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
            if (pendingNodes.Count >= AppendBatchSize)
                FlushPendingNodes(pendingNodes);
        }
        private void FlushPendingNodes(List<Node> pendingNodes)
        {
            if (pendingNodes.Count == 0)
                return;
            var batch = pendingNodes.ToList();
            pendingNodes.Clear();
            treeView.BeginInvoke(() =>
            {
                treeView.SuspendLayout();
                RemoveLoadingPlaceholders(treeView.Nodes);
                foreach (var node in batch)
                {
                    nodes.Add(node);
                    treeView.Nodes.Add(CreateRootTreeNode(node));
                }
                treeView.ResumeLayout();
            });
        }
        private TreeNode CreateRootTreeNode(Node fileNode)
        {
            var rootNode = new TreeNode();
            rootNode.Text = fileNode.title;
            rootNode.Tag = fileNode;
            foreach (var fileSet in fileNode.fileSets)
                rootNode.Nodes.Add(CreateFileSetTreeNode(fileSet));
            if (fileNode.type == Node.NodeType.DLSite && !fileNode.loaded)
                rootNode.Nodes.Add(CreateLoadingNode());
            return rootNode;
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
        private static TreeNode CreateLoadingNode()
        {
            var node = new TreeNode();
            node.Text = LoadingPlaceholderText;
            node.Tag = LoadingPlaceholderTag;
            return node;
        }
        private static bool IsLoadingPlaceholder(TreeNode node)
        {
            return ReferenceEquals(node.Tag, LoadingPlaceholderTag);
        }
        private static void RemoveLoadingPlaceholders(TreeNodeCollection treeNodes)
        {
            for (int i = treeNodes.Count - 1; i >= 0; i--)
                if (IsLoadingPlaceholder(treeNodes[i]))
                    treeNodes.RemoveAt(i);
        }
        private async Task LoadTreeNodeChildrenAsync(TreeNode treeNode)
        {
            if (treeNode.Tag is not Node node)
                return;
            if (node.type != Node.NodeType.DLSite || node.loaded)
                return;
            if (treeNode.Nodes.Count == 0)
                treeNode.Nodes.Add(CreateLoadingNode());
            try
            {
                var fileSets = await StartLoadingNode(node);
                if (treeView.IsDisposed || treeNode.TreeView != treeView)
                    return;
                ApplyLoadedFileSets(treeNode, node, fileSets);
            }
            catch (Exception e)
            {
                if (!treeView.IsDisposed && treeNode.TreeView == treeView)
                    ApplyLoadError(treeNode, node, e);
            }
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
            await FileSetLoadSemaphore.WaitAsync();
            try
            {
                return await Task.Run(() => LoadFileSetsFromDir(dirInfo));
            }
            finally
            {
                FileSetLoadSemaphore.Release();
            }
        }
        private bool EnsureTreeNodeLoaded(TreeNode treeNode)
        {
            var rootNode = GetTopNode(treeNode);
            if (rootNode is null || rootNode.Tag is not Node node)
                return true;
            if (node.type != Node.NodeType.DLSite || node.loaded)
                return true;
            try
            {
                var fileSets = StartLoadingNode(node).GetAwaiter().GetResult();
                if (rootNode.TreeView == treeView)
                    ApplyLoadedFileSets(rootNode, node, fileSets);
                return true;
            }
            catch (Exception e)
            {
                if (rootNode.TreeView == treeView)
                    ApplyLoadError(rootNode, node, e);
                return false;
            }
        }
        private void ApplyLoadedFileSets(TreeNode rootNode, Node node, List<AFileSet> fileSets)
        {
            if (node.loaded)
                return;
            node.fileSets = fileSets;
            node.loaded = true;
            node.loadingTask = null;
            rootNode.Nodes.Clear();
            foreach (var fileSet in node.fileSets)
                rootNode.Nodes.Add(CreateFileSetTreeNode(fileSet));
        }
        private void ApplyLoadError(TreeNode rootNode, Node node, Exception e)
        {
            Console.WriteLine(e.Message);
            node.loadingTask = null;
            rootNode.Nodes.Clear();
            rootNode.Nodes.Add(CreateLoadingNode());
        }
        public override FileInfo? GetCurrentFile()
        {
            if(this.currentNode == null)
                return null;
            //选中叶节点返回文件，选择上级时向下寻找第一个子节点
            var currentNode = this.currentNode!;//注意不使用treeView.SelectedNode
            if (currentNode.Tag as AFile != null)
                return (currentNode.Tag as AFile)!.fileInfo;
            else if (currentNode.Tag as AFileSet != null)
            {
                var files = (currentNode.Tag as AFileSet)!.files;
                if (files.Count == 0)
                    return null;
                return files.First().fileInfo;
            }
            else if(currentNode.Tag as Node != null)
            {
                if (!EnsureTreeNodeLoaded(currentNode))
                    return null;
                var fileSet = (currentNode.Tag as Node)!.fileSets;
                if (fileSet.Count == 0)//不会为null
                    return null;
                var files = fileSet.First().files;
                if (files.Count == 0)
                    return null;
                return files.First().fileInfo;
            }
            return null;
        }
        //删除选中的节点的文件
        public void DeleteNodePart(TreeNode _node)
        {
            var selectedNode = _node;
            if (selectedNode is null)
                return;
            if (selectedNode.Tag is Node)
                EnsureTreeNodeLoaded(selectedNode);
            if (selectedNode.Tag is Node)//顶级节点,此时播放的肯定是第一个文件(播放完第一个通过MoveCurrent播放第二个时会选中子节点)，删除第一个
            {
                while (selectedNode.Nodes.Count > 0 && !IsLoadingPlaceholder(selectedNode.Nodes[0]))//
                    selectedNode = selectedNode.Nodes[0];
            }
            if (selectedNode.Tag is AFileSet)
            {
                var fileSet = (selectedNode.Tag as AFileSet)!;
                var node=(selectedNode.Parent.Tag as Node)!;
                Console.WriteLine($"DelPart:{node.rootRir}");
                //如果currentNode也被删除，则移动到下个节点，selectedNode不处理
                if (IsChildren(currentNode,selectedNode))
                {
                    //不用管currentNode是哪个，因为selectedNode整个被删除，直接移动到selectedNode的NextNode
                    if (selectedNode.NextNode != null)
                        currentNode = selectedNode.NextNode;
                    else if (selectedNode.Parent.NextNode != null)//selectedNode是FileSet，不需要检查grandparent
                        currentNode = selectedNode.Parent.NextNode;
                    //如果移动后currentNode仍然要被删除，清空currentNode
                    if (IsChildren(currentNode,selectedNode))
                        currentNode = null;
                }
                var tmpParent=selectedNode.Parent;//remove之后parent会置空
                treeView.Nodes.Remove(selectedNode);
                node.fileSets.Remove(fileSet);//node也要修改否则之后再选中所在节点会出问题
                if (tmpParent.Nodes.Count <= 0)
                    treeView.Nodes.Remove(tmpParent);
                foreach (var file in fileSet.files)//删除下面的所有文件，注意不要删除文件夹，因为AFileSet可能在顶级目录
                    file.fileInfo.Delete();
        }
            else if (selectedNode.Tag is AFile)
            {
                var fileset = (selectedNode.Parent.Tag as AFileSet)!;
                var file = (selectedNode.Tag as AFile)!;
                Console.WriteLine($"DelPart:{file.fileInfo.FullName}");
                FileInfo fileInfo = file.fileInfo;
                //如果currentNode也被删除，则移动到下个节点
                if (IsChildren(currentNode, selectedNode))
                {
                    MoveCurrent(1);
                    if (IsChildren(currentNode, selectedNode))//如果移动后currentNode仍然等于selectedNode(即只有这个叶节点)，清空currentNode
                        currentNode = null;
                }
                var tmpParent = selectedNode.Parent;//remove之后parent会置空
                treeView.Nodes.Remove(selectedNode);
                fileset.files.Remove(file);//fileset也要修改否则之后再选中所在节点会出问题
                if (tmpParent.Nodes.Count <= 0)
                {
                    var tmptmpParent= tmpParent.Parent;
                    treeView.Nodes.Remove(tmpParent);
                    if (tmptmpParent.Nodes.Count <= 0 && tmptmpParent.Tag is Node)
                    {
                        try
                        {
                            //通知DLSiteHelperServer
                            string url = $"{dlServer}/?markEliminated{(tmptmpParent.Tag as Node)!.RJ}";
                            using (HttpResponseMessage response = httpClient.GetAsync(url).Result)
                            {
                                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                                    throw new Exception("DLServer Return non-success");
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.Message);
                            throw;
                        }
                    }
                }
                if (fileInfo.Exists)
                    fileInfo.Delete();
            }
        }
        //删除选中的节点所在的顶级节点并通知server标记为eliminated
        public void DeleteNode(TreeNode _node) 
        {
            var selectedNode = GetTopNode(_node);
            if (selectedNode is null)
                return;
            var node = (selectedNode.Tag as Node)!;
            Console.WriteLine($"Delete:{node.rootRir}");
            //如果会删除currentNode则移动CurrentNode
            if(IsChildren(currentNode,selectedNode))
            {
                //因为selectedNode连同子节点都被删除，直接把currentNode移动到SelectedNode的Next
                if (selectedNode.NextNode != null)
                    currentNode = selectedNode.NextNode;
                else if (treeView.Nodes.Count == 0)
                    currentNode = null;
                else
                    currentNode = treeView.Nodes[0];
            }
            treeView.Nodes.Remove(selectedNode);
            if(node.type==Node.NodeType.SingleFile)
            {
                if (node.fileSets.Count <= 0)//SingleFile一定只有一个文件,但是可能因运行程序后删除导致没有文件
                    return;
                if (node.fileSets[0].files.Count <= 0)
                    return;
                var fileInfo = node.fileSets[0].files[0].fileInfo;
                //仅删除文件(移动到deleted)
                if (fileInfo.Exists)
                {
                    var destdir = rootDir.FullName + "/deleted";
                    if (!Directory.Exists(destdir))
                        Directory.CreateDirectory(destdir);
                    fileInfo.MoveTo($"{destdir}/{fileInfo.Name}");
                }
            }
            else if(node.type==Node.NodeType.DLSite)
            {
                try
                {
                    //移动到deleted文件夹，这是为了防止误删
                    {
                        var destdir = rootDir.FullName + "/deleted";
                        if (!Directory.Exists(destdir))
                            Directory.CreateDirectory(destdir);
                        Directory.Move($"{rootDir.FullName}/{node.rootRir.Name}", $"{destdir}/{node.rootRir.Name}");
                    }
                    //通知DLSiteHelperServer
                    string url = $"{dlServer}/?markEliminated{node.RJ}";
                    using (HttpResponseMessage response = httpClient.GetAsync(url).Result)
                    {
                        if (response.StatusCode != System.Net.HttpStatusCode.OK)
                            throw new Exception("DLServer Return non-success");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                    throw;
                }
            }
        }
        public void FavNode(TreeNode _node) 
        {
            if (favDir.FullName == rootDir.FullName)//当前根目录就是favDir
                return;
            var selectedNode = GetTopNode(_node);
            if (selectedNode is null)
                return;
            var node = (selectedNode.Tag as Node)!;
            Console.WriteLine($"Fav:{node.rootRir}");
            //如果会删除currentNode则移动CurrentNode
            if (IsChildren(currentNode,selectedNode))
            {
                //因为selectedNode连同子节点都被删除，直接把currentNode移动到SelectedNode的Next
                if (selectedNode.NextNode != null)
                    currentNode = selectedNode.NextNode;
                else if (treeView.Nodes.Count == 0)
                    currentNode = null;
                else
                    currentNode = treeView.Nodes[0];
            }
            treeView.Nodes.Remove(selectedNode);
            try
            {
                if (node.type == Node.NodeType.SingleFile)
                {
                    if (node.fileSets.Count <= 0)//SingleFile一定只有一个文件,但是可能因运行程序后删除导致没有文件
                        return;
                    if (node.fileSets[0].files.Count <= 0)
                        return;
                    var fileInfo=node.fileSets[0].files[0].fileInfo;
                    var dest = $"{favDir.FullName}/{fileInfo.Name}";
                    if (!File.Exists(dest))
                        fileInfo.MoveTo(dest);
                    else//目标目录已存在则不拷贝仅删除
                        fileInfo.Delete();
                }
                else if (node.type == Node.NodeType.DLSite)
                {
                    var dirname = (selectedNode.Tag as Node)!.rootRir.Name;
                    var destdir = $"{favDir.FullName}/{dirname}";
                    if (!Directory.Exists(destdir))
                        Directory.Move($"{rootDir.FullName}/{dirname}", $"{favDir.FullName}/{dirname}");
                    else//目标目录已存在则不拷贝仅删除
                        Directory.Delete($"{rootDir.FullName}/{dirname}", true);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
        }

        public override void DeleteCurrentPart()
        {
            DeleteNodePart(currentNode!);
        }
        //删除选中的节点所在的顶级节点并通知server标记为eliminated
        public override void DeleteCurrent()
        {
            DeleteNode(currentNode!);
        }
        public override void FavCurrent()
        {
            FavNode(currentNode!);
        }

        public override string GetCurrentFileDesc() 
        {
            var desc = "";
            {
                var curNode = GetCurrentTopNode();
                if (curNode is null)
                    return desc;
                EnsureTreeNodeLoaded(curNode);
                var node = (curNode.Tag as Node)!;
                desc += node.title+"\n";
            }
            {
                var curNode = GetCurrentLeafNode();
                if (curNode is null)
                    return desc;
                var node = (curNode.Tag as AFile)!;
                desc += node.title+"\n";
                desc += GetFileDetail(node.fileInfo);
            }
            return desc;
        }
        public override void OpenLocalSelected()
        {
            var selectedNode = GetTopNode(treeView.SelectedNode);
            if (selectedNode is null)
                return;
            var dir = (selectedNode.Tag as Node)!.rootRir;
            if (!dir.Exists)
                return;
            //更新net版本后UseShellExecute默认值变为false导致无法打开url，需要指定为true
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(dir.FullName) { UseShellExecute = true });
        }
        public override void OpenWebSelected() 
        {
            var selectedNode = GetTopNode(treeView.SelectedNode);
            if (selectedNode is null)
                return;
            var rj = (selectedNode.Tag as Node)!.RJ;
            var url = $"https://www.dlsite.com/maniax/work/=/product_id/{rj}.html";
            //更新net版本后UseShellExecute默认值变为false导致无法打开url，需要指定为true
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(url) { UseShellExecute = true });
        }
        TreeNode? GetTopNode(TreeNode? selectedNode)
        {
            if (selectedNode is null)
                return null;
            while (selectedNode.Parent is not null)
                selectedNode = selectedNode.Parent;
            if (selectedNode.Tag as Node is null)
                return null;
            return selectedNode;
        }
        TreeNode? GetCurrentTopNode()
        {
            if (currentNode is null)
                return null;
            return GetTopNode(this.currentNode);
        }
/*        TreeNode? GetSelectedLeafNode()
        {
            var selectedNode = treeView.SelectedNode;
            while (selectedNode.Nodes.Count > 0)
                selectedNode = selectedNode.Nodes[0];
            if (selectedNode.Tag is not AFile)
                return null;
            return selectedNode;
        }
*/
        TreeNode? GetCurrentLeafNode()
        {
            if (currentNode is null)
                return null;
            var curNode = this.currentNode;
            EnsureTreeNodeLoaded(curNode);
            while (curNode.Nodes.Count > 0 && !IsLoadingPlaceholder(curNode.Nodes[0]))
            {
                curNode = curNode.Nodes[0];
                EnsureTreeNodeLoaded(curNode);
            }
            if (curNode.Tag is not AFile)
                return null;
            return curNode;
        }
        string GetFileDetail(FileInfo fileInfo)
        {
            var ret = "";
            Shell32.ShellClass shell = new Shell32.ShellClass();
            Shell32.Folder dir = shell.NameSpace(fileInfo.Directory!.FullName);
            Shell32.FolderItem item = dir.ParseName(fileInfo.Name);
            /*{
                int i = 0;
                while (true)
                {
                    var key = dir.GetDetailsOf(null, i);
                    if (string.IsNullOrEmpty(key))
                        break;
                    Console.WriteLine(key);
                    Console.WriteLine(i);
                    i++;
                }
            }*/
            //Artists13 Authors20
            var str = dir.GetDetailsOf(item, 13);
            if (str != "")
                ret += $"作者:{str}\n";
            else
            {
                str = dir.GetDetailsOf(item, 20);
                if (str != "")
                    ret += $"作者:{str}\n";
            }
            //Album14
            str = dir.GetDetailsOf(item, 14);
            if (str != "")
                ret += $"系列:{str}\n";
            //Title21
            str = dir.GetDetailsOf(item, 21);
            if (str != "")
                ret += $"标题:{str}\n";
            return ret;
        }
        bool IsChildren(TreeNode? child,TreeNode node)
        {
            if(child is null)
                return false;
            if(child==node)
                return true;
            foreach (TreeNode subnode in node.Nodes)
                if (subnode == child)
                    return true;
                else if (subnode.Nodes.Contains(child))
                    return true;
            return false;
        }
        public override void SelectCurrent() 
        {
            if (currentNode is null)
                return;
            treeView.SelectedNode = currentNode;
            treeView.SelectedNode.EnsureVisible();
            treeView.Focus();//treeView如果不处于focus状态，选中的条目不会高亮看不见
        }
    }
}

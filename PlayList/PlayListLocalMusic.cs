using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static MyAudioPlayer.PlayList.PlayListDLSite.AFile;

/*
 * 单文件，无子目录
 * 视频视作音频播放
 * 目标是播放能够播放所有既存文件，因此不筛选直接展示所有文件，防止漏掉乱七八糟的格式
 * 懒得上数据库，把文件列表写入文件存在音频同目录下
 * 分Fav和All两个文件夹，Fav时复制到fav目录，Del时移动到deleted
 */
namespace MyAudioPlayer.PlayList
{
    internal class PlayListLocalMusic : PlayListBase
    {
        static string ConfigFileName = "PlayList.json";
        public class Node
        {
            public enum FileType
            {
                Default,
            };
            public string title = "";
            public FileInfo fileInfo;
            public FileType type = FileType.Default;
            public Node(FileInfo _fileInfo)
            {
                fileInfo = _fileInfo;
                title = fileInfo.Name;
                var ext = fileInfo.Extension.ToLower();
                //if (ext != ".json")
                    //type = FileType.Default;
            }
        };
        private TreeView treeView = new TreeView();
        private TreeNode? currentNode = null;//当前(正在播放的)曲目，和Selected不同，双击触发
        private DirectoryInfo rootDir;
        private DirectoryInfo favDir;
        private List<Node> nodes = new List<Node>();
        ContextMenuStrip contextMenuStrip = new ContextMenuStrip();
        public PlayListLocalMusic(string _rootDir, MyFileEditEventHandler _begin, MyFileEditEventHandler _end)
        {
            needDelPartButton=false;
            rootDir = new DirectoryInfo(_rootDir);
            favDir = new DirectoryInfo(Config.MusicFavDir);
            Title = rootDir.Name;
            treeView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            treeView.NodeMouseDoubleClick += this.NodeDoubleClicked;
            treeView.NodeMouseClick += this.NodeClicked;
            treeView.AllowDrop = true;
            //treeView.Scrollable = true;//需要设置tabPage.AutoScroll，而不是treeView.Scrollable
            contextMenuStrip.Items.Add("Fav");
            contextMenuStrip.Items.Add("Del");
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
            if (e.Node != null)
                contextMenuStrip.Show(treeView, e.X, e.Y);
        }
        private void ContextMenuClicked(object? sender, ToolStripItemClickedEventArgs e)
        {
            MyFileEditEventArgs tmp_args = new MyFileEditEventArgs();
            if (e.ClickedItem.Text == "Fav")
            {
                RasieFileEditBeginEvent(tmp_args);
                FavNode(treeView.SelectedNode);
                RasieFileEditEndEvent(tmp_args);
            }
            else if (e.ClickedItem.Text == "Del")
            {
                RasieFileEditBeginEvent(tmp_args);
                DeleteNode(treeView.SelectedNode);
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
            var currentNode = this.currentNode!;//注意不使用treeView.selectedNode
            TreeNode nextNode;
            if (offset == 1)
            {
                nextNode = currentNode.NextNode;
                //找到null说明到头了，直接取列表第一项
                if (nextNode == null)
                    nextNode = treeView.Nodes[0];
            }
            else
            {
                nextNode = currentNode.PrevNode;
                //找到null说明到头了，直接取列表第一项
                if (nextNode == null)
                    nextNode = treeView.Nodes[treeView.Nodes.Count - 1];
            }
            this.currentNode = nextNode;
        }
        public void RefreshMainControl()
        {
            treeView.SuspendLayout();
            treeView.Nodes.Clear();
            foreach (var fileNode in this.nodes)
            {
                var rootNode = new TreeNode();
                rootNode.Text = fileNode.title;
                rootNode.Tag = fileNode;
                treeView.Nodes.Add(rootNode);
            }
            treeView.ItemDrag += new ItemDragEventHandler(treeView_ItemDrag);
            treeView.DragEnter += new DragEventHandler(treeView_DragEnter);
            treeView.DragOver += new DragEventHandler(treeView_DragOver);
            treeView.DragDrop += new DragEventHandler(treeView_DragDrop);
            treeView.ResumeLayout();
        }
        private void LoadFiles()
        {
            nodes.Clear();
            if (!rootDir.Exists)
                rootDir.Create();
            var tmp_nodes=new Dictionary<string,Node>();
            var ordered_config=new List<string>();
            //Music下应当仅有数百单个文件，大概不会有性能问题
            foreach (var fileInfo in rootDir.GetFiles())
                if (fileInfo.Name == ConfigFileName)//顺序配置文件
                    using (JsonReader reader = new JsonTextReader(new System.IO.StreamReader(fileInfo.FullName)))
                    {
                        JArray? jsonObject = JToken.ReadFrom(reader) as JArray;
                        if (jsonObject != null)
                            foreach(var pair in jsonObject)
                                ordered_config.Add(pair.ToString());
                    }
                else
                    tmp_nodes[fileInfo.Name]=new Node(fileInfo);
            var ordered_config_set = ordered_config.ToHashSet<string>();
            //把不在配置文件中的即新文件放到最前
            nodes.AddRange(from node_pair in tmp_nodes where !ordered_config_set.Contains(node_pair.Key) select node_pair.Value);
            foreach(var name in ordered_config)
                if(tmp_nodes.ContainsKey(name))
                    nodes.Add(tmp_nodes[name]);
            SaveOrderConfig();
            //此函数是在MainWindow构造函数里开子线程调用的，如果load文件过快，此时可能窗口句柄尚未创建，因此要等待
            while (!treeView.IsHandleCreated) Task.Delay(100);
            treeView.Invoke(() => this.RefreshMainControl());//编辑控件需要在主线程，借用treeView的invoke
        }
        private void SaveOrderConfig()
        {
            using (var writer = new StreamWriter(Path.Combine(rootDir.FullName, ConfigFileName)))
                writer.Write(JsonConvert.SerializeObject(nodes.Select(node => node.fileInfo.Name)));
        }
        private void treeView_ItemDrag(object? sender, ItemDragEventArgs e)
        {
            // Move the dragged node when the left mouse button is used.
            if (e.Button == MouseButtons.Left)
                treeView.DoDragDrop(e.Item, DragDropEffects.Move);
        }

        // Set the target drop effect to the effect 
        // specified in the ItemDrag event handler.
        private void treeView_DragEnter(object? sender, DragEventArgs e)
        {
            e.Effect = e.AllowedEffect;
        }

        // Select the node under the mouse pointer to indicate the 
        // expected drop location.
        private void treeView_DragOver(object? sender, DragEventArgs e)
        {
            // Retrieve the client coordinates of the mouse position.
            Point targetPoint = treeView.PointToClient(new Point(e.X, e.Y));
            // Select the node at the mouse position.
            treeView.SelectedNode = treeView.GetNodeAt(targetPoint);
        }

        private void treeView_DragDrop(object? sender, DragEventArgs e)
        {
            // Retrieve the client coordinates of the drop location.
            Point targetPoint = treeView.PointToClient(new Point(e.X, e.Y));
            TreeNode targetNode = treeView.GetNodeAt(targetPoint);
            TreeNode draggedNode = (TreeNode)e.Data.GetData(typeof(TreeNode));

            if (!draggedNode.Equals(targetNode))
            {
                // If it is a move operation, remove the node from its current 
                // location and add it to the node at the drop location.
                if (e.Effect == DragDropEffects.Move)
                {
                    draggedNode.Remove();
                    treeView.Nodes.Insert(targetNode.Index, draggedNode);
                }
                SaveOrderConfig();
            }
        }
        public override FileInfo? GetCurrentFile()
        {
            if (this.currentNode == null)
                return null;
            //选中叶节点返回文件，选择上级时向下寻找第一个子节点
            var currentNode = this.currentNode!;//注意不使用treeView.SelectedNode
            if (currentNode.Tag is not Node)
                return null;
            return (currentNode.Tag as Node)!.fileInfo;
        }
        //删除选中的节点所在的顶级节点并通知server标记为eliminated
        public void DeleteNode(TreeNode _node)
        {
            var selectedNode = _node;
            if (selectedNode is null)
                return;
            var node = (selectedNode.Tag as Node)!;
            Console.WriteLine($"Delete:{node.title}");
            //如果会删除currentNode则移动CurrentNode
            if (currentNode==selectedNode)
            {
                if (selectedNode.NextNode != null)
                    currentNode = selectedNode.NextNode;
                else if (treeView.Nodes.Count == 0)
                    currentNode = null;
                else
                    currentNode = treeView.Nodes[0];
            }
            treeView.Nodes.Remove(selectedNode);
            var fileInfo = node.fileInfo;
            //仅删除文件(移动到deleted)
            if (fileInfo.Exists)
            {
                var destdir = rootDir.FullName + "/deleted";
                if (!Directory.Exists(destdir))
                    Directory.CreateDirectory(destdir);
                fileInfo.MoveTo($"{destdir}/{fileInfo.Name}");
            }
        }
        public void FavNode(TreeNode _node)
        {
            if (favDir.FullName == rootDir.FullName)//当前根目录就是favDir
                return;
            var selectedNode = _node;
            if (selectedNode is null)
                return;
            var node = (selectedNode.Tag as Node)!;
            Console.WriteLine($"Fav:{node.fileInfo}");
            try
            {
                var fileInfo = node.fileInfo;
                var dest = $"{favDir.FullName}/{fileInfo.Name}";
                if (!File.Exists(dest))
                    fileInfo.CopyTo(dest);
                //Fav时不删除原文件
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                throw;
            }
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
            var curNode = currentNode;
            if (curNode is null)
                return desc;
            var node = (curNode.Tag as Node)!;
            desc += node.title + "\n";
            return desc;
        }
        public override void OpenLocalSelected()
        {
            var selectedNode = treeView.SelectedNode;
            if (selectedNode is null)
                return;
            var dir = (selectedNode.Tag as Node)!.fileInfo.Directory;
            if (dir == null)
                return ;
            if (!dir.Exists)
                return;
            //更新net版本后UseShellExecute默认值变为false导致无法打开url，需要指定为true
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(dir.FullName) { UseShellExecute = true });
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

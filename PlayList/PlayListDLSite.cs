using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MyAudioPlayer.PlayList
{
    internal class PlayListDLSite: PlayListBase
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
                else if (ext == ".mp3" || ext == ".mp4")
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
            public List<AFileSet> fileSets= new List<AFileSet>();
        };
        public static Regex workNameRegex =new Regex("^[RVBJ]{0,2}[0-9]{3,8}");
        private TreeView treeView=new TreeView();
        private TreeNode? mySelectedNode=null;//因为需要双击选中曲目，忽略treeView自己的selectedNode
        private DirectoryInfo rootDir;
        private List<Node> nodes=new List<Node>();
        public PlayListDLSite(string _rootDir)
        {
            rootDir = new DirectoryInfo(_rootDir);
            Title = "DL-"+rootDir.Name;
            LoadFiles();
            treeView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            treeView.NodeMouseDoubleClick += this.SelectedIndexChanged;
        }
        //TODO: 添加搜索栏
        public override Control GetMainControl()
        {
            return treeView;
        }
        private void SelectedIndexChanged(object? sender, TreeNodeMouseClickEventArgs e)
        {
            mySelectedNode = treeView.SelectedNode;
        }
        public override void MountSelectedChangeEvent(TreeNodeMouseClickEventHandler handler)
        {
            treeView.NodeMouseDoubleClick += handler;
        }
        public override void UnmountSelectedChangeEvent(TreeNodeMouseClickEventHandler handler)
        {
            treeView.NodeMouseDoubleClick -= handler;
        }
        public override void Move(int offset) 
        {
            if(Math.Abs(offset) !=1)
                throw new NotImplementedException();
            if(mySelectedNode==null)//如果没有选中任何节点，选中第一个
            {
                if (treeView.Nodes.Count == 0)
                    return;
                mySelectedNode = treeView.SelectedNode = treeView.Nodes[0];
                return;
            }
            //先找到当前叶节点
            var currentNode = mySelectedNode!;//注意不使用treeView.selectedNode
            while(currentNode.Nodes.Count>0)//
                currentNode = currentNode.Nodes[0];
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
                    nextNode = treeView.Nodes[treeView.Nodes.Count-1];
                else
                    nextNode = parentNode.PrevNode;
            }
            mySelectedNode=treeView.SelectedNode = nextNode;
        }
        public override void RefreshMainControl() 
        {
            treeView.SuspendLayout();
            treeView.Scrollable = true;
            treeView.Nodes.Clear();
            foreach(var fileNode in this.nodes)
            {
                var rootNode=new TreeNode();
                rootNode.Text = fileNode.title;
                rootNode.Tag= fileNode;
                foreach(var fileSet in fileNode.fileSets)
                {
                    var secondNode = new TreeNode();
                    secondNode.Text = fileSet.title;
                    secondNode.Tag = fileSet;
                    foreach(var file in fileSet.files)
                    {
                        var leafNode=new TreeNode();
                        leafNode.Text= file.title;
                        leafNode.Tag= file;
                        secondNode.Nodes.Add(leafNode);
                    }
                    rootNode.Nodes.Add(secondNode);
                }
                treeView.Nodes.Add(rootNode);
                //break;
            }
            treeView.ResumeLayout();
        }
        private void LoadFiles()
        {
            nodes.Clear();
            LoadFilesImpl(rootDir);
            //to do:sort
        }
        private void LoadFilesImpl(DirectoryInfo dir)
        {            
            foreach (var fileInfo in dir.GetFiles())
            {
                var file = new AFile(fileInfo);
                if(file.type!= AFile.FileType.OTHER)
                {
                    var node=new Node();
                    node.type = Node.NodeType.SingleFile;
                    node.title=file.title;
                    node.rootRir = dir;
                    node.fileSets.Add(new AFileSet {title=node.title,files=new List<AFile> { file} });
                    nodes.Add(node);
                }
            }
            foreach (var dirInfo in dir.GetDirectories())
                if(workNameRegex.IsMatch(dirInfo.Name))
                {
                    var node = new Node();
                    string workId = workNameRegex.Match(dirInfo.Name).Groups[0].Value;
                    node.title = dirInfo.Name;
                    node.RJ = workId;
                    node.type = Node.NodeType.DLSite;
                    node.rootRir = dirInfo;
                    //每个目录下每种类型分别放到一个set里
                    //set按目录名排序，set内按文件名排序
                    {
                        var subDirs=new List<DirectoryInfo>();
                        subDirs.Add(dirInfo);
                        for(int i=0;i<subDirs.Count;i++)
                            subDirs.AddRange(subDirs[i].GetDirectories());
                        subDirs.Sort((l,r)=>l.FullName.CompareTo(r.FullName));

                        foreach(var subDir in subDirs)
                        {
                            var fileSets=new Dictionary<AFile.FileType, AFileSet>();
                            foreach(var fileInfo in subDir.GetFiles())
                            {
                                var file=new AFile(fileInfo);
                                var afileType = file.type;
                                if (afileType == AFile.FileType.OTHER)
                                    continue;
                                if(!fileSets.ContainsKey(afileType))
                                    fileSets.Add(afileType, new AFileSet { title = subDir.Name+"_"+(fileSets.Count+1).ToString(), files = new List<AFile> {} });
                                fileSets[afileType].files.Add(file);
                            }
                            foreach (var fileSet in fileSets.Values)
                            {
                                fileSet.files.Sort((l,r)=>l.title.CompareTo(r.title));
                                node.fileSets.Add(fileSet);
                            }
                        }
                    }
                    nodes.Add(node);
                }
        }
        public void OnDelete()
        {

        }

        public override FileInfo? GetCurrentFile()
        {
            //选中叶节点返回文件，选择上级时向下寻找第一个子节点
            if (treeView.SelectedNode.Tag as AFile != null)
                return (treeView.SelectedNode.Tag as AFile)!.fileInfo;
            else if (treeView.SelectedNode.Tag as AFileSet != null)
            {
                var files = (treeView.SelectedNode.Tag as AFileSet)!.files;
                if (files.Count == 0)
                    return null;
                return files.First().fileInfo;
            }
            else if(treeView.SelectedNode.Tag as Node != null)
            {
                var fileSet = (treeView.SelectedNode.Tag as Node)!.fileSets;
                if (fileSet.Count == 0)//不会为null
                    return null;
                var files = fileSet.First().files;
                if (files.Count == 0)
                    return null;
                return files.First().fileInfo;
            }
            return null;
        }
    }
}

using MyAudioPlayer.PlayList;
using System.Text;
using NAudio;
using NAudio.Wave;

namespace MyAudioPlayer
{
    public partial class MainWindow : Form
    {
        private List<PlayListBase> playLists=new List<PlayListBase>();
        public static string TreeViewName = "MyTreeView123";
        public FileInfo? currentFile=null;
        public WaveOutEvent audioDevice=new WaveOutEvent();
        public AudioFileReader currentFileReader;
        public bool noTriggerPlayStoppedEvent = false;
        public MainWindow()
        {
            InitializeComponent();
            {
                PlayListTab.SelectedIndexChanged += this.OnCurrentPlayListChanged;
                playButton.Click += this.OnPlayButtonClicked;
                pauseButton.Click += this.OnPauseButtonClicked;
                prevButton.Click += this.OnPrevButtonClicked;
                nextButton.Click += this.OnNextButtonClicked;
            }
            {
                audioDevice.PlaybackStopped += this.OnPlayStopped;
            }
            InitPlayListTree();
        }
        void OnPlayButtonClicked(object? sender, EventArgs e)
        {
            PlayCurrentFile();
        }
        void OnPrevButtonClicked(object? sender, EventArgs e)
        {
            playLists[PlayListTab.SelectedIndex].Move(-1);
            ReLoadCurrentFile();
        }
        void OnNextButtonClicked(object? sender, EventArgs e)
        {
            playLists[PlayListTab.SelectedIndex].Move(1);
            ReLoadCurrentFile();
        }
        void PlayCurrentFile()
        {
            if (currentFile == null)
                return;
            if(currentFileReader==null||currentFileReader.FileName!=currentFile.ToString())
            {
                noTriggerPlayStoppedEvent = true;
                audioDevice.Stop();
                if(currentFileReader!=null)
                    currentFileReader.Dispose();
                currentFileReader = new AudioFileReader(currentFile!.ToString());
                audioDevice.Init(currentFileReader);
                noTriggerPlayStoppedEvent=false;
            }
            audioDevice.Play();
        }
        void ReLoadCurrentFile()
        {
            currentFile = playLists[PlayListTab.SelectedIndex].GetCurrentFile();
            if (audioDevice.PlaybackState == PlaybackState.Playing)
                PlayCurrentFile();
            titleBox.Text= currentFile!.ToString();
        }
        void OnPauseButtonClicked(object? sender, EventArgs e)
        {
            audioDevice.Pause();
        }
        void OnPlayStopped(object? sender, EventArgs e)
        {
            if (noTriggerPlayStoppedEvent)
                return;
            //下一曲
            playLists[PlayListTab.SelectedIndex].Move(1);
            ReLoadCurrentFile();
        }
        void OnCurrentPlayListChanged(object? sender, EventArgs e)
        {
            int currentIndex=PlayListTab.SelectedIndex;
            if(currentIndex>=0&&currentIndex<=playLists.Count)
            {
                playLists[currentIndex].RefreshMainControl();
                //令当前选项卡选择曲目时触发该控件响应事件
                foreach (var playList in playLists)
                    playList.UnmountSelectedChangeEvent(this.PlayList_SelectedIndexChanged);
                playLists[currentIndex].MountSelectedChangeEvent(this.PlayList_SelectedIndexChanged);
                //暂定：不触发PlayList_SelectedIndexChanged，即继续播放之前的曲目
            }
            else
                throw new Exception("Unknown UI Problem");
        }
        public void InitPlayListTree()
        {
            Config.LoadJson();
            foreach(var pair in Config.playLists)
                if(pair.Key == typeof(PlayListDLSite).Name)
                    playLists.Add(new PlayListDLSite(pair.Value));
            PlayListTab.SuspendLayout();
            foreach(var playList in playLists)
            {
                var tabPage = new TabPage();
                tabPage.Text = playList.Title;
                tabPage.BorderStyle = BorderStyle.Fixed3D;
                //tabpage里的控件右侧到tabpage右边缘会有一段空白，宽度等于tabPage.Width，所以将width设为0规避,为什么会这样？                
                tabPage.Width = 0;
                tabPage.Controls.Add(playList.GetMainControl());
                PlayListTab.TabPages.Add(tabPage);
            }
            PlayListTab.ResumeLayout(false);
            if (playLists.Count > 0)
            {
                PlayListTab.SelectTab(0);
                OnCurrentPlayListChanged(null,new EventArgs());
            }
       }

        private void PlayList_SelectedIndexChanged(object? sender, TreeNodeMouseClickEventArgs e)
        {
            int currentIndex = PlayListTab.SelectedIndex;
            if (currentIndex < 0 || currentIndex >= playLists.Count)
                return;
            ReLoadCurrentFile();
            this.Refresh();
        }
    }
}
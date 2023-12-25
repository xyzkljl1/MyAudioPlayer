using MyAudioPlayer.PlayList;
using System.Text;
using NAudio;
using NAudio.Wave;
using Timer = System.Timers.Timer;
using System.Timers;

namespace MyAudioPlayer
{
    public partial class MainWindow : Form
    {
        private List<PlayListBase> playLists=new List<PlayListBase>();
        public static string TreeViewName = "MyTreeView123";
        public FileInfo? currentFile=null;
        public WaveOutEvent audioDevice=new WaveOutEvent();
        public AudioFileReader? currentFileReader;
        public bool noTriggerPlayStoppedEvent = false;
        public Timer timer;
        public MainWindow()
        {
            InitializeComponent();
            {//进度条timer
                timer = new Timer(300); //milliseconds
                timer.Enabled = true;
                timer.AutoReset = true;
                timer.Elapsed += this.OnPlayTimerTick;
            }
            {
                PlayListTab.SelectedIndexChanged += this.OnCurrentPlayListChanged;
                PlayButton.Click += this.OnPlayButtonClicked;
                PauseButton.Click += this.OnPauseButtonClicked;
                PrevButton.Click += this.OnPrevButtonClicked;
                NextButton.Click += this.OnNextButtonClicked;
                DelButton.Click += this.OnDelButtonClicked;
                DelPartButton.Click += this.OnDelPartButtonClicked;
                FavButton.Click += this.OnFavButtonClicked;
                SelectCurrentButton.Click += delegate { playLists[PlayListTab.SelectedIndex].SelectCurrent(); };
                OpenLocalButton.Click += delegate { playLists[PlayListTab.SelectedIndex].OpenLocalSelected(); };
                OpenWebButton.Click += delegate { playLists[PlayListTab.SelectedIndex].OpenWebSelected(); };
                playSlider.ValueChanged += OnSliderValueChanged;
                //防止拖动进度条时重设进度条
                playSlider.MouseDown += delegate { timer.Enabled = false; };
                playSlider.MouseUp += delegate { timer.Enabled = true; };
                playSlider.Scroll += OnSliderScrolled;
                volumeSlider.Scroll += OnVolumeSliderScrolled;
                volumeSlider.Value = 100;
            }
            {
                MountPlayStopEvent();
            }
            InitPlayListTree();
        }
        void OnPlayTimerTick(object? sender, ElapsedEventArgs e)
        {
            if (currentFile != null)//timer触发的响应不在主线程，需要用invoke
                this.Invoke(() => 
                        { 
                            if(currentFileReader is not null)
                                this.playSlider.Value = (int)Math.Floor(currentFileReader.CurrentTime.TotalSeconds);
                        });
        }
        //slider因任何原因产生变化时，修改label
        void OnSliderValueChanged(object? sender, EventArgs e)
        {
            int sec = playSlider.Value;
            this.sliderLabel.Text = $"{sec / 60}:{(sec % 60).ToString().PadLeft(2, '0')}";
        }
        //用户拖动进度条结束时，改变播放进度
        void OnSliderScrolled(object? sender, EventArgs e)
        {
            if (currentFile is null || currentFileReader is null)
                return;
            int sec = playSlider.Value;
            currentFileReader.CurrentTime =new TimeSpan(sec*TimeSpan.TicksPerSecond);
        }
        void OnVolumeSliderScrolled(object? sender, EventArgs e)
        {
            //范围0-1
            audioDevice.Volume = Math.Clamp(volumeSlider.Value/100.0f,.0f,1.0f);
        }
        void OnPlayButtonClicked(object? sender, EventArgs e)
        {
            Play();
        }
        void OnPrevButtonClicked(object? sender, EventArgs e)
        {
            playLists[PlayListTab.SelectedIndex].MoveCurrent(-1);
            ReloadCurrentFile();
        }
        void OnNextButtonClicked(object? sender, EventArgs e)
        {
            playLists[PlayListTab.SelectedIndex].MoveCurrent(1);
            ReloadCurrentFile();
        }
        void UnmountPlayStopEvent()
        {
            audioDevice.PlaybackStopped -= this.OnPlayStopped;
        }
        void MountPlayStopEvent()
        {
            audioDevice.PlaybackStopped += this.OnPlayStopped;
        }
        void OnFileEditBegin(object? sender, MyFileEditEventArgs e)
        {
            if (audioDevice.PlaybackState != PlaybackState.Playing)
                return;
            //因为可能会删除当前文件，需要先stop
            //如果之前正在播放，且执行完后仍然继续播放，如果文件未改变则从之前的位置开始播放
            e.needContinue = true;
            e.prevFile = currentFile;
            e.prevPosition = currentFileReader!.Position;
            Stop(true);
        }
        void OnFileEditEnd(object? sender, MyFileEditEventArgs e)
        {
            if (!e.needContinue)
                return;
            ReloadCurrentFile();
            if (currentFile == e.prevFile)
                currentFileReader!.Position = e.prevPosition;
            Play();
        }
        void OnDelButtonClicked(object? sender, EventArgs e)
        {
            if (audioDevice.PlaybackState != PlaybackState.Playing)
                playLists[PlayListTab.SelectedIndex].DeleteCurrent();
            else
            {
                var arg = new MyFileEditEventArgs();
                OnFileEditBegin(null,arg);
                playLists[PlayListTab.SelectedIndex].DeleteCurrent();
                OnFileEditEnd(null, arg);
            }
        }
        void OnDelPartButtonClicked(object? sender, EventArgs e)
        {
            if (audioDevice.PlaybackState != PlaybackState.Playing)
                playLists[PlayListTab.SelectedIndex].DeleteCurrentPart();
            else
            {
                var arg = new MyFileEditEventArgs();
                OnFileEditBegin(null, arg);
                playLists[PlayListTab.SelectedIndex].DeleteCurrentPart();
                OnFileEditEnd(null, arg);
            }
        }
        void OnFavButtonClicked(object? sender, EventArgs e)
        {
            if(audioDevice.PlaybackState != PlaybackState.Playing)
                playLists[PlayListTab.SelectedIndex].FavCurrent();
            else
            {   
                var arg = new MyFileEditEventArgs();
                OnFileEditBegin(null, arg);
                playLists[PlayListTab.SelectedIndex].FavCurrent();
                OnFileEditEnd(null, arg);
            }
        }
        void Stop(bool releaseFileReader=false)
        {
            UnmountPlayStopEvent();
            audioDevice.Stop();
            //关闭fileReader会触发playstop事件所以需要放在此处
            if(releaseFileReader)
                if(currentFileReader != null)
                {
                    currentFileReader.Close();
                    currentFileReader.Dispose();
                    currentFileReader = null;
                }
            MountPlayStopEvent();
        }
        void Play()
        {
            if (currentFile == null)
                return;
            audioDevice.Play();
        }
        void ReloadCurrentFile()
        {
            bool playing = audioDevice.PlaybackState == PlaybackState.Playing;
            //从playlist获取当前文件
            currentFile = playLists[PlayListTab.SelectedIndex].GetCurrentFile();
            if(currentFile == null)
            {
                Stop(true);
                titleBox.Text = "None";
                return;
            }
            //重设device
            if (currentFileReader == null || currentFileReader.FileName != currentFile!.ToString())
            {
                Stop(true);
                try
                {
                    currentFileReader = new AudioFileReader(currentFile!.ToString());
                    audioDevice.Init(currentFileReader);
                }
                catch (System.Runtime.InteropServices.COMException e)//因为文件有问题产生的异常？如RJ01045015的限定ボーナス02.mp4
                {
                    MessageBox.Show($"Invalid File Cause Exception:{currentFile.FullName}/{e.Message}");
                    currentFileReader =  null;
                    currentFile = null;
                    return;
                }
            }
            //显示信息
            titleBox.Text = playLists[PlayListTab.SelectedIndex].GetCurrentFileDesc();
            this.playSlider.Minimum= 0;
            this.playSlider.Maximum=(int)Math.Floor(currentFileReader.TotalTime.TotalSeconds);
            this.playSlider.Value = (int)Math.Floor(currentFileReader.CurrentTime.TotalSeconds);
            //如果之前在播放则继续播放
            if (playing)
                Play();
        }
        void OnPauseButtonClicked(object? sender, EventArgs e)
        {
            audioDevice.Pause();
        }
        void OnPlayStopped(object? sender, EventArgs e)
        {
            if (noTriggerPlayStoppedEvent)
                return;
            //由audioDevice触发，不在主线程，需要用invoke
            this.Invoke(delegate ()
            {
                //下一曲
                playLists[PlayListTab.SelectedIndex].MoveCurrent(1);
                ReloadCurrentFile();
                Play();
            });
        }
        void OnCurrentPlayListChanged(object? sender, EventArgs e)
        {
            int currentIndex=PlayListTab.SelectedIndex;
            if(currentIndex>=0&&currentIndex<=playLists.Count)
            {
                //令当前选项卡选择曲目时触发该控件响应事件
                foreach (var playList in playLists)
                    playList.UnmountDoubleClickEvent(this.PlayList_DoubleClicked);
                playLists[currentIndex].MountDoubleClickEvent(this.PlayList_DoubleClicked);
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
                    playLists.Add(new PlayListDLSite(pair.Value, this.OnFileEditBegin, this.OnFileEditEnd));
            PlayListTab.SuspendLayout();
            foreach(var playList in playLists)
            {
                var tabPage = new TabPage();
                tabPage.AutoScroll = true;
                tabPage.Text = playList.Title;
                tabPage.BorderStyle = BorderStyle.Fixed3D;
                //tabpage里的控件右侧到tabpage右边缘会有一段空白，宽度约等于tabPage.Width-120，将tabPage.Width设为120规避(小于120会导致滚动条被挡住)，为什么会这样？                
                tabPage.Width = 120;
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

        private void PlayList_DoubleClicked(object? sender, TreeNodeMouseClickEventArgs e)
        {
            int currentIndex = PlayListTab.SelectedIndex;
            if (currentIndex < 0 || currentIndex >= playLists.Count)
                return;
            ReloadCurrentFile();
            Play();
            this.Refresh();
        }
        protected override void OnClosed(EventArgs e)
        {
            timer.Stop();//防止关闭窗口后timer还触发事件导致异常
            base.OnClosed(e);
        }
    }
}
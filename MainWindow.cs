using MyAudioPlayer.PlayList;
using System.Text;
using Timer = System.Timers.Timer;
using System.Timers;
using System.Runtime.InteropServices;
using MyAudioPlayer.Player;

namespace MyAudioPlayer
{
    public partial class MainWindow : Form
    {
        private List<PlayListBase> playLists = new List<PlayListBase>();
        public bool noTriggerPlayStoppedEvent = false;
        public Timer timer;
        private Size prevWindowSize;
        private Point prevLocation;
        //If necessary:创建多个player，根据文件格式选择使用哪个
        //private ManagedBassPlayer BassPlayer=new ManagedBassPlayer();
        private BasePlayer CurrentPlayer=new ManagedBassPlayer();

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
                toolTip.SetToolTip(this.SelectCurrentButton, "选中正在播放(Current)的条目");
                toolTip.SetToolTip(this.FavButton, "Fav Current");
                toolTip.SetToolTip(this.DelButton, "Delete Current");
                toolTip.SetToolTip(this.FavButton, "Delete Current Part");
            }
            {
                MountPlayStopEvent();
            }
            InitPlayListTree();
            RefreshPlayButton();
        }
        void OnPlayTimerTick(object? sender, ElapsedEventArgs e)
        {
            if (CurrentPlayer.IsLoaded())//timer触发的响应不在主线程，需要用invoke
                this.Invoke(() =>this.playSlider.Value = CurrentPlayer.GetCurrentPositionSec());
        }
        //slider因任何原因产生变化时，修改label
        void OnSliderValueChanged(object? sender, EventArgs e)
        {
            int sec = playSlider.Value;
            int total = playSlider.Maximum;
            this.sliderLabel.Text = $"{sec / 60}:{(sec % 60).ToString().PadLeft(2, '0')} / {total / 60}:{(total % 60).ToString().PadLeft(2, '0')}";
        }
        //用户拖动进度条结束时，改变播放进度
        void OnSliderScrolled(object? sender, EventArgs e)
        {
            if (!CurrentPlayer.IsLoaded())
                return;
            int sec = playSlider.Value;
            CurrentPlayer.SetCurrentPositionSec(sec);
        }
        void OnVolumeSliderScrolled(object? sender, EventArgs e)
        {
            //范围0-1
            CurrentPlayer.SetVolume(Math.Clamp(volumeSlider.Value / 100.0f, .0f, 1.0f));
        }
        void OnPlayButtonClicked(object? sender, EventArgs e)
        {
            if (!CurrentPlayer.IsLoaded())
                return;
            if (CurrentPlayer.IsPlaying())
                Pause();
            else
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
            CurrentPlayer.PlayStopped -= this.OnPlayStopped;
        }
        void MountPlayStopEvent()
        {
            CurrentPlayer.PlayStopped += this.OnPlayStopped;
        }
        void OnFileEditBegin(object? sender, MyFileEditEventArgs e)
        {
            //ManagedBass不锁文件，不需要stop
            if (CurrentPlayer.IsPlaying())
                e.needContinue = true;
        }
        void OnFileEditEnd(object? sender, MyFileEditEventArgs e)
        {
            ReloadCurrentFile();
            if (e.needContinue)
                Play();
        }
        void OnDelButtonClicked(object? sender, EventArgs e)
        {
            if (!CurrentPlayer.IsLoaded())
                playLists[PlayListTab.SelectedIndex].DeleteCurrent();
            else
            {
                var arg = new MyFileEditEventArgs();
                OnFileEditBegin(null, arg);
                playLists[PlayListTab.SelectedIndex].DeleteCurrent();
                OnFileEditEnd(null, arg);
            }
        }
        void OnDelPartButtonClicked(object? sender, EventArgs e)
        {
            if (!CurrentPlayer.IsLoaded())
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
            if (!CurrentPlayer.IsLoaded())
                playLists[PlayListTab.SelectedIndex].FavCurrent();
            else
            {
                var arg = new MyFileEditEventArgs();
                OnFileEditBegin(null, arg);
                playLists[PlayListTab.SelectedIndex].FavCurrent();
                OnFileEditEnd(null, arg);
            }
        }
        void Stop()
        {
            UnmountPlayStopEvent();
            CurrentPlayer.Stop();
            MountPlayStopEvent();
        }
        void Play()
        {
            CurrentPlayer.Play();
            RefreshPlayButton();
        }
        void Pause()
        {
            CurrentPlayer.Pause();
            RefreshPlayButton();
        }
        void RefreshPlayButton()
        {
            //https://stackoverflow.com/questions/22885702/html-for-the-pause-symbol-in-audio-and-video-control
            if (CurrentPlayer.IsPlaying())
            {
                PlayButton.Text = "⏸︎";
                //PlayButton.Font = new Font(PlayButton.Font.FontFamily, 14F, FontStyle.Bold);
            }
            else
            {
                PlayButton.Text = "▶";
                //PlayButton.Font = new Font(PlayButton.Font.FontFamily, 14F, FontStyle.Regular);
            }
        }

        void ReloadCurrentFile()
        {
            bool playing = CurrentPlayer.IsPlaying();
            //从playlist获取当前文件
            var currentFile = playLists[PlayListTab.SelectedIndex].GetCurrentFile();
            if (currentFile == null)
            {
                Stop();
                titleBox.Text = "None";
                return;
            }
            if (currentFile!=CurrentPlayer.CurrentFile)
            {
                Stop();
                try
                {
                    CurrentPlayer.Reload(currentFile);
                }
                catch (Exception e)//也是文件有问题？如RJ01003442
                {
                    MessageBox.Show($"Invalid File Cause Exception:{currentFile.FullName}/{e.Message}");
                    return;
                }
            }
            //显示信息
            titleBox.Text = playLists[PlayListTab.SelectedIndex].GetCurrentFileDesc();
            this.playSlider.Minimum = 0;
            this.playSlider.Maximum = CurrentPlayer.GetTotalLengthSec();
            this.playSlider.Value = CurrentPlayer.GetCurrentPositionSec();
            //如果之前在播放则继续播放
            if (playing)
                Play();
        }
        void OnPauseButtonClicked(object? sender, EventArgs e)
        {
            CurrentPlayer.Pause();
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
            int currentIndex = PlayListTab.SelectedIndex;
            if (currentIndex >= 0 && currentIndex <= playLists.Count)
            {
                //令当前选项卡选择曲目时触发该控件响应事件
                foreach (var playList in playLists)
                    playList.UnmountDoubleClickEvent(this.PlayList_DoubleClicked);
                playLists[currentIndex].MountDoubleClickEvent(this.PlayList_DoubleClicked);
                DelPartButton.Enabled = playLists[currentIndex].needDelPartButton;
                OpenWebButton.Enabled = playLists[currentIndex].needWebButton;
                //暂定：不触发PlayList_SelectedIndexChanged，即继续播放之前的曲目
            }
            else
                throw new Exception("Unknown UI Problem");
        }
        public void InitPlayListTree()
        {
            Config.LoadJson();
            foreach (var pair in Config.playLists)
                if (pair.Key == typeof(PlayListDLSite).Name)
                    playLists.Add(new PlayListDLSite(pair.Value, this.OnFileEditBegin, this.OnFileEditEnd));
                else if (pair.Key == typeof(PlayListLocalMusic).Name)
                    playLists.Add(new PlayListLocalMusic(pair.Value, this.OnFileEditBegin, this.OnFileEditEnd));

            PlayListTab.SuspendLayout();
            foreach (var playList in playLists)
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
                OnCurrentPlayListChanged(null, new EventArgs());
            }
        }
        private void SwitchToBar(bool _toBar)
        {
            isBar = _toBar;
            this.SuspendLayout();

            UpPanel.Visible = !isBar;
            //UpPanel.MinimumSize =new Size(0,0);
            //UpPanel.Dock = DockStyle.None;
            //UpPanel.Size = new Size(0,0);
            DownPanel.Visible = !isBar;
            //this.ControlBox = !isBar;
            //flowLayout实在用不明白，只好用tableLayout手动调行高了
            mainTableLayoutPanel.RowStyles[0].Height = isBar ? 0 : 190F;
            //mainTableLayoutPanel.RowStyles[1].Height = 101F;
            mainTableLayoutPanel.RowStyles[2].Height = isBar ? 0 : 618F;

            foreach (var btn in new List<Button> { PlayButton, PrevButton, NextButton, FavButton, SelectCurrentButton, DelButton, DelPartButton })
                btn.Font = new Font(btn.Font.FontFamily, isBar ? 12 : 24);

            foreach (var btn in new List<Button> { FavButton, SelectCurrentButton, DelButton, DelPartButton })
            {
                btn.Width = isBar ? 54 : 84;
                btn.Height = isBar ? 54 : 87;
            }

            MiddlePanel.Height = isBar ? 80 : 95;
            this.FormBorderStyle = isBar ? System.Windows.Forms.FormBorderStyle.None : System.Windows.Forms.FormBorderStyle.Sizable;
            this.ControlBox = !isBar;
            //控件多(加载列表里的文件后)时set Text会耗费很长时间，why？？？？？？
            //this.Text = isBar ? String.Empty : "万万静听";
            this.LockCheckBox.Visible = isBar;
            this.TopMost = isBar ? LockCheckBox.Checked : false;
            if (_toBar)
            {
                prevWindowSize = this.Size;
                prevLocation = this.Location;
                this.Height = 80;
                this.Width = 850;
                this.Location = new Point((Screen.PrimaryScreen.Bounds.Width - this.Width) / 2, 0);
            }
            else
            {
                this.Location = prevLocation;
                this.Size = prevWindowSize;
            }
            ResumeLayout();
        }
        private void PlayList_DoubleClicked(object? sender, TreeNodeMouseClickEventArgs e)
        {
            int currentIndex = PlayListTab.SelectedIndex;
            if (currentIndex < 0 || currentIndex >= playLists.Count)
                return;
            ReloadCurrentFile();
            Play();
            Refresh();
        }

        protected override void OnClosed(EventArgs e)
        {
            timer.Stop();
            timer.Elapsed -= this.OnPlayTimerTick;//防止关闭窗口后timer还触发事件导致异常
            base.OnClosed(e);
        }
        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MINIMIZE = 0xf020;
        private const int SC_MOVE = 0xf010;
        private const int HTCAPTION = 0x0002;
        private bool isBar = false;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_SYSCOMMAND)
            {
                if (m.WParam.ToInt32() == SC_MINIMIZE)
                {
                    //m.Result = IntPtr.Zero;
                    //SwitchToBar(!isBar);
                    //return;
                }
            }
            base.WndProc(ref m);
        }
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        public static extern bool SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);

        //应该令所有panel忽略鼠标事件传给parent，但是不知道怎么实现
        //目前是把MiddlePanel、MiddlePanelFlowLayoutPanel、MainWindow的鼠标事件全挂在同一个函数上
        private void onMainWindowMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && e.Clicks == 1)//double click也会先触发mousedown,需要判断是否是单击左键
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_SYSCOMMAND, SC_MOVE | HTCAPTION, 0);
            }
        }

        private void onMainWindowMouseDoubleClick(object sender, EventArgs e)
        {
            SwitchToBar(!isBar);
        }

        private void onMainWindowMove(object sender, EventArgs e)
        {
            //会闪烁，如何解决？
            if (isBar)
                this.Location = new Point(this.Location.X, 0);
        }

        private void onLockChanged(object sender, EventArgs e)
        {
            this.TopMost = isBar ? LockCheckBox.Checked : false;
        }
    }
}
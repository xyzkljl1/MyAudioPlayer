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
            {//������timer
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
                //��ֹ�϶�������ʱ���������
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
            if (currentFile != null)//timer��������Ӧ�������̣߳���Ҫ��invoke
                this.Invoke(() => 
                        { 
                            if(currentFileReader is not null)
                                this.playSlider.Value = (int)Math.Floor(currentFileReader.CurrentTime.TotalSeconds);
                        });
        }
        //slider���κ�ԭ������仯ʱ���޸�label
        void OnSliderValueChanged(object? sender, EventArgs e)
        {
            int sec = playSlider.Value;
            this.sliderLabel.Text = $"{sec / 60}:{(sec % 60).ToString().PadLeft(2, '0')}";
        }
        //�û��϶�����������ʱ���ı䲥�Ž���
        void OnSliderScrolled(object? sender, EventArgs e)
        {
            if (currentFile is null || currentFileReader is null)
                return;
            int sec = playSlider.Value;
            currentFileReader.CurrentTime =new TimeSpan(sec*TimeSpan.TicksPerSecond);
        }
        void OnVolumeSliderScrolled(object? sender, EventArgs e)
        {
            //��Χ0-1
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
            //��Ϊ���ܻ�ɾ����ǰ�ļ�����Ҫ��stop
            //���֮ǰ���ڲ��ţ���ִ�������Ȼ�������ţ�����ļ�δ�ı����֮ǰ��λ�ÿ�ʼ����
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
            //�ر�fileReader�ᴥ��playstop�¼�������Ҫ���ڴ˴�
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
            //��playlist��ȡ��ǰ�ļ�
            currentFile = playLists[PlayListTab.SelectedIndex].GetCurrentFile();
            if(currentFile == null)
            {
                Stop(true);
                titleBox.Text = "None";
                return;
            }
            //����device
            if (currentFileReader == null || currentFileReader.FileName != currentFile!.ToString())
            {
                Stop(true);
                try
                {
                    currentFileReader = new AudioFileReader(currentFile!.ToString());
                    audioDevice.Init(currentFileReader);
                }
                catch (System.Runtime.InteropServices.COMException e)//��Ϊ�ļ�������������쳣����RJ01045015���޶��ܩ`�ʥ�02.mp4
                {
                    MessageBox.Show($"Invalid File Cause Exception:{currentFile.FullName}/{e.Message}");
                    currentFileReader =  null;
                    currentFile = null;
                    return;
                }
            }
            //��ʾ��Ϣ
            titleBox.Text = playLists[PlayListTab.SelectedIndex].GetCurrentFileDesc();
            this.playSlider.Minimum= 0;
            this.playSlider.Maximum=(int)Math.Floor(currentFileReader.TotalTime.TotalSeconds);
            this.playSlider.Value = (int)Math.Floor(currentFileReader.CurrentTime.TotalSeconds);
            //���֮ǰ�ڲ������������
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
            //��audioDevice�������������̣߳���Ҫ��invoke
            this.Invoke(delegate ()
            {
                //��һ��
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
                //�ǰѡ�ѡ����Ŀʱ�����ÿؼ���Ӧ�¼�
                foreach (var playList in playLists)
                    playList.UnmountDoubleClickEvent(this.PlayList_DoubleClicked);
                playLists[currentIndex].MountDoubleClickEvent(this.PlayList_DoubleClicked);
                //�ݶ���������PlayList_SelectedIndexChanged������������֮ǰ����Ŀ
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
                //tabpage��Ŀؼ��Ҳൽtabpage�ұ�Ե����һ�οհף����Լ����tabPage.Width-120����tabPage.Width��Ϊ120���(С��120�ᵼ�¹���������ס)��Ϊʲô��������                
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
            timer.Stop();//��ֹ�رմ��ں�timer�������¼������쳣
            base.OnClosed(e);
        }
    }
}
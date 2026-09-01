using MyAudioPlayer.PlayList;
using System.Text;
using System.Drawing.Drawing2D;
using Svg;
using Timer = System.Timers.Timer;
using System.Timers;
using System.Runtime.InteropServices;
using MyAudioPlayer.Player;
using MyAudioPlayer.Themes;

namespace MyAudioPlayer
{
    public partial class MainWindow : Form
    {
        private List<PlayListBase> playLists = new List<PlayListBase>();
        public bool noTriggerPlayStoppedEvent = false;
        public Timer timer;
        //If necessary:创建多个player，根据文件格式选择使用哪个
        //private ManagedBassPlayer BassPlayer=new ManagedBassPlayer();
        private BasePlayer CurrentPlayer=new ManagedBassPlayer();
        private const int NormalIconSize = 46;
        private PlayerTheme currentTheme = PlayerThemes.Resolve(PlayerThemes.DefaultId);
        private readonly ContextMenuStrip themeMenuStrip = new ContextMenuStrip();
        private readonly Dictionary<Button, ButtonVisualStyle> buttonStyles = new Dictionary<Button, ButtonVisualStyle>();
        private readonly HashSet<Button> styledButtons = new HashSet<Button>();
        private MiniPlayerForm? miniPlayer;
        private bool isMiniMode;
        private Rectangle normalBoundsBeforeMini;
        private FormWindowState normalWindowStateBeforeMini;
        private bool normalTopMostBeforeMini;
        private float currentPlayListVolumeScale = 1.0f;

        private sealed class ButtonVisualStyle
        {
            public Color BorderColor { get; init; }
        }

        private enum ButtonIconKind
        {
            Play,
            Pause,
            Previous,
            Next,
            Favorite,
            Delete,
            DeletePart,
            Locate,
            Globe,
            Folder,
            Theme
        }

        public MainWindow()
        {
            InitializeComponent();
            Config.LoadJson();
            currentTheme = PlayerThemes.Resolve(Config.PlayerThemeId);
            MountThemeMenu();
            ApplyTheme(currentTheme);
            MountResponsiveLayout();
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
                toolTip.SetToolTip(this.PlayButton, "播放/暂停");
                toolTip.SetToolTip(this.PrevButton, "上一首");
                toolTip.SetToolTip(this.NextButton, "下一首");
                toolTip.SetToolTip(this.SelectCurrentButton, "选中正在播放的条目");
                toolTip.SetToolTip(this.FavButton, "收藏当前");
                toolTip.SetToolTip(this.DelButton, "删除当前");
                toolTip.SetToolTip(this.DelPartButton, "删除当前播放文件");
                toolTip.SetToolTip(this.ThemeButton, "切换主题");
                toolTip.SetToolTip(this.OpenWebButton, "打开网页");
                toolTip.SetToolTip(this.OpenLocalButton, "打开本地文件夹");
            }
            MountMiniModeInteractions();
            {
                MountPlayStopEvent();
            }
            InitPlayListTree();
            RefreshPlayButton();
        }
        void OnPlayTimerTick(object? sender, ElapsedEventArgs e)
        {
            if (CurrentPlayer.IsLoaded())//timer触发的响应不在主线程，需要用invoke
                this.Invoke(() =>
                {
                    this.playSlider.Value = CurrentPlayer.GetCurrentPositionSec();
                    SyncMiniPlayer();
                });
        }
        //slider因任何原因产生变化时，修改label
        void OnSliderValueChanged(object? sender, EventArgs e)
        {
            int sec = playSlider.Value;
            int total = playSlider.Maximum;
            this.sliderLabel.Text = $"{sec / 60}:{(sec % 60).ToString().PadLeft(2, '0')} / {total / 60}:{(total % 60).ToString().PadLeft(2, '0')}";
            SyncMiniPlayer();
        }
        //用户拖动进度条结束时，改变播放进度
        void OnSliderScrolled(object? sender, EventArgs e)
        {
            SeekTo(playSlider.Value);
        }
        void OnVolumeSliderScrolled(object? sender, EventArgs e)
        {
            ApplyEffectiveVolume();
        }

        private void ApplyEffectiveVolume()
        {
            var userVolume = Math.Clamp(volumeSlider.Value / 100.0f, 0.0f, 1.0f);
            var effectiveVolume = userVolume * Math.Clamp(currentPlayListVolumeScale, 0.0f, 1.0f);
            CurrentPlayer.SetVolume(effectiveVolume);
        }

        private void SeekTo(int sec)
        {
            if (!CurrentPlayer.IsLoaded())
                return;

            int nextPosition = Math.Clamp(sec, playSlider.Minimum, playSlider.Maximum);
            CurrentPlayer.SetCurrentPositionSec(nextPosition);
            playSlider.Value = nextPosition;
            SyncMiniPlayer();
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
            MoveOrStartCurrentPlayList(-1);
        }
        void OnNextButtonClicked(object? sender, EventArgs e)
        {
            MoveOrStartCurrentPlayList(1);
        }

        private void MoveOrStartCurrentPlayList(int offset)
        {
            int currentIndex = PlayListTab.SelectedIndex;
            if (currentIndex < 0 || currentIndex >= playLists.Count)
                return;

            var playList = playLists[currentIndex];
            bool shouldStartPlayback = !CurrentPlayer.IsLoaded();
            if (shouldStartPlayback)
                playList.MoveToFirst();
            else
                playList.MoveCurrent(offset);
            ReloadCurrentFile();
            if (shouldStartPlayback && CurrentPlayer.IsLoaded())
                Play();
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
                return;
            else
            {
                var arg = new MyFileEditEventArgs();
                OnFileEditBegin(null, arg);
                playLists[PlayListTab.SelectedIndex].DeleteCurrentFile();
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

        private void MountMiniModeInteractions()
        {
            UpPanel.DoubleClick += onMainWindowMouseDoubleClick;
            titleBox.DoubleClick += onMainWindowMouseDoubleClick;
        }

        private MiniPlayerForm GetMiniPlayer()
        {
            if (miniPlayer != null && !miniPlayer.IsDisposed)
                return miniPlayer;

            var player = new MiniPlayerForm(currentTheme)
            {
                Icon = Icon
            };
            player.PlayPauseClicked += OnPlayButtonClicked;
            player.PreviousClicked += OnPrevButtonClicked;
            player.NextClicked += OnNextButtonClicked;
            player.FavoriteClicked += OnFavButtonClicked;
            player.DeleteClicked += OnDelButtonClicked;
            player.DeletePartClicked += OnDelPartButtonClicked;
            player.RestoreRequested += delegate { RestoreFromMiniMode(); };
            player.SeekStarted += delegate { timer.Enabled = false; };
            player.SeekEnded += delegate { timer.Enabled = true; };
            player.SeekRequested += delegate { SeekTo(player.SeekPositionSec); };
            miniPlayer = player;
            return player;
        }

        private void EnterMiniMode()
        {
            if (isMiniMode)
                return;

            var player = GetMiniPlayer();
            normalWindowStateBeforeMini = WindowState;
            normalBoundsBeforeMini = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;
            normalTopMostBeforeMini = TopMost;
            isMiniMode = true;

            player.TopMost = true;
            player.ApplyTheme(currentTheme);
            SyncMiniPlayer();
            player.PlaceNearTop(Screen.FromControl(this));
            player.Show();
            player.Activate();
            Hide();
        }

        private void RestoreFromMiniMode()
        {
            if (!isMiniMode)
                return;

            isMiniMode = false;
            miniPlayer?.Hide();
            Show();
            WindowState = FormWindowState.Normal;
            if (normalBoundsBeforeMini.Width > 0 && normalBoundsBeforeMini.Height > 0)
                Bounds = normalBoundsBeforeMini;
            TopMost = normalTopMostBeforeMini;
            if (normalWindowStateBeforeMini != FormWindowState.Minimized)
                WindowState = normalWindowStateBeforeMini;
            Activate();
        }

        private void SyncMiniPlayer()
        {
            if (miniPlayer == null || miniPlayer.IsDisposed)
                return;

            miniPlayer.UpdatePlayback(
                GetCurrentMiniTitle(),
                playSlider.Value,
                playSlider.Maximum,
                CurrentPlayer.IsLoaded(),
                IsCurrentPlayListDelPartEnabled(),
                CurrentPlayer.IsPlaying());
        }

        private string GetCurrentMiniTitle()
        {
            int currentIndex = PlayListTab.SelectedIndex;
            if (currentIndex < 0 || currentIndex >= playLists.Count)
                return titleBox.Text;

            var title = playLists[currentIndex].GetCurrentMiniTitle();
            return string.IsNullOrWhiteSpace(title) ? titleBox.Text : title;
        }

        private bool IsCurrentPlayListDelPartEnabled()
        {
            int currentIndex = PlayListTab.SelectedIndex;
            return currentIndex >= 0
                && currentIndex < playLists.Count
                && playLists[currentIndex].needDelPartButton;
        }

        private void MountThemeMenu()
        {
            ThemeButton.Click += OnThemeButtonClicked;
            RebuildThemeMenu();
        }

        private void RebuildThemeMenu()
        {
            themeMenuStrip.Items.Clear();
            themeMenuStrip.ShowCheckMargin = true;
            themeMenuStrip.BackColor = currentTheme.SurfaceColor;
            themeMenuStrip.ForeColor = currentTheme.TextColor;

            foreach (var theme in PlayerThemes.All)
            {
                var item = new ToolStripMenuItem(theme.DisplayName)
                {
                    Checked = theme.Id == currentTheme.Id,
                    Tag = theme
                };
                item.Click += OnThemeMenuItemClicked;
                themeMenuStrip.Items.Add(item);
            }
        }

        private void OnThemeButtonClicked(object? sender, EventArgs e)
        {
            themeMenuStrip.Show(ThemeButton, new Point(0, ThemeButton.Height + 2));
        }

        private void OnThemeMenuItemClicked(object? sender, EventArgs e)
        {
            if (sender is not ToolStripMenuItem { Tag: PlayerTheme theme })
                return;

            Config.PlayerThemeId = theme.Id;
            Config.SaveTheme();
            ApplyTheme(theme);
            RebuildThemeMenu();
        }

        private void ApplyTheme(PlayerTheme theme)
        {
            currentTheme = theme;

            BackColor = theme.WindowBackColor;
            mainTableLayoutPanel.BackColor = theme.WindowBackColor;
            UpPanel.BackColor = theme.WindowBackColor;
            MiddlePanel.BackColor = theme.WindowBackColor;
            MiddlePanelFlowLayoutPanel.BackColor = theme.WindowBackColor;
            DownPanel.BackColor = theme.WindowBackColor;
            PlayListTab.ApplyTheme(theme);
            titleBox.BackColor = theme.TitleBackColor;
            titleBox.ForeColor = theme.TextColor;
            titleBox.Font = new Font("Microsoft YaHei UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point);
            titleBox.BorderStyle = BorderStyle.FixedSingle;
            sliderLabel.ForeColor = theme.MutedTextColor;
            sliderLabel.BackColor = theme.WindowBackColor;
            LockCheckBox.BackColor = theme.WindowBackColor;
            LockCheckBox.ForeColor = theme.TextColor;
            volumeSlider.BackColor = theme.WindowBackColor;
            ApplySliderTheme(playSlider, theme);
            ApplySliderTheme(volumeSlider, theme);
            volumeSlider.TickFrequency = 10;

            StyleIconButton(PlayButton, theme.AccentColor, theme.AccentIconColor, theme.AccentHoverColor, theme.AccentDownColor, theme.AccentColor);
            foreach (var button in new[] { PrevButton, NextButton, SelectCurrentButton, ThemeButton, OpenWebButton, OpenLocalButton })
                StyleIconButton(button, theme.ButtonBackColor, theme.ButtonIconColor, theme.ButtonHoverColor, theme.ButtonDownColor, theme.BorderColor);
            StyleIconButton(FavButton, theme.ButtonBackColor, theme.FavoriteColor, theme.FavoriteHoverColor, theme.FavoriteDownColor, theme.BorderColor);
            StyleIconButton(DelButton, theme.ButtonBackColor, theme.DeleteColor, theme.DeleteHoverColor, theme.DeleteDownColor, theme.BorderColor);
            StyleIconButton(DelPartButton, theme.ButtonBackColor, theme.DeletePartColor, theme.DeletePartHoverColor, theme.DeletePartDownColor, theme.BorderColor);

            ApplyThemeToChildControls(DownPanel);
            ApplyThemeToPlayLists();
            ApplyNativeTitleBarTheme();
            miniPlayer?.ApplyTheme(theme);
            UpdateButtonIcons();
            SyncMiniPlayer();
            PlayListTab.Invalidate();
            Refresh();
        }

        private static void ApplySliderTheme(ElegantTrackBar slider, PlayerTheme theme)
        {
            slider.BackColor = theme.WindowBackColor;
            slider.TrackColor = theme.SliderTrackColor;
            slider.FillColor = theme.SliderFillColor;
            slider.ThumbColor = theme.SliderThumbColor;
            slider.ThumbBorderColor = theme.SliderFillColor;
            slider.TickColor = theme.SliderTickColor;
            slider.ShadowColor = theme.SliderShadowColor;
            slider.TrackHeight = theme.SliderTrackHeight;
            slider.ThumbSize = theme.SliderThumbSize;
            slider.ActiveThumbSize = theme.SliderActiveThumbSize;
        }

        private void ApplyThemeToPlayLists()
        {
            foreach (var playList in playLists)
                playList.ApplyTheme(currentTheme);
        }

        private void ApplyThemeToChildControls(Control parent)
        {
            foreach (Control control in parent.Controls)
            {
                switch (control)
                {
                    case Button:
                        break;
                    case TabPage:
                        break;
                    case TreeView treeView:
                        treeView.BackColor = currentTheme.ListBackColor;
                        treeView.ForeColor = currentTheme.ListForeColor;
                        treeView.LineColor = currentTheme.BorderColor;
                        treeView.BorderStyle = BorderStyle.None;
                        break;
                    case ListView listView:
                        listView.BackColor = currentTheme.ListBackColor;
                        listView.ForeColor = currentTheme.ListForeColor;
                        listView.BorderStyle = BorderStyle.None;
                        break;
                    case Panel:
                        control.BackColor = currentTheme.WindowBackColor;
                        control.ForeColor = currentTheme.TextColor;
                        break;
                    default:
                        control.BackColor = currentTheme.SurfaceColor;
                        control.ForeColor = currentTheme.TextColor;
                        break;
                }

                if (control.HasChildren)
                    ApplyThemeToChildControls(control);
                control.Invalidate();
            }
        }

        private void MountResponsiveLayout()
        {
            UpPanel.Resize += delegate { LayoutTopControls(); };
            MiddlePanel.Resize += delegate { LayoutPlaybackControls(); };
            MiddlePanelFlowLayoutPanel.SizeChanged += delegate { LayoutPlaybackControls(); };
            LayoutTopControls();
            LayoutPlaybackControls();
        }

        private void LayoutTopControls()
        {
            int rightControlsLeft = ThemeButton.Left;
            int rightControlsRight = OpenLocalButton.Right;
            int volumeWidth = Math.Max(120, rightControlsRight - rightControlsLeft);

            titleBox.Width = Math.Max(120, rightControlsLeft - titleBox.Left - 8);
            volumeSlider.Bounds = new Rectangle(
                rightControlsLeft,
                volumeSlider.Top,
                volumeWidth,
                volumeSlider.Height);
        }

        private void LayoutPlaybackControls()
        {
            int sliderLeft = NextButton.Right + 12;
            int sliderRight = MiddlePanelFlowLayoutPanel.Left - 12;
            int sliderWidth = Math.Max(90, sliderRight - sliderLeft);
            int sliderHeight = 54;
            playSlider.Bounds = new Rectangle(
                sliderLeft,
                Math.Max(4, (MiddlePanel.Height - sliderHeight) / 2),
                sliderWidth,
                sliderHeight);
        }

        private void StyleIconButton(Button button, Color backColor, Color foreColor, Color hoverColor, Color downColor, Color borderColor)
        {
            buttonStyles[button] = new ButtonVisualStyle { BorderColor = borderColor };
            button.UseVisualStyleBackColor = false;
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = hoverColor;
            button.FlatAppearance.MouseDownBackColor = downColor;
            button.BackColor = backColor;
            button.ForeColor = foreColor;
            button.Text = "";
            button.ImageAlign = ContentAlignment.MiddleCenter;
            button.TextAlign = ContentAlignment.MiddleCenter;
            button.Padding = Padding.Empty;
            button.Cursor = Cursors.Hand;
            if (styledButtons.Add(button))
            {
                button.Paint += delegate (object? sender, PaintEventArgs e)
                {
                    var target = (Button)sender!;
                    PaintRoundedButtonBorder(target, e);
                };
                button.Resize += delegate { ApplyRoundedRegion(button); };
            }
            ApplyRoundedRegion(button);
            button.Invalidate();
        }

        private void UpdateButtonIcons()
        {
            int size = NormalIconSize;
            SetButtonIcon(PrevButton, ButtonIconKind.Previous, currentTheme.ButtonIconColor, size);
            SetButtonIcon(NextButton, ButtonIconKind.Next, currentTheme.ButtonIconColor, size);
            SetButtonIcon(FavButton, ButtonIconKind.Favorite, currentTheme.FavoriteColor, size);
            SetButtonIcon(DelButton, ButtonIconKind.Delete, currentTheme.DeleteColor, size);
            SetButtonIcon(DelPartButton, ButtonIconKind.DeletePart, currentTheme.DeletePartColor, size);
            SetButtonIcon(SelectCurrentButton, ButtonIconKind.Locate, currentTheme.AccentColor, size);
            SetButtonIcon(ThemeButton, ButtonIconKind.Theme, currentTheme.ButtonIconColor, size);
            SetButtonIcon(OpenWebButton, ButtonIconKind.Globe, currentTheme.ButtonIconColor, size);
            SetButtonIcon(OpenLocalButton, ButtonIconKind.Folder, currentTheme.ButtonIconColor, size);
            RefreshPlayButton();
        }

        private void SetButtonIcon(Button button, ButtonIconKind icon, Color color, int size)
        {
            var oldImage = button.Image;
            button.Image = CreateButtonIcon(icon, color, size);
            oldImage?.Dispose();
            button.Text = "";
        }

        private void PaintRoundedButtonBorder(Button button, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var borderWidth = currentTheme.ButtonBorderWidth;
            var inset = borderWidth / 2F;
            var bounds = new RectangleF(inset, inset, button.Width - borderWidth - 1, button.Height - borderWidth - 1);
            using var path = CreateRoundedRectanglePath(bounds, currentTheme.ButtonCornerRadius);
            var borderColor = buttonStyles.TryGetValue(button, out var style)
                ? style.BorderColor
                : currentTheme.BorderColor;
            using var pen = new Pen(button.Enabled ? borderColor : ControlPaint.Light(currentTheme.BorderColor), borderWidth);
            e.Graphics.DrawPath(pen, path);
        }

        private void ApplyRoundedRegion(Control control)
        {
            if (control.Width <= 0 || control.Height <= 0)
                return;
            var bounds = new Rectangle(0, 0, control.Width, control.Height);
            using var path = CreateRoundedRectanglePath(bounds, currentTheme.ButtonCornerRadius);
            var oldRegion = control.Region;
            control.Region = new Region(path);
            oldRegion?.Dispose();
        }

        private static GraphicsPath CreateRoundedRectanglePath(RectangleF bounds, float radius)
        {
            var path = new GraphicsPath();
            float diameter = Math.Min(radius * 2, Math.Min(bounds.Width, bounds.Height));
            if (diameter <= 0)
                return path;

            var arc = new RectangleF(bounds.Location, new SizeF(diameter, diameter));
            path.AddArc(arc, 180, 90);
            arc.X = bounds.Right - diameter;
            path.AddArc(arc, 270, 90);
            arc.Y = bounds.Bottom - diameter;
            path.AddArc(arc, 0, 90);
            arc.X = bounds.Left;
            path.AddArc(arc, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static Bitmap CreateButtonIcon(ButtonIconKind icon, Color color, int size)
        {
            var svgPath = GetLucideIconPath(icon);
            if (!File.Exists(svgPath))
                return new Bitmap(size, size);

            var svg = File.ReadAllText(svgPath)
                .Replace("currentColor", ColorTranslator.ToHtml(color))
                .Replace("stroke-width=\"2\"", "stroke-width=\"2.35\"");
            var document = SvgDocument.FromSvg<SvgDocument>(svg);
            document.Width = new SvgUnit(SvgUnitType.Pixel, size);
            document.Height = new SvgUnit(SvgUnitType.Pixel, size);
            return document.Draw(size, size);
        }

        private static string GetLucideIconPath(ButtonIconKind icon)
        {
            return Path.Combine(AppContext.BaseDirectory, "Assets", "Lucide", $"{GetLucideIconName(icon)}.svg");
        }

        private static string GetLucideIconName(ButtonIconKind icon)
        {
            return icon switch
            {
                ButtonIconKind.Play => "play",
                ButtonIconKind.Pause => "pause",
                ButtonIconKind.Previous => "skip-back",
                ButtonIconKind.Next => "skip-forward",
                ButtonIconKind.Favorite => "heart",
                ButtonIconKind.Delete => "trash-2",
                ButtonIconKind.DeletePart => "file-x",
                ButtonIconKind.Locate => "arrow-right-to-line",
                ButtonIconKind.Globe => "external-link",
                ButtonIconKind.Folder => "folder-open",
                ButtonIconKind.Theme => "palette",
                _ => "circle"
            };
        }

        void RefreshPlayButton()
        {
            var icon = CurrentPlayer.IsPlaying() ? ButtonIconKind.Pause : ButtonIconKind.Play;
            SetButtonIcon(PlayButton, icon, currentTheme.AccentIconColor, NormalIconSize);
            SyncMiniPlayer();
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
                playSlider.Value = 0;
                playSlider.Maximum = 0;
                SyncMiniPlayer();
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
            currentPlayListVolumeScale = Math.Clamp(playLists[PlayListTab.SelectedIndex].VolumeScale, 0.0f, 1.0f);
            ApplyEffectiveVolume();
            //显示信息
            titleBox.Text = playLists[PlayListTab.SelectedIndex].GetCurrentFileDesc();
            this.playSlider.Minimum = 0;
            this.playSlider.Maximum = CurrentPlayer.GetTotalLengthSec();
            this.playSlider.Value = CurrentPlayer.GetCurrentPositionSec();
            SyncMiniPlayer();
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
            foreach (var config in Config.playLists)
            {
                PlayListBase? playList = null;
                if (config.Type == typeof(PlayListDLSite).Name)
                    playList = new PlayListDLSite(config.Path, this.OnFileEditBegin, this.OnFileEditEnd);
                else if (config.Type == typeof(PlayListLocalMusic).Name)
                    playList = new PlayListLocalMusic(config.Path, this.OnFileEditBegin, this.OnFileEditEnd);

                if (playList != null)
                {
                    playList.VolumeScale = Math.Clamp(config.VolumeScale, 0.0f, 1.0f);
                    playLists.Add(playList);
                }
            }

            PlayListTab.SuspendLayout();
            foreach (var playList in playLists)
            {
                playList.ApplyTheme(currentTheme);
                var tabPage = new TabPage();
                tabPage.AutoScroll = true;
                tabPage.Text = playList.Title;
                tabPage.BackColor = currentTheme.SurfaceColor;
                tabPage.ForeColor = currentTheme.TextColor;
                tabPage.BorderStyle = BorderStyle.None;
                tabPage.Padding = Padding.Empty;
                tabPage.UseVisualStyleBackColor = false;
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
            ApplyThemeToChildControls(DownPanel);
            PlayListTab.Invalidate();
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

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            timer.Stop();
            timer.Elapsed -= this.OnPlayTimerTick;//防止关闭窗口后timer还触发事件导致异常
            noTriggerPlayStoppedEvent = true;
            UnmountPlayStopEvent();
            if (miniPlayer != null && !miniPlayer.IsDisposed)
            {
                miniPlayer.AllowClose();
                miniPlayer.Close();
            }
            CurrentPlayer.Shutdown();
            base.OnFormClosing(e);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            ApplyNativeTitleBarTheme();
        }

        private const int WM_SYSCOMMAND = 0x0112;
        private const int SC_MOVE = 0xf010;
        private const int HTCAPTION = 0x0002;
        private const int WM_NCLBUTTONDBLCLK = 0x00A3;
        private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
        private const int DWMWA_BORDER_COLOR = 34;
        private const int DWMWA_CAPTION_COLOR = 35;
        private const int DWMWA_TEXT_COLOR = 36;

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_NCLBUTTONDBLCLK && m.WParam.ToInt32() == HTCAPTION)
            {
                EnterMiniMode();
                return;
            }

            base.WndProc(ref m);
        }
        [DllImport("user32.dll")]
        public static extern bool ReleaseCapture();
        [DllImport("user32.dll")]
        public static extern bool SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);
        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int dwAttribute, ref int pvAttribute, int cbAttribute);

        private void ApplyNativeTitleBarTheme()
        {
            if (!IsHandleCreated)
                return;

            try
            {
                int darkMode = IsDarkColor(currentTheme.WindowBackColor) ? 1 : 0;
                DwmSetWindowAttribute(Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref darkMode, sizeof(int));

                int captionColor = ColorTranslator.ToWin32(currentTheme.WindowBackColor);
                int textColor = ColorTranslator.ToWin32(currentTheme.TextColor);
                int borderColor = ColorTranslator.ToWin32(currentTheme.BorderColor);
                DwmSetWindowAttribute(Handle, DWMWA_CAPTION_COLOR, ref captionColor, sizeof(int));
                DwmSetWindowAttribute(Handle, DWMWA_TEXT_COLOR, ref textColor, sizeof(int));
                DwmSetWindowAttribute(Handle, DWMWA_BORDER_COLOR, ref borderColor, sizeof(int));
            }
            catch
            {
                // Older Windows builds may not support all DWM color attributes.
            }
        }

        private static bool IsDarkColor(Color color)
        {
            double luminance = (0.2126 * color.R + 0.7152 * color.G + 0.0722 * color.B) / 255D;
            return luminance < 0.45D;
        }

        //应该令所有panel忽略鼠标事件传给parent，但是不知道怎么实现
        //目前是把MiddlePanel、MiddlePanelFlowLayoutPanel、MainWindow的鼠标事件全挂在同一个函数上
        private void onMainWindowMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left && e.Clicks == 1)//double click也会先触发mousedown,需要判断是否是单击左键
            {
                ReleaseCapture();
                SendMessage(this.Handle, WM_SYSCOMMAND, SC_MOVE | HTCAPTION, 0);
            }
        }

        private void onMainWindowMouseDoubleClick(object? sender, EventArgs e)
        {
            EnterMiniMode();
        }

        private void onMainWindowMove(object? sender, EventArgs e)
        {
        }

        private void onLockChanged(object? sender, EventArgs e)
        {
        }
    }
}

using MyAudioPlayer.Themes;
using Svg;
using System.Drawing.Drawing2D;

namespace MyAudioPlayer
{
    internal sealed class MiniPlayerForm : Form
    {
        private const int ButtonSize = 44;
        private const int SmallIconSize = 25;
        private const int SnapDistance = 24;

        private readonly Button playButton = new Button();
        private readonly Button previousButton = new Button();
        private readonly Button nextButton = new Button();
        private readonly Button favoriteButton = new Button();
        private readonly Button deleteButton = new Button();
        private readonly Button deletePartButton = new Button();
        private readonly MarqueeLabel titleLabel = new MarqueeLabel();
        private readonly Label timeLabel = new Label();
        private readonly ElegantTrackBar playSlider = new ElegantTrackBar();
        private readonly ToolTip toolTip = new ToolTip();

        private PlayerTheme theme;
        private bool isPlaying;
        private bool allowClose;
        private bool dragging;
        private Point dragCursorStart;
        private Point dragWindowStart;

        public event EventHandler? PlayPauseClicked;
        public event EventHandler? PreviousClicked;
        public event EventHandler? NextClicked;
        public event EventHandler? FavoriteClicked;
        public event EventHandler? DeleteClicked;
        public event EventHandler? DeletePartClicked;
        public event EventHandler? RestoreRequested;
        public event EventHandler? SeekStarted;
        public event EventHandler? SeekRequested;
        public event EventHandler? SeekEnded;

        public MiniPlayerForm(PlayerTheme initialTheme)
        {
            theme = initialTheme;

            SetStyle(ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw
                | ControlStyles.UserPaint, true);
            AutoScaleMode = AutoScaleMode.None;
            ClientSize = new Size(980, 72);
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            MaximizeBox = false;
            MinimizeBox = false;
            MinimumSize = ClientSize;
            MaximumSize = ClientSize;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;

            ConfigureButton(playButton, "播放/暂停");
            ConfigureButton(previousButton, "上一首");
            ConfigureButton(nextButton, "下一首");
            ConfigureButton(favoriteButton, "收藏当前");
            ConfigureButton(deleteButton, "删除当前");
            ConfigureButton(deletePartButton, "删除当前文件或文件集");

            timeLabel.AutoSize = false;
            timeLabel.TextAlign = ContentAlignment.MiddleCenter;

            playSlider.Minimum = 0;
            playSlider.Maximum = 0;
            playSlider.TickFrequency = 60;

            Controls.Add(playButton);
            Controls.Add(previousButton);
            Controls.Add(nextButton);
            Controls.Add(titleLabel);
            Controls.Add(playSlider);
            Controls.Add(timeLabel);
            Controls.Add(favoriteButton);
            Controls.Add(deleteButton);
            Controls.Add(deletePartButton);

            playButton.Click += delegate { PlayPauseClicked?.Invoke(this, EventArgs.Empty); };
            previousButton.Click += delegate { PreviousClicked?.Invoke(this, EventArgs.Empty); };
            nextButton.Click += delegate { NextClicked?.Invoke(this, EventArgs.Empty); };
            favoriteButton.Click += delegate { FavoriteClicked?.Invoke(this, EventArgs.Empty); };
            deleteButton.Click += delegate { DeleteClicked?.Invoke(this, EventArgs.Empty); };
            deletePartButton.Click += delegate { DeletePartClicked?.Invoke(this, EventArgs.Empty); };
            playSlider.MouseDown += delegate { SeekStarted?.Invoke(this, EventArgs.Empty); };
            playSlider.MouseUp += delegate { SeekEnded?.Invoke(this, EventArgs.Empty); };
            playSlider.Scroll += delegate { SeekRequested?.Invoke(this, EventArgs.Empty); };

            foreach (Control dragSurface in new Control[] { this, titleLabel, timeLabel })
            {
                dragSurface.MouseDown += OnDragSurfaceMouseDown;
                dragSurface.MouseMove += OnDragSurfaceMouseMove;
                dragSurface.MouseUp += OnDragSurfaceMouseUp;
                dragSurface.DoubleClick += delegate { RestoreRequested?.Invoke(this, EventArgs.Empty); };
            }

            ApplyTheme(theme);
            UpdatePlayback("None", 0, 0, false, false, false);
            LayoutControls();
        }

        public int SeekPositionSec
        {
            get { return playSlider.Value; }
        }

        public void AllowClose()
        {
            allowClose = true;
        }

        public void PlaceNearTop(Screen screen)
        {
            var area = screen.WorkingArea;
            Location = new Point(
                area.Left + Math.Max(0, (area.Width - Width) / 2),
                area.Top);
        }

        public void ApplyTheme(PlayerTheme nextTheme)
        {
            theme = nextTheme;

            BackColor = theme.WindowBackColor;
            ForeColor = theme.TextColor;
            titleLabel.BackColor = Color.Transparent;
            titleLabel.ForeColor = theme.TextColor;
            titleLabel.Font = new Font("Microsoft YaHei UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
            timeLabel.BackColor = Color.Transparent;
            timeLabel.ForeColor = theme.MutedTextColor;
            timeLabel.Font = new Font("Microsoft YaHei UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point);

            ApplySliderTheme(playSlider, theme);
            StyleButton(playButton, theme.AccentColor, theme.AccentIconColor, theme.AccentColor);
            StyleButton(previousButton, theme.ButtonBackColor, theme.ButtonIconColor, theme.BorderColor);
            StyleButton(nextButton, theme.ButtonBackColor, theme.ButtonIconColor, theme.BorderColor);
            StyleButton(favoriteButton, theme.ButtonBackColor, theme.FavoriteColor, theme.BorderColor);
            StyleButton(deleteButton, theme.ButtonBackColor, theme.DeleteColor, theme.BorderColor);
            StyleButton(deletePartButton, theme.ButtonBackColor, theme.DeletePartColor, theme.BorderColor);
            UpdateButtonIcons();
            ApplyRoundedRegion();
            Invalidate();
        }

        public void UpdatePlayback(string title, int positionSec, int durationSec, bool isLoaded, bool canDeletePart, bool nextIsPlaying)
        {
            bool playStateChanged = isPlaying != nextIsPlaying;
            isPlaying = nextIsPlaying;
            string nextTitle = string.IsNullOrWhiteSpace(title) ? "None" : title.Trim();
            if (titleLabel.Text != nextTitle)
                titleLabel.Text = nextTitle;
            timeLabel.Text = $"{FormatTime(positionSec)} / {FormatTime(durationSec)}";
            playSlider.Maximum = Math.Max(0, durationSec);
            playSlider.Value = Math.Clamp(positionSec, playSlider.Minimum, playSlider.Maximum);
            playSlider.Enabled = isLoaded && durationSec > 0;
            playButton.Enabled = true;
            previousButton.Enabled = true;
            nextButton.Enabled = true;
            favoriteButton.Enabled = isLoaded;
            deleteButton.Enabled = isLoaded;
            deletePartButton.Enabled = isLoaded && canDeletePart;
            if (playStateChanged)
                SetButtonIcon(playButton, isPlaying ? "pause" : "play", theme.AccentIconColor, SmallIconSize);
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            LayoutControls();
            ApplyRoundedRegion();
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            var bounds = new RectangleF(1.5F, 1.5F, Width - 3F, Height - 3F);
            using var path = CreateRoundedRectanglePath(bounds, 18F);
            using var backBrush = new SolidBrush(theme.WindowBackColor);
            using var leftBrush = new SolidBrush(theme.SubtleSurfaceColor);
            e.Graphics.FillPath(backBrush, path);

            var leftBounds = new RectangleF(1.5F, 1.5F, 166F, Height - 3F);
            using var leftPath = CreateRoundedRectanglePath(leftBounds, 18F);
            e.Graphics.FillPath(leftBrush, leftPath);

            using var separatorPen = new Pen(Color.FromArgb(90, theme.BorderColor), 1.5F);
            e.Graphics.DrawLine(separatorPen, 166F, 12F, 166F, Height - 12F);

            using var borderPen = new Pen(theme.BorderColor, 2.5F);
            e.Graphics.DrawPath(borderPen, path);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!allowClose && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                RestoreRequested?.Invoke(this, EventArgs.Empty);
                return;
            }

            base.OnFormClosing(e);
        }

        private void ConfigureButton(Button button, string tip)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.ImageAlign = ContentAlignment.MiddleCenter;
            button.TextAlign = ContentAlignment.MiddleCenter;
            button.Padding = Padding.Empty;
            button.Cursor = Cursors.Hand;
            button.UseVisualStyleBackColor = false;
            button.Paint += OnButtonPaint;
            button.Resize += delegate { ApplyRoundedRegion(button); };
            toolTip.SetToolTip(button, tip);
        }

        private void StyleButton(Button button, Color backColor, Color foreColor, Color borderColor)
        {
            button.BackColor = backColor;
            button.ForeColor = foreColor;
            button.Tag = borderColor;
            button.FlatAppearance.MouseOverBackColor = Blend(backColor, foreColor, 0.18F);
            button.FlatAppearance.MouseDownBackColor = Blend(backColor, Color.Black, 0.18F);
            ApplyRoundedRegion(button);
            button.Invalidate();
        }

        private void OnButtonPaint(object? sender, PaintEventArgs e)
        {
            if (sender is not Button button)
                return;

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            var borderColor = button.Tag is Color color ? color : theme.BorderColor;
            var borderWidth = Math.Max(1.5F, theme.ButtonBorderWidth);
            var bounds = new RectangleF(borderWidth / 2F, borderWidth / 2F, button.Width - borderWidth - 1, button.Height - borderWidth - 1);
            using var path = CreateRoundedRectanglePath(bounds, theme.ButtonCornerRadius);
            using var pen = new Pen(button.Enabled ? borderColor : ControlPaint.Light(theme.BorderColor), borderWidth);
            e.Graphics.DrawPath(pen, path);
        }

        private void LayoutControls()
        {
            int top = 14;
            playButton.Bounds = new Rectangle(12, top, ButtonSize, ButtonSize);
            previousButton.Bounds = new Rectangle(62, top, ButtonSize, ButtonSize);
            nextButton.Bounds = new Rectangle(112, top, ButtonSize, ButtonSize);

            int right = Width - 12;
            deletePartButton.Bounds = new Rectangle(right - ButtonSize, top, ButtonSize, ButtonSize);
            deleteButton.Bounds = new Rectangle(deletePartButton.Left - ButtonSize - 6, top, ButtonSize, ButtonSize);
            favoriteButton.Bounds = new Rectangle(deleteButton.Left - ButtonSize - 6, top, ButtonSize, ButtonSize);
            timeLabel.Bounds = new Rectangle(favoriteButton.Left - 98, top + 1, 88, 42);

            int contentLeft = 182;
            int contentRight = timeLabel.Left - 16;
            titleLabel.Bounds = new Rectangle(contentLeft, 6, Math.Max(120, contentRight - contentLeft), 24);
            playSlider.Bounds = new Rectangle(contentLeft - 18, 32, Math.Max(120, contentRight - contentLeft + 18), 34);
        }

        private void UpdateButtonIcons()
        {
            SetButtonIcon(playButton, isPlaying ? "pause" : "play", theme.AccentIconColor, SmallIconSize);
            SetButtonIcon(previousButton, "skip-back", theme.ButtonIconColor, SmallIconSize);
            SetButtonIcon(nextButton, "skip-forward", theme.ButtonIconColor, SmallIconSize);
            SetButtonIcon(favoriteButton, "heart", theme.FavoriteColor, SmallIconSize);
            SetButtonIcon(deleteButton, "trash-2", theme.DeleteColor, SmallIconSize);
            SetButtonIcon(deletePartButton, "file-x", theme.DeletePartColor, SmallIconSize);
        }

        private void SetButtonIcon(Button button, string iconName, Color color, int size)
        {
            var oldImage = button.Image;
            button.Image = CreateButtonIcon(iconName, color, size);
            oldImage?.Dispose();
            button.Text = "";
        }

        private static Bitmap CreateButtonIcon(string iconName, Color color, int size)
        {
            var svgPath = Path.Combine(AppContext.BaseDirectory, "Assets", "Lucide", $"{iconName}.svg");
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

        private static void ApplySliderTheme(ElegantTrackBar slider, PlayerTheme theme)
        {
            slider.BackColor = theme.WindowBackColor;
            slider.TrackColor = theme.SliderTrackColor;
            slider.FillColor = theme.SliderFillColor;
            slider.ThumbColor = theme.SliderThumbColor;
            slider.ThumbBorderColor = theme.SliderFillColor;
            slider.TickColor = theme.SliderTickColor;
            slider.ShadowColor = theme.SliderShadowColor;
            slider.TrackHeight = Math.Max(5F, theme.SliderTrackHeight - 1F);
            slider.ThumbSize = Math.Max(15F, theme.SliderThumbSize - 2F);
            slider.ActiveThumbSize = Math.Max(18F, theme.SliderActiveThumbSize - 2F);
        }

        private static string FormatTime(int totalSeconds)
        {
            totalSeconds = Math.Max(0, totalSeconds);
            return $"{totalSeconds / 60}:{(totalSeconds % 60).ToString().PadLeft(2, '0')}";
        }

        private void OnDragSurfaceMouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || e.Clicks != 1)
                return;

            dragging = true;
            dragCursorStart = Cursor.Position;
            dragWindowStart = Location;
            if (sender is Control control)
                control.Capture = true;
        }

        private void OnDragSurfaceMouseMove(object? sender, MouseEventArgs e)
        {
            if (!dragging)
                return;

            var cursor = Cursor.Position;
            Location = new Point(
                dragWindowStart.X + cursor.X - dragCursorStart.X,
                dragWindowStart.Y + cursor.Y - dragCursorStart.Y);
        }

        private void OnDragSurfaceMouseUp(object? sender, MouseEventArgs e)
        {
            if (!dragging || e.Button != MouseButtons.Left)
                return;

            dragging = false;
            if (sender is Control control)
                control.Capture = false;
            SnapToEdges();
        }

        private void SnapToEdges()
        {
            var area = Screen.FromControl(this).WorkingArea;
            int x = Left;
            int y = Top;

            if (Math.Abs(Left - area.Left) <= SnapDistance)
                x = area.Left;
            if (Math.Abs(Right - area.Right) <= SnapDistance)
                x = area.Right - Width;
            if (Math.Abs(Top - area.Top) <= SnapDistance)
                y = area.Top;
            if (Math.Abs(Bottom - area.Bottom) <= SnapDistance)
                y = area.Bottom - Height;

            if (x != Left || y != Top)
                Location = new Point(x, y);
        }

        private void ApplyRoundedRegion()
        {
            if (Width <= 0 || Height <= 0)
                return;

            using var path = CreateRoundedRectanglePath(new RectangleF(0, 0, Width, Height), 18F);
            var oldRegion = Region;
            Region = new Region(path);
            oldRegion?.Dispose();
        }

        private void ApplyRoundedRegion(Control control)
        {
            if (control.Width <= 0 || control.Height <= 0)
                return;

            using var path = CreateRoundedRectanglePath(new RectangleF(0, 0, control.Width, control.Height), theme.ButtonCornerRadius);
            var oldRegion = control.Region;
            control.Region = new Region(path);
            oldRegion?.Dispose();
        }

        private static GraphicsPath CreateRoundedRectanglePath(RectangleF bounds, float radius)
        {
            var path = new GraphicsPath();
            float diameter = Math.Min(radius * 2F, Math.Min(bounds.Width, bounds.Height));
            if (diameter <= 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

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

        private static Color Blend(Color from, Color to, float amount)
        {
            amount = Math.Clamp(amount, 0F, 1F);
            int r = (int)Math.Round(from.R + (to.R - from.R) * amount);
            int g = (int)Math.Round(from.G + (to.G - from.G) * amount);
            int b = (int)Math.Round(from.B + (to.B - from.B) * amount);
            return Color.FromArgb(from.A, r, g, b);
        }

        private sealed class MarqueeLabel : Control
        {
            private const int ScrollGap = 72;
            private const int ScrollStep = 2;
            private const int InitialPauseTicks = 28;

            private readonly System.Windows.Forms.Timer scrollTimer = new System.Windows.Forms.Timer();
            private int offset;
            private int pauseTicks = InitialPauseTicks;
            private int measuredTextWidth;
            private bool scrolling;

            public MarqueeLabel()
            {
                SetStyle(ControlStyles.AllPaintingInWmPaint
                    | ControlStyles.OptimizedDoubleBuffer
                    | ControlStyles.ResizeRedraw
                    | ControlStyles.SupportsTransparentBackColor
                    | ControlStyles.UserPaint, true);
                BackColor = Color.Transparent;
                scrollTimer.Interval = 35;
                scrollTimer.Tick += OnScrollTimerTick;
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing)
                    scrollTimer.Dispose();
                base.Dispose(disposing);
            }

            protected override void OnHandleCreated(EventArgs e)
            {
                base.OnHandleCreated(e);
                ResetScroll();
            }

            protected override void OnHandleDestroyed(EventArgs e)
            {
                scrollTimer.Enabled = false;
                base.OnHandleDestroyed(e);
            }

            protected override void OnTextChanged(EventArgs e)
            {
                base.OnTextChanged(e);
                ResetScroll();
            }

            protected override void OnFontChanged(EventArgs e)
            {
                base.OnFontChanged(e);
                ResetScroll();
            }

            protected override void OnSizeChanged(EventArgs e)
            {
                base.OnSizeChanged(e);
                ResetScroll();
            }

            protected override void OnVisibleChanged(EventArgs e)
            {
                base.OnVisibleChanged(e);
                UpdateTimer();
            }

            protected override void OnPaint(PaintEventArgs e)
            {
                base.OnPaint(e);
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                string text = Text;
                if (string.IsNullOrEmpty(text))
                    return;

                using var brush = new SolidBrush(ForeColor);
                using var format = new StringFormat
                {
                    FormatFlags = StringFormatFlags.NoWrap,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.None
                };
                e.Graphics.SetClip(ClientRectangle);

                if (!scrolling)
                {
                    e.Graphics.DrawString(text, Font, brush, ClientRectangle, format);
                    return;
                }

                e.Graphics.DrawString(text, Font, brush, new RectangleF(-offset, 0, measuredTextWidth + 8, Height), format);
                e.Graphics.DrawString(text, Font, brush, new RectangleF(measuredTextWidth + ScrollGap - offset, 0, measuredTextWidth + 8, Height), format);
            }

            private void ResetScroll()
            {
                offset = 0;
                pauseTicks = InitialPauseTicks;
                RecalculateTextWidth();
                UpdateTimer();
                Invalidate();
            }

            private void RecalculateTextWidth()
            {
                if (string.IsNullOrEmpty(Text) || Width <= 0)
                {
                    measuredTextWidth = 0;
                    scrolling = false;
                    return;
                }

                using var graphics = CreateGraphics();
                measuredTextWidth = (int)Math.Ceiling(graphics.MeasureString(Text, Font).Width);
                scrolling = measuredTextWidth > Width;
            }

            private void UpdateTimer()
            {
                scrollTimer.Enabled = scrolling && Visible && IsHandleCreated;
            }

            private void OnScrollTimerTick(object? sender, EventArgs e)
            {
                if (!scrolling)
                {
                    scrollTimer.Enabled = false;
                    return;
                }

                if (pauseTicks > 0)
                {
                    pauseTicks--;
                    return;
                }

                offset += ScrollStep;
                if (offset >= measuredTextWidth + ScrollGap)
                {
                    offset = 0;
                    pauseTicks = InitialPauseTicks;
                }
                Invalidate();
            }
        }
    }
}

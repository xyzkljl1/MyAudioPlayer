using System.ComponentModel;
using System.Drawing.Drawing2D;

namespace MyAudioPlayer
{
    internal class ElegantTrackBar : Control, ISupportInitialize
    {
        private int minimum = 0;
        private int maximum = 100;
        private int value = 0;
        private bool dragging = false;
        private bool hovering = false;
        private Color trackColor = Color.FromArgb(218, 226, 237);
        private Color fillColor = Color.FromArgb(58, 117, 214);
        private Color thumbColor = Color.White;
        private Color thumbBorderColor = Color.FromArgb(58, 117, 214);
        private Color tickColor = Color.FromArgb(118, 148, 163, 184);
        private Color shadowColor = Color.FromArgb(34, 58, 68, 84);
        private float trackHeight = 8F;
        private float thumbSize = 19F;
        private float activeThumbSize = 22F;

        public event EventHandler? Scroll;
        public event EventHandler? ValueChanged;

        public ElegantTrackBar()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw
                | ControlStyles.Selectable
                | ControlStyles.SupportsTransparentBackColor
                | ControlStyles.UserPaint, true);
            BackColor = Color.Transparent;
            TabStop = true;
            Height = 54;
        }

        public int Minimum
        {
            get { return minimum; }
            set
            {
                minimum = value;
                if (maximum < minimum)
                    maximum = minimum;
                Value = this.value;
                Invalidate();
            }
        }

        public int Maximum
        {
            get { return maximum; }
            set
            {
                maximum = Math.Max(minimum, value);
                Value = this.value;
                Invalidate();
            }
        }

        public int Value
        {
            get { return value; }
            set
            {
                int next = Math.Clamp(value, minimum, maximum);
                if (this.value == next)
                    return;
                this.value = next;
                Invalidate();
                ValueChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public int TickFrequency { get; set; } = 60;

        public Color TrackColor
        {
            get { return trackColor; }
            set { trackColor = value; Invalidate(); }
        }

        public Color FillColor
        {
            get { return fillColor; }
            set { fillColor = value; Invalidate(); }
        }

        public Color ThumbColor
        {
            get { return thumbColor; }
            set { thumbColor = value; Invalidate(); }
        }

        public Color ThumbBorderColor
        {
            get { return thumbBorderColor; }
            set { thumbBorderColor = value; Invalidate(); }
        }

        public Color TickColor
        {
            get { return tickColor; }
            set { tickColor = value; Invalidate(); }
        }

        public Color ShadowColor
        {
            get { return shadowColor; }
            set { shadowColor = value; Invalidate(); }
        }

        public float TrackHeight
        {
            get { return trackHeight; }
            set { trackHeight = Math.Clamp(value, 3F, 18F); Invalidate(); }
        }

        public float ThumbSize
        {
            get { return thumbSize; }
            set { thumbSize = Math.Clamp(value, 8F, 36F); Invalidate(); }
        }

        public float ActiveThumbSize
        {
            get { return activeThumbSize; }
            set { activeThumbSize = Math.Clamp(value, thumbSize, 42F); Invalidate(); }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

            var track = GetTrackBounds();
            using var trackBrush = new SolidBrush(trackColor);
            using var fillBrush = new SolidBrush(fillColor);
            using var thumbBrush = new SolidBrush(thumbColor);
            using var thumbBorder = new Pen(thumbBorderColor, 2F);
            using var shadowBrush = new SolidBrush(shadowColor);

            FillRoundRect(e.Graphics, trackBrush, track, track.Height / 2F);
            var fill = track;
            fill.Width = Math.Max(track.Height, GetThumbCenterX() - track.Left);
            FillRoundRect(e.Graphics, fillBrush, fill, track.Height / 2F);

            DrawTicks(e.Graphics, track);

            float thumbSize = dragging || hovering ? activeThumbSize : this.thumbSize;
            float cx = GetThumbCenterX();
            float cy = track.Top + track.Height / 2F;
            var shadow = new RectangleF(cx - thumbSize / 2F + 1.5F, cy - thumbSize / 2F + 2F, thumbSize, thumbSize);
            var thumb = new RectangleF(cx - thumbSize / 2F, cy - thumbSize / 2F, thumbSize, thumbSize);
            e.Graphics.FillEllipse(shadowBrush, shadow);
            e.Graphics.FillEllipse(thumbBrush, thumb);
            e.Graphics.DrawEllipse(thumbBorder, thumb);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hovering = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hovering = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Focus();
                dragging = true;
                Capture = true;
                SetValueFromX(e.X, true);
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (dragging)
                SetValueFromX(e.X, true);
            base.OnMouseMove(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (dragging && e.Button == MouseButtons.Left)
            {
                dragging = false;
                Capture = false;
                SetValueFromX(e.X, true);
                Invalidate();
            }
            base.OnMouseUp(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            int step = e.KeyCode == Keys.PageUp || e.KeyCode == Keys.PageDown
                ? Math.Max(1, TickFrequency)
                : 1;
            if (e.KeyCode == Keys.Left || e.KeyCode == Keys.Down || e.KeyCode == Keys.PageDown)
            {
                Value -= step;
                Scroll?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.Up || e.KeyCode == Keys.PageUp)
            {
                Value += step;
                Scroll?.Invoke(this, EventArgs.Empty);
                e.Handled = true;
            }
            base.OnKeyDown(e);
        }

        private RectangleF GetTrackBounds()
        {
            float horizontalPadding = 18F;
            float height = trackHeight;
            return new RectangleF(
                horizontalPadding,
                (Height - height) / 2F,
                Math.Max(1, Width - horizontalPadding * 2F),
                height);
        }

        private float GetThumbCenterX()
        {
            var track = GetTrackBounds();
            if (maximum <= minimum)
                return track.Left;
            float percent = (value - minimum) / (float)(maximum - minimum);
            return track.Left + track.Width * percent;
        }

        private void SetValueFromX(int x, bool raiseScroll)
        {
            var track = GetTrackBounds();
            float percent = Math.Clamp((x - track.Left) / track.Width, 0F, 1F);
            Value = minimum + (int)Math.Round((maximum - minimum) * percent);
            if (raiseScroll)
                Scroll?.Invoke(this, EventArgs.Empty);
        }

        private void DrawTicks(Graphics graphics, RectangleF track)
        {
            if (TickFrequency <= 0 || maximum <= minimum)
                return;

            using var tickPen = new Pen(tickColor, 1F);
            for (int tick = minimum + TickFrequency; tick < maximum; tick += TickFrequency)
            {
                float percent = (tick - minimum) / (float)(maximum - minimum);
                float x = track.Left + track.Width * percent;
                graphics.DrawLine(tickPen, x, track.Bottom + 7F, x, track.Bottom + 11F);
            }
        }

        private static void FillRoundRect(Graphics graphics, Brush brush, RectangleF bounds, float radius)
        {
            using var path = new GraphicsPath();
            float diameter = radius * 2F;
            path.AddArc(bounds.X, bounds.Y, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Y, diameter, diameter, 270, 90);
            path.AddArc(bounds.Right - diameter, bounds.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(bounds.X, bounds.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            graphics.FillPath(brush, path);
        }

        public void BeginInit()
        {
        }

        public void EndInit()
        {
        }
    }
}

using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using MyAudioPlayer.Themes;

namespace MyAudioPlayer
{
    internal class ThemedTabControl : TabControl
    {
        private const int TcmAdjustRect = 0x1328;
        private PlayerTheme theme = PlayerThemes.Resolve(PlayerThemes.DefaultId);

        public ThemedTabControl()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint
                | ControlStyles.OptimizedDoubleBuffer
                | ControlStyles.ResizeRedraw
                | ControlStyles.UserPaint, true);
            DrawMode = TabDrawMode.OwnerDrawFixed;
            Padding = new Point(10, 8);
        }

        public void ApplyTheme(PlayerTheme nextTheme)
        {
            theme = nextTheme;
            BackColor = theme.WindowBackColor;
            ForeColor = theme.TextColor;
            foreach (TabPage tabPage in TabPages)
            {
                tabPage.BackColor = theme.SurfaceColor;
                tabPage.ForeColor = theme.TextColor;
                tabPage.BorderStyle = BorderStyle.None;
                tabPage.Padding = System.Windows.Forms.Padding.Empty;
                tabPage.UseVisualStyleBackColor = false;
            }
            Invalidate();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            SetWindowTheme(Handle, "", "");
        }

        protected override void OnControlAdded(ControlEventArgs e)
        {
            base.OnControlAdded(e);
            if (e.Control is TabPage tabPage)
            {
                tabPage.BackColor = theme.SurfaceColor;
                tabPage.ForeColor = theme.TextColor;
                tabPage.BorderStyle = BorderStyle.None;
                tabPage.Padding = System.Windows.Forms.Padding.Empty;
                tabPage.UseVisualStyleBackColor = false;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var backgroundBrush = new SolidBrush(theme.WindowBackColor))
                e.Graphics.FillRectangle(backgroundBrush, ClientRectangle);

            DrawTabSurface(e.Graphics);
            for (int i = 0; i < TabPages.Count; i++)
                DrawTab(e.Graphics, i);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == TcmAdjustRect && !m.WParam.Equals(IntPtr.Zero))
            {
                base.WndProc(ref m);
                return;
            }
            base.WndProc(ref m);
        }

        private void DrawTabSurface(Graphics graphics)
        {
            var tabBottom = GetTabStripBottom();
            var contentBounds = new Rectangle(0, Math.Max(0, tabBottom - 1), Width - 1, Height - tabBottom);
            if (contentBounds.Width <= 0 || contentBounds.Height <= 0)
                return;

            using var contentBrush = new SolidBrush(theme.SurfaceColor);
            graphics.FillRectangle(contentBrush, contentBounds);
            using var borderPen = new Pen(theme.BorderColor);
            graphics.DrawRectangle(borderPen, contentBounds);
        }

        private void DrawTab(Graphics graphics, int index)
        {
            var selected = index == SelectedIndex;
            var bounds = GetTabRect(index);
            bounds.Inflate(selected ? 1 : 0, selected ? 2 : 0);
            bounds.Y += selected ? 0 : 3;
            bounds.Height -= selected ? 0 : 3;

            var radius = Math.Min(8, Math.Max(2, theme.ButtonCornerRadius / 2));
            using var path = CreateTopRoundedRectangle(bounds, radius);
            using (var brush = new SolidBrush(selected ? theme.SurfaceColor : theme.SubtleSurfaceColor))
                graphics.FillPath(brush, path);
            using (var pen = new Pen(theme.BorderColor))
                graphics.DrawPath(pen, path);

            var textColor = selected ? theme.TextColor : theme.MutedTextColor;
            TextRenderer.DrawText(
                graphics,
                TabPages[index].Text,
                Font,
                bounds,
                textColor,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        }

        private int GetTabStripBottom()
        {
            int bottom = 0;
            for (int i = 0; i < TabPages.Count; i++)
                bottom = Math.Max(bottom, GetTabRect(i).Bottom);
            return bottom <= 0 ? Font.Height + 14 : bottom;
        }

        private static GraphicsPath CreateTopRoundedRectangle(Rectangle bounds, int radius)
        {
            var path = new GraphicsPath();
            int diameter = radius * 2;
            if (diameter <= 0)
            {
                path.AddRectangle(bounds);
                return path;
            }

            path.AddArc(bounds.Left, bounds.Top, diameter, diameter, 180, 90);
            path.AddArc(bounds.Right - diameter, bounds.Top, diameter, diameter, 270, 90);
            path.AddLine(bounds.Right, bounds.Top + radius, bounds.Right, bounds.Bottom);
            path.AddLine(bounds.Right, bounds.Bottom, bounds.Left, bounds.Bottom);
            path.AddLine(bounds.Left, bounds.Bottom, bounds.Left, bounds.Top + radius);
            path.CloseFigure();
            return path;
        }

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hwnd, string? pszSubAppName, string? pszSubIdList);
    }
}

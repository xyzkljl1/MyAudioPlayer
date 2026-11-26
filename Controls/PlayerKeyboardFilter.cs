namespace MyAudioPlayer
{
    internal sealed class PlayerKeyboardFilter : IMessageFilter
    {
        private const int WmKeyDown = 0x0100;
        private const int WmKeyUp = 0x0101;
        private const int WmChar = 0x0102;
        private const int WmSysKeyDown = 0x0104;
        private const int WmSysKeyUp = 0x0105;
        private const int WmSysChar = 0x0106;
        private const int WmSysDeadChar = 0x0107;
        private const int WmUniChar = 0x0109;

        private static ToolStripDropDown? activePopup;
        private readonly Form mainWindow;

        public PlayerKeyboardFilter(Form mainWindow)
        {
            this.mainWindow = mainWindow;
        }

        public static T TrackPopup<T>(T popup) where T : ToolStripDropDown
        {
            popup.Opened += (_, _) => activePopup = popup;
            popup.Closed += (_, _) =>
            {
                if (activePopup == popup)
                    activePopup = null;
            };
            return popup;
        }

        public bool PreFilterMessage(ref Message message)
        {
            if (message.Msg is not (>= WmKeyDown and <= WmSysDeadChar or WmUniChar))
                return false;

            var target = Control.FromChildHandle(message.HWnd);
            if (target == null)
                return false;

            var form = target.FindForm();
            var popup = activePopup is { Visible: true } ? activePopup : null;
            bool targetsPopup = popup != null && (target == popup || popup.Contains(target));
            if (form != mainWindow && form is not MiniPlayerForm && !targetsPopup)
                return false;

            var key = (Keys)message.WParam.ToInt32();
            if ((message.Msg is WmSysKeyDown or WmSysKeyUp && key is Keys.F4 or Keys.Space)
                || (message.Msg == WmSysChar && key == Keys.Space))
                return false;

            // A non-activating dropdown can receive keys initially addressed to the search box.
            if (popup != null)
                return key != Keys.Escape || message.Msg is not (WmKeyDown or WmKeyUp or WmChar);

            if (target is PlaylistSearchBox)
            {
                return (key == Keys.Tab && message.Msg is WmKeyDown or WmKeyUp or WmChar)
                    || (key is Keys.PageUp or Keys.PageDown
                        && message.Msg is WmKeyDown or WmKeyUp
                        && (Control.ModifierKeys & Keys.Control) != Keys.None);
            }

            return true;
        }
    }

    internal sealed class PlaylistSearchBox : TextBox { }
}

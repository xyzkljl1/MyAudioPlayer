namespace MyAudioPlayer
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            var mainWindow = new MainWindow();
            var keyboardFilter = new PlayerKeyboardFilter(mainWindow, mainWindow.TogglePlayback, mainWindow.AdjustVolume);
            Application.AddMessageFilter(keyboardFilter);
            try
            {
                Application.Run(mainWindow);
            }
            finally
            {
                Application.RemoveMessageFilter(keyboardFilter);
            }
        }
    }
}

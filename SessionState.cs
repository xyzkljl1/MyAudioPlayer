using Newtonsoft.Json;

namespace MyAudioPlayer
{
    internal sealed class SessionState
    {
        private const string FileName = "session_state.json";

        public string PlayListType { get; set; } = "";
        public string PlayListPath { get; set; } = "";
        public string CurrentItemPath { get; set; } = "";

        public bool Matches(string playListType, string playListPath)
        {
            if (!string.Equals(PlayListType, playListType, StringComparison.Ordinal))
                return false;
            return PathsEqual(PlayListPath, playListPath);
        }

        public static SessionState Load()
        {
            var path = Path.Combine(AppContext.BaseDirectory, FileName);
            if (!File.Exists(path))
                return new SessionState();

            try
            {
                return JsonConvert.DeserializeObject<SessionState>(File.ReadAllText(path))
                    ?? new SessionState();
            }
            catch (Exception exception) when (
                exception is IOException ||
                exception is UnauthorizedAccessException ||
                exception is JsonException)
            {
                Console.WriteLine($"Failed to load session state: {exception.Message}");
                return new SessionState();
            }
        }

        public void Save()
        {
            var path = Path.Combine(AppContext.BaseDirectory, FileName);
            try
            {
                File.WriteAllText(path, JsonConvert.SerializeObject(this, Formatting.Indented));
            }
            catch (Exception exception) when (
                exception is IOException || exception is UnauthorizedAccessException)
            {
                Console.WriteLine($"Failed to save session state: {exception.Message}");
            }
        }

        private static bool PathsEqual(string left, string right)
        {
            try
            {
                return string.Equals(
                    NormalizePath(left),
                    NormalizePath(right),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception exception) when (
                exception is ArgumentException ||
                exception is NotSupportedException ||
                exception is PathTooLongException)
            {
                return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
            }
        }

        private static string NormalizePath(string path)
        {
            return Path.GetFullPath(path)
                .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}

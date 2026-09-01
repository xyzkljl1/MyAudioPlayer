using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace MyAudioPlayer
{
    public class Config
    {
        public class PlayListConfig
        {
            public string Type { get; set; } = "";
            public string Path { get; set; } = "";
            public float VolumeScale { get; set; } = 1.0f;

            public PlayListConfig() { }

            public PlayListConfig(string type, string path, float volumeScale = 1.0f)
            {
                Type = type;
                Path = path;
                VolumeScale = Math.Clamp(volumeScale, 0.0f, 1.0f);
            }
        }

        public static List<PlayListConfig> playLists = new List<PlayListConfig>();
        public static string DLServerAddress = "";
        public static string DLSiteFavDir = "";
        public static string MusicFavDir = "";
        public static string PlayerThemeId = "";
        public static void LoadJson()
        {
            var path = "config.json";
            if (System.IO.File.Exists(path))
            {
                using (JsonReader reader = new JsonTextReader(new System.IO.StreamReader(path)))
                {
                    JObject jsonObject = (JObject)JToken.ReadFrom(reader);
                    foreach (var fieldInfo in (new Config()).GetType().GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.Instance))
                        if (fieldInfo.FieldType == typeof(string))
                        {
                            if (jsonObject[fieldInfo.Name] != null
                                && jsonObject[fieldInfo.Name]!.Type == JTokenType.String)
                                fieldInfo.SetValue(null, jsonObject[fieldInfo.Name]!.ToString());
                        }
                        else if (fieldInfo.FieldType == typeof(bool))
                        {
                            if (jsonObject[fieldInfo.Name] != null
                                && jsonObject[fieldInfo.Name]!.Type == JTokenType.Boolean)
                                fieldInfo.SetValue(null, jsonObject[fieldInfo.Name]!.ToObject<Boolean>());
                        }
                        else if (fieldInfo.FieldType == typeof(int))
                        {
                            if (jsonObject[fieldInfo.Name] != null
                                && jsonObject[fieldInfo.Name]!.Type == JTokenType.Integer)
                                fieldInfo.SetValue(null, jsonObject[fieldInfo.Name]!.ToObject<int>());
                        }
                        else if(fieldInfo.FieldType == typeof(List<PlayListConfig>))
                        {
                            if (jsonObject[fieldInfo.Name] != null
                                && jsonObject[fieldInfo.Name]!.Type == JTokenType.Array)
                                fieldInfo.SetValue(null, ParsePlayLists(jsonObject[fieldInfo.Name]!));
                        }
                }
            }
        }

        private static List<PlayListConfig> ParsePlayLists(JToken token)
        {
            var tmp = new List<PlayListConfig>();
            foreach(var line in token.ToArray())
                if(line!=null&&line!.Type== JTokenType.Array)
                {
                    var arr = line.ToArray();
                    if (arr.Length >= 2)
                        tmp.Add(new PlayListConfig(
                            arr[0].ToString(),
                            arr[1].ToString(),
                            ParseVolumeScale(arr.Length >= 3 ? arr[2] : null)));
                }
            return tmp;
        }

        private static float ParseVolumeScale(JToken? token)
        {
            if (token == null)
                return 1.0f;
            if (token.Type == JTokenType.Float || token.Type == JTokenType.Integer)
                return Math.Clamp(token.ToObject<float>(), 0.0f, 1.0f);
            return 1.0f;
        }

        public static void SaveTheme()
        {
            var path = "config.json";
            JObject jsonObject = new JObject();
            if (System.IO.File.Exists(path))
            {
                using var reader = new JsonTextReader(new System.IO.StreamReader(path));
                jsonObject = (JObject)JToken.ReadFrom(reader);
            }

            jsonObject[nameof(PlayerThemeId)] = PlayerThemeId;
            System.IO.File.WriteAllText(path, jsonObject.ToString(Formatting.Indented));
        }
    }
}

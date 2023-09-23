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
        public static List<KeyValuePair<string, string>> playLists = new List<KeyValuePair<string, string>>();
        public static string DLServerAddress = "";

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
                        else if(fieldInfo.FieldType == typeof(List<KeyValuePair<string, string>>))
                        {
                            if (jsonObject[fieldInfo.Name] != null
                                && jsonObject[fieldInfo.Name]!.Type == JTokenType.Array)
                            {
                                List<KeyValuePair<string, string>> tmp = new List<KeyValuePair<string, string>>();
                                foreach(var line in jsonObject[fieldInfo.Name]!.ToArray())
                                    if(line!=null&&line!.Type== JTokenType.Array)
                                    {
                                        var arr = line.ToArray();
                                        if (arr.Length == 2)
                                            tmp.Add(new KeyValuePair<string, string>(arr[0].ToString(),arr[1].ToString()));
                                    }
                                fieldInfo.SetValue(null, tmp);
                            }
                        }
                }
            }
        }
    }
}

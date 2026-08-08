using Kittehface.Build.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static RainMeadow.Arena.Settings.SettingsManager;

namespace RainMeadow.Arena.Settings
{
    //manages the settings. Basically as long as person edited the config, it will be saved locally, if another does it. it will not be saved
    public class SettingsManager : CategorySettings
    {
        public int index = 0;
        public string folderPth;
        public int Index
        {
            get => index;
            set
            {
                if (index == value) return;
                index = value;
                LoadPreset();
            }
        }
        public string FilePth => Path.Combine(folderPth, $"Settings{Index}.json");
        public SettingsManager() : base("All Settings Manager")
        {
            RainMeadow.rainMeadowOptions.config.GetConfigPath();
            folderPth = Path.Combine(OptionInterface.ConfigHolder.configDirPath, "MeadowOnlineSettings");
        }
        public void LoadPreset()
        {
            var settings = LoadFromFile(FilePth);
            if (settings == null) return;
            var settingToCombine = new CategorySettings(id, settings);
            UpdateWithSavedSetting(settingToCombine);

        }
        public void SavePreset()
        {
            SaveToFile(FilePth, JsonConvert.SerializeObject(settings, Formatting.Indented));
        }
        public Dictionary<string, BaseSetting>? LoadFromFile(string filePth)
        {
            if (File.Exists(filePth))
            {
                var serializer = GetJsonSerializer();
                var val = serializer.Deserialize(File.OpenText(filePth), typeof(Dictionary<string, BaseSetting>));
                if (val is Dictionary<string, BaseSetting> list)
                    return list;
            }
            return null;
        }
        public void SaveToFile(string filePth, string text)
        {
            FileInfo fileInfo = new(filePth);
            if (!fileInfo.Directory.Exists)
            {
                fileInfo.Directory.Create();
            }
            File.WriteAllText(FilePth, JsonConvert.SerializeObject(settings, Formatting.Indented));
        }
        public JsonSerializer GetJsonSerializer()
        {
            var serializer = new JsonSerializer()
            {
                TypeNameHandling = TypeNameHandling.Auto
            };
            return serializer;
        }
     
    }
}

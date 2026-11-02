using Newtonsoft.Json;
using OpenPronounceSharp.Model;
using System.Reflection;

namespace OpenPronounceSharp.Manager
{
    public class SettingManager
    {
        public static string Location => Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        public static string ConfigFolder => Path.Combine(Location, "Config");

        public static Setting Setting { get; private set; } = new Setting();

        static SettingManager()
        {
            if(!Directory.Exists(ConfigFolder))
            {
                Directory.CreateDirectory(ConfigFolder);
            }

            LoadConfig();
        }

        public static string ConfigFilePath
        {
            get
            {
                return Path.Combine(ConfigFolder, "Setting.json");
            }
        }

        public static void LoadConfig(string configFilePath = null)
        {
            configFilePath ??= ConfigFilePath;

            if (File.Exists(configFilePath))
            {
                Setting = (Setting)JsonConvert.DeserializeObject(File.ReadAllText(configFilePath), typeof(Setting));
            }
        }

        public static void SaveConfig(Setting setting)
        {
            Setting = setting;
            string content = JsonConvert.SerializeObject(setting, Formatting.Indented);

            File.WriteAllText(ConfigFilePath, content);
        }
    }
}

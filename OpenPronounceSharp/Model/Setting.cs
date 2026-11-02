using OpenPronounceSharp.Model;

namespace OpenPronounceSharp.Model
{
    public class Setting
    {
        public bool UseProxy { get; set; }
        public bool IsOffline { get; set; }
        public string HF_ENDPOINT { get; set; }
        public string HF_HUB_CACHE { get; set; }
        public string Speech_LocalModelDatFilePath { get; set; }
        public string Phones_LocalModelDatFilePath { get; set; }
        public AudioParserType AudioParserType { get; set; } = AudioParserType.None;
        public bool EnableLog { get; set; }
        public bool ShowConsoleWindow { get; set; } = true;
    }
}

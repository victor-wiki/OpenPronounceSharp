using EdgeTtsSharp;
using HuggingFaceHubSharp;
using NumSharp;
using OpenPronounceSharp.Helper;
using SharpUtility.Extension;
using SharpUtility.Helper;

namespace OpenPronounceSharp
{
    public class tts
    {
        private static Dictionary<string, dynamic> _piper_voices = new Dictionary<string, dynamic>();
        private static Dictionary<string, dynamic> _kokoro_pipelines = new Dictionary<string, dynamic>();

        public const string DEFAULT_BACKEND = "gtts";

        // Piper voices on Hugging Face (rhasspy/piper-voices), one medium-quality voice per language.
        public const string PIPER_VOICES_REPO = "rhasspy/piper-voices";

        public static readonly Dictionary<string, string> PIPER_DEFAULT_VOICES = new Dictionary<string, string>()
        {
            {"ar", "ar_JO-kareem-medium"},
            {"de", "de_DE-thorsten-medium"},
            {"en", "en_US-lessac-medium"},
            {"es", "es_ES-davefx-medium"},
            {"fr", "fr_FR-siwis-medium"},
            {"hi", "hi_IN-pratham-medium"},
            {"it", "it_IT-paola-medium"},
            {"nl", "nl_NL-mls-medium"},
            {"pl", "pl_PL-gosia-medium"},
            {"pt", "pt_BR-faber-medium"},
            {"ru", "ru_RU-irina-medium"},
            {"sv", "sv_SE-nst-medium"},
            {"tr", "tr_TR-dfki-medium"},
            {"zh", "zh_CN-huayan-medium"}
        };

        // Kokoro-82M: our 2-letter language codes -> (Kokoro lang code, default voice).
        public const string KOKORO_REPO = "hexgrad/Kokoro-82M";
        public const int KOKORO_SAMPLE_RATE = 24000;

        public static readonly Dictionary<string, (string, string)> KOKORO_LANGUAGES = new Dictionary<string, (string, string)>()
        {
            {"en", ("a", "af_heart") },
            {"es", ("e", "ef_dora") },
            {"fr", ("f", "ff_siwis") },
            {"hi", ("h", "hf_alpha") },
            {"it", ("i", "if_sara") },
            {"ja", ("j", "jf_alpha") },
            {"pt", ("p", "pf_dora") },
            {"zh", ("z", "zf_xiaobei") }
        };

        public static readonly Dictionary<string, (Delegate, Delegate)> BACKENDS = new Dictionary<string, (Delegate, Delegate)>()
        {
            {"gtts", (synthesize_gtts, gtts_default_voice) },
            {"piper", (synthesize_piper, piper_default_voice) },
            //{"kokoro", (synthesize_kokoro, kokoro_default_voice) } ////to do
        };

        /// <summary>
        /// Return the effective ``(backend, voice)`` from the arguments, the environment and the defaults.
        /// </summary>
        /// <param name="lang"></param>
        /// <param name="backend"></param>
        /// <param name="voice"></param>
        /// <returns></returns>
        public static (string, string) resolve(string lang, string backend = null, string voice = null)
        {
            backend = backend ?? Environment.GetEnvironmentVariable("OPENPRONOUNCE_TTS") ?? DEFAULT_BACKEND;

            if (!BACKENDS.ContainsKey(backend))
            {
                throw new ValueError($"Unknown TTS backend {backend} (OPENPRONOUNCE_TTS), expected one of: {BACKENDS.Keys.ToFridenlyString()}");
            }

            voice = voice ?? Environment.GetEnvironmentVariable("OPENPRONOUNCE_TTS_VOICE") ?? MethodHelper.Invoke(BACKENDS[backend].Item2, new Dictionary<dynamic, dynamic>() { { "lang", lang } }) as string;

            return (backend, voice);
        }

        /// <summary>
        /// Return the default gTTS "voice", i.e. the Google Translate domain.
        /// </summary>
        /// <param name="lang"></param>
        /// <returns></returns>
        public static string gtts_default_voice(string lang)
        {
            return "com";
        }

        /// <summary>
        /// Synthesize with Google Translate TTS (network). Returns ``(waveform, sample_rate)``.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="lang"></param>
        /// <param name="voice"></param>
        /// <returns></returns>
        public static async Task<string> synthesize_gtts(string text, string lang, string voice)
        {
            var edge_tts = new Communicate(text, "en-US-JennyNeural"); //"en-US-EmmaMultilingualNeural"

            string filePath = Path.Combine(LocationHelper.TEMP_DIR, $"{Guid.NewGuid()}.mp3");

            await edge_tts.SaveAsync(filePath);

            return filePath;
        }

        /// <summary>
        /// Return the default Piper voice for ``lang`` or raise if we have none.
        /// </summary>
        /// <param name="lang"></param>
        /// <returns></returns>
        public static string piper_default_voice(string lang)
        {
            try
            {
                return PIPER_DEFAULT_VOICES[lang];
            }
            catch (Exception ex)
            {
                throw new ValueError($"No default Piper voice for language {lang}. Pick one at " +
                    "https://huggingface.co/{PIPER_VOICES_REPO} (e.g. 'en_US-lessac-medium') and set " +
                    "OPENPRONOUNCE_TTS_VOICE or pass voice=... to text2speech().");
            }
        }

        /// <summary>
        /// Run the given backend and return ``(waveform, sample_rate)`` (mono float32).
        /// </summary>
        /// <param name="text"></param>
        /// <param name="lang"></param>
        /// <param name="backend"></param>
        /// <param name="voice"></param>
        /// <returns></returns>
        public static dynamic synthesize(string text, string lang, string backend, string voice)
        {
            return MethodHelper.Invoke(BACKENDS[backend].Item1, new Dictionary<dynamic, dynamic>() { { "text", text }, { "lang", lang }, { "voice", voice } });
        }

        /// <summary>
        /// Download (once, into the Hugging Face cache) and return the .onnx and .json paths of a Piper voice.
        /// </summary>
        /// <param name="voice"></param>
        /// <returns></returns>
        private static async Task<(dynamic, dynamic)> _piper_voice_files(string voice)
        {
            string lang_code = null;
            string name = null;
            string quality = null;
            string family = null;

            try
            {
                var items = voice.Split('-', 2);

                lang_code = items[0];
                name = items[1];
                quality = items[2];

                family = lang_code.Split('_')[0];

            }
            catch (Exception)
            {
                throw new ValueError($"Invalid Piper voice {voice}, expected '<lang>_<REGION>-<name>-<quality>' like 'en_US-lessac-medium'.");
            }

            var folder = $"{family}/{lang_code}/{name}/{quality}";
            var model = await file_download.hf_hub_download(PIPER_VOICES_REPO, $"{folder}/{voice}.onnx");
            var config = await file_download.hf_hub_download(PIPER_VOICES_REPO, $"{folder}/{voice}.onnx.json");

            return (model, config);
        }

        /// <summary>
        /// Synthesize with Piper (offline). Returns ``(waveform, sample_rate)``.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="lang"></param>
        /// <param name="voice"></param>
        public static async Task<(NDArray, int)> synthesize_piper(string text, string lang, string voice)
        {
            if (!_piper_voices.ContainsKey(voice))
            {
                var (model, config) = await _piper_voice_files(voice);

                //_piper_voices[voice] = piper.PiperVoice.load(model, config); ////to do
            }

            return (null, 0); ////to do
        }
    }
}

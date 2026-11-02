using OpenPronounceSharp.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace OpenPronounceSharp
{
    public class languages
    {
        public const string DEFAULT_LANGUAGE = "en";

        public static readonly Dictionary<string, Language> LANGUAGES = new Dictionary<string, Language>()
        {
            {"en", new Language(){code="en", espeak= "en-us", asr_model ="facebook/wav2vec2-large-960h", name ="English", acoustic_good=6.0f } },
            {"fr", new Language(){code="fr", espeak= "fr-fr", asr_model ="jonatasgrosman/wav2vec2-large-xlsr-53-french", name ="French", acoustic_good=9.0f } },
            {"es", new Language(){code="es", espeak= "es", asr_model ="jonatasgrosman/wav2vec2-large-xlsr-53-spanish", name ="Spanish", acoustic_good=10.0f } },
            {"de", new Language(){code="de", espeak= "de", asr_model ="jonatasgrosman/wav2vec2-large-xlsr-53-german",name = "German", acoustic_good=10.0f } },
            {"it", new Language(){code="it", espeak= "it", asr_model ="jonatasgrosman/wav2vec2-large-xlsr-53-italian",name = "Italian", acoustic_good=9.0f } },
            {"pt", new Language(){code="pt", espeak= "pt-br",asr_model = "jonatasgrosman/wav2vec2-large-xlsr-53-portuguese",name = "Portuguese", acoustic_good=11.0f } },
            {"nl", new Language(){code="nl", espeak= "nl", asr_model ="jonatasgrosman/wav2vec2-large-xlsr-53-dutch",name = "Dutch", acoustic_good=13.0f } }
        };

        public static Language get_language(string code)
        {
            return LANGUAGES[code];
        }
    }
}

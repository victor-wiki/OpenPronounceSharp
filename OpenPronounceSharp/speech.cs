using FastDtwSharp;
using HuggingFaceHubSharp;
using Newtonsoft.Json;
using NumSharp;
using OpenPronounceSharp.Levenshtein;
using OpenPronounceSharp.Model;
using PhonemizerSharp;
using SharpUtility.Extension;
using SharpUtility.Helper;
using System.Text.RegularExpressions;
using TorchSharp;
using TransformersSharp.feature_extraction_utils;
using TransformersSharp.Helper;
using TransformersSharp.Model;
using TransformersSharp.models.wav2vec2;

namespace OpenPronounceSharp
{
    public class speech
    {
        private static Delegate euclidean = OpenPronounceSharp.distance.euclidean;
        private const string _WORD_RE = @"\b[\w']+\b";
        public const string MODEL_NAME = "facebook/wav2vec2-large-960h";
        public const int SAMPLING_RATE = 16000;
        private static Wav2Vec2Processor _processor = null;
        private static Wav2Vec2ForCTC _model_ctc = null;

        /// <summary>
        /// Threshold above which a word is considered mispronounced:
        /// ratio of edited phonemes over the number of expected phonemes.
        /// </summary>
        public const float WORD_ERROR_THRESHOLD = 0.4f;

        // Mean per-step DTW distance between Wav2Vec2 frames of the learner and of the TTS
        // reference. Measured on the bundled samples: ~6 for a clean native-like reading,
        // ~10 for a good reading by a different voice, ~12-15 for a wrong sentence. On
        // speechocean762 the median is 11.5 (9.3 to 13.6 for the middle half).
        public const float ACOUSTIC_DISTANCE_GOOD = 6.0f;
        public const float ACOUSTIC_DISTANCE_BAD = 15.0f;

        // The English embeddings put two native voices of another language further apart
        // (9-13 instead of ~6.5), so each language carries its own "good" distance
        // (``Language.acoustic_good``); the good-to-bad span stays the same.
        public const float ACOUSTIC_DISTANCE_SPAN = ACOUSTIC_DISTANCE_BAD - ACOUSTIC_DISTANCE_GOOD;

        // Calibrated on 500 speechocean762 utterances (see benchmarks/README.md): Spearman 0.65
        // with the human total score. Heavier acoustic weights correlate slightly better (0.68
        // at 0.7/0.2/0.1) but no longer punish a wrong sentence, so the phone and word terms
        // keep the majority of the weight.
        public static readonly Dictionary<string, float> SCORE_WEIGHTS = new Dictionary<string, float>() { { "acoustic", 0.3f }, { "phonemes", 0.4f }, { "words", 0.3f } };

        /// <summary>
        /// Load a Wav2Vec2 processor and CTC model on first use (cached per checkpoint).
        /// 
        /// ``Wav2Vec2ForCTC`` embeds a ``Wav2Vec2Model`` (``.wav2vec2``), so a single
        /// checkpoint serves both transcription (CTC head) and embedding extraction.
        /// </summary>
        /// <param name="model_name"></param>
        /// <returns></returns>
        private static async Task<(Wav2Vec2Processor processor, Wav2Vec2ForCTC model_ctc)> _load_models(string model_name = MODEL_NAME)
        {
            Dictionary<string, dynamic> kwargs = new Dictionary<string, dynamic>();

            if (_processor is null)
            {
                kwargs["cls"] = typeof(Wav2Vec2Processor);
                _processor = await Wav2Vec2Processor.from_pretrained(model_name, kwargs: kwargs) as Wav2Vec2Processor;
            }

            if (_model_ctc is null)
            {
                kwargs["cls"] = typeof(Wav2Vec2ForCTC);
                kwargs["LocalModelDatFilePath"] = Environment.GetEnvironmentVariable("Speech_LocalModelDatFilePath");
                kwargs["ShowStateDictReport"] = constants.is_true(Environment.GetEnvironmentVariable("SHOW_STATE_DICT_REPORT‌"));

                _model_ctc = ((await Wav2Vec2ForCTC.from_pretrained(model_name, kwargs: kwargs)).Item1 as torch.nn.Module).to(Utility.get_device()) as Wav2Vec2ForCTC;

                _model_ctc.eval();
            }

            return (_processor, _model_ctc);
        }

        /// <summary>
        /// Embedding extractor: always the English checkpoint, whatever the language.
        /// </summary>
        /// <returns></returns>
        private static async Task<Wav2Vec2Model> _get_model()
        {
            return (await _load_models(MODEL_NAME)).Item2.wav2vec2;
        }

        /// <summary>
        /// Assess how well ``audio_1`` (16 kHz mono waveform) pronounces ``text_reference``.
        /// </summary>
        /// <param name="audio_1"></param>
        /// <param name="text_reference"></param>
        /// <param name="sampling_rate"></param>
        /// <param name="use_phone_model"></param>
        /// <param name="lang">selects the language (see :data:`openpronounce.languages.LANGUAGES`);
        /// it drives the reference TTS voice, the phonemizer and the word transcription model.
        /// </param>
        /// <returns>a JSON-serializable dict with ``score`` (0-100), ``distance``,
        /// ``differences`` (per-word errors, phonemes, feedback), ``transcribe``,
        /// ``language`` and ``prosody`` (``f0`` and ``energy`` contours).
        /// 
        /// When the phone recognizer is enabled (default, see :mod:`openpronounce.phones`),
        /// ``differences.errors`` and ``differences.phoneme_error_rate`` come from phones
        /// recognized directly in the audio; otherwise they are derived from the word
        /// transcription.
        /// </returns>
        public static async Task<Dictionary<string, dynamic>> compare_audio_with_text(NDArray audio_1, string text_reference, int sampling_rate = SAMPLING_RATE, bool? use_phone_model = null, string lang = languages.DEFAULT_LANGUAGE)
        {
            if (use_phone_model == null)
            {
                use_phone_model = phones.is_enabled();
            }

            lang = languages.get_language(lang).code;

            var emb_1 = await extract_embeddings(audio_1, sampling_rate);
    
            var reference_file = await audio.text2speech(text_reference, lang: lang);
            var audio_2 = (await audio.load(reference_file, sr: sampling_rate)).Item1;
            var emb_2 =  await extract_embeddings(audio_2, sampling_rate);             

            var distanceResult = FastDtw.fastdtw(emb_1, emb_2, dist: euclidean);

            float distance = distanceResult.Item1;
            List<(long, long)> path = distanceResult.Item2;

            var acoustic_distance =(float)(distance * 1.0 / Math.Max(1, path.Count));
            distance = (int)distance;

            var transcription = await transcribe(audio_1, lang);

            Dictionary<string,dynamic> differences = compare_transcriptions(transcription, text_reference, lang);

            if (use_phone_model == true)
            {
                PhoneRecognition recognition = await phones.recognize_phones(audio_1, sampling_rate, lang: lang);
                var phone_result = phones.compare_phones(recognition, text_reference, lang);

                differences.update(new Dictionary<string, dynamic>()
                {
                    {"errors", phone_result["errors"]},
                    {"words_with_errors", phone_result["words_with_errors"]},
                    {"phoneme_error_rate", phone_result["phone_error_rate"]},
                    {"expected_phones", phone_result["expected_phones"]},
                    {"heard_phones", phone_result["heard_phones"]},
                    {"heard_phones_confidence", phone_result["heard_phones_confidence"]},
                    {"feedback", _feedback(phone_result["words_with_errors"])}
                });
            }

            var score = compute_pronunciation_score(acoustic_distance, differences["phoneme_error_rate"], differences["word_error_rate"], lang);
            //var energy = extract_energy(audio_1); ////to do
            //var f0 = interpolate_f0(extract_f0(audio_1, sampling_rate));

            return new Dictionary<string, dynamic>()
            {
                {"score", score },
                {"distance", distance },
                {"acoustic_distance", Math.Round(acoustic_distance, 3) },
                {"differences", differences },
                {"feedback", differences["feedback"] },
                {"transcribe", differences["transcribe"] },
                {"language", lang },
                //{"prosody", new Dictionary<string,dynamic>() { { "f0", f0.tolist() }, { "energy", energy.tolist() }  } } ////to do
            };
        }

        /// <summary>
        /// Linearly interpolate unvoiced (0) frames so the pitch curve has no gaps.
        /// </summary>
        /// <param name="f0"></param>
        /// <returns></returns>
        public static dynamic interpolate_f0(NDArray f0)
        {
            f0 = np.asanyarray(f0, dtype: np.float32);
            var mask = f0 > 0;

            if (!mask.any().item<bool>())
            {
                return f0;
            }

            return np.interp(np.arange(LengthHelper.GetNDArrayLength(f0)), np.where(mask)[0], f0[mask]);
        }

        /// <summary>
        /// Fundamental frequency (pitch) contour, unvoiced frames set to 0.
        /// </summary>
        /// <param name="audio_waveform"></param>
        /// <param name="sr"></param>
        /// <returns></returns>
        //public static NDArray extract_f0(NDArray audio_waveform, int sr = SAMPLING_RATE)
        //{
        //    var (f0, _, _) = librosa.pyin(audio_waveform, fmin: 50, fmax: 300, sr: sr);

        //    return np.nan_to_num(f0);
        //}

        /// <summary>
        /// RMS energy contour scaled to 0-250 so it can share an axis with F0.
        /// </summary>
        /// <param name="audio_waveform"></param>
        /// <returns></returns>
        //public static dynamic extract_energy(NDArray audio_waveform)
        //{
        //    var energy = librosa.feature.rms(y : audio_waveform);
        //    var scaler = MinMaxScaler(feature_range : (0, 250));

        //    return scaler.fit_transform(energy.T).flatten();
        //}

        /// <summary>
        /// Combine length-independent measures into a 0-100 score.
        /// </summary>
        /// <param name="acoustic_distance"></param>
        /// <param name="phoneme_error_rate"></param>
        /// <param name="word_error_rate"></param>
        /// <param name="lang"></param>
        /// <returns></returns>
        public static float compute_pronunciation_score(float acoustic_distance, float phoneme_error_rate, float word_error_rate, string lang = languages.DEFAULT_LANGUAGE)
        {
            var good = languages.get_language(lang).acoustic_good;
            var acoustic_score = 100 * (1 - (acoustic_distance - good) / ACOUSTIC_DISTANCE_SPAN);
            var phoneme_score = 100 * (1 - phoneme_error_rate);
            var word_score = 100 * (1 - word_error_rate);

            Func<float, float> clip = (x) => (float)Math.Min(100.0, Math.Max(0.0f, x)); // noqa: E731

            var final_score = SCORE_WEIGHTS["acoustic"] * clip(acoustic_score) + SCORE_WEIGHTS["phonemes"] * clip(phoneme_score) + SCORE_WEIGHTS["words"] * clip(word_score);

            return (float)Math.Round(clip(final_score), 2);
        }

        /// <summary>
        /// Compare an automatic transcription with the expected text, word by word.
        /// </summary>
        /// <param name="transcription"></param>
        /// <param name="text_reference"></param>
        /// <param name="lang"></param>
        /// <returns>a JSON-serializable dict with distances, per-word errors and feedback.</returns>
        public static Dictionary<string, dynamic> compare_transcriptions(string transcription, string text_reference, string lang = languages.DEFAULT_LANGUAGE)
        {
            var transcription_clean = transcription.ToLower().Trim();
            var reference_clean = text_reference.ToLower().Trim();

            var word_distance = Fastenshtein.Levenshtein.Distance(transcription_clean, reference_clean);

            var (expected_phonemes, _) = get_phonemes_with_word_mapping(text_reference, lang);
            var (transcribed_phonemes, transcribed_map) = get_phonemes_with_word_mapping(transcription_clean, lang);

            // Global phoneme distance (DTW on codepoints, kept for the score and the charts)
            var expected_seq = get_phoneme_embeddings(string.Join(" ", expected_phonemes));
            var transcribed_seq = get_phoneme_embeddings(string.Join(" ", transcribed_phonemes));

            var expected_seq_len = LengthHelper.GetNDArrayLength(expected_seq);
            var transcribed_seq_len = LengthHelper.GetNDArrayLength(transcribed_seq);

            float distance = 0;

            if (expected_seq_len > 0 && transcribed_seq_len > 0)
            {
                (distance, _) = FastDtw.fastdtw(expected_seq, transcribed_seq, dist: euclidean);
            }
            else
            {
                distance = (float)(Math.Max(expected_seq_len, transcribed_seq_len));
            }

            var alignment_map = _align_phoneme_indices(expected_phonemes, transcribed_phonemes);

            var errors = new List<Dictionary<string, dynamic>>();
            var words_with_errors = new List<string>();
            var current_phoneme_idx = 0;

            List<string> expected_seg = null;
            List<string> actual_seg = null;

            foreach (var word in _words(text_reference))
            {
                var word_phonemes = _phonemize_word(word, lang);

                if (!word_phonemes.HasValue())
                {
                    continue;
                }

                var word_indices = RangeHelper.GetRange(current_phoneme_idx, current_phoneme_idx + word_phonemes.Length);
                current_phoneme_idx += word_phonemes.Length;

                var matched = new HashSet<int>();

                foreach (var idx in word_indices)
                {
                    if (idx < alignment_map.Count)
                    {
                        matched.update(alignment_map[idx]);
                    }
                }

                if (matched.Count == 0)
                {
                    errors.Add(new Dictionary<string, dynamic>()
                    {
                        {"position", word_indices.FirstOrDefault() },
                        {"expected", string.Join("", word_phonemes) },
                        {"actual", "" },
                        {"word", word },
                        {"actual_word", "" }
                    });

                    words_with_errors.Add(word);

                    continue;
                }

                var sorted_matched = matched.OrderBy(item => item).ToHashSet();
                var actual_words = new List<string>();

                foreach (var tidx in sorted_matched)
                {
                    var w = transcribed_map.GetValueOrDefault(tidx);

                    if (w is not null && (!actual_words.HasValue() || actual_words.LastOrDefault() != w))
                    {
                        actual_words.Add(w);
                    }
                }

                expected_seg = word_indices.Select(i => expected_phonemes[i]).ToList();
                actual_seg = sorted_matched.Select(i => transcribed_phonemes[i]).ToList();

                if (LevenshteinDistance.distance(expected_seg, actual_seg) > expected_seg.Count * WORD_ERROR_THRESHOLD)
                {
                    errors.Add(new Dictionary<string, dynamic>()
                    {
                        {"position", word_indices.FirstOrDefault() },
                        {"expected", string.Join("", expected_seg) },
                        {"actual", string.Join("", actual_seg) },
                        {"word", word },
                        {"actual_word", string.Join(" ", actual_words) }
                    });

                    words_with_errors.Add(word);
                }
            }

            // De-duplicate while preserving order
            words_with_errors = words_with_errors.ToList();

            var expected_words = _words(text_reference).Select(w => w.ToLower()).ToList();
            var transcribed_words = _words(transcription_clean).Select(w => w.ToLower()).ToList();
            var word_error_rate = LevenshteinDistance.distance(expected_words, transcribed_words) * 1.0f / Math.Max(1, expected_words.Count);
            var phoneme_error_rate = LevenshteinDistance.distance(expected_phonemes, transcribed_phonemes) * 1.0f / Math.Max(1, expected_phonemes.Count);

            var feedback = _feedback(words_with_errors);

            var (expected_vector, transcribed_vector) = align_sequences_dtw(TorchHelper.Get2DimensonArray<int>(expected_seq), TorchHelper.Get2DimensonArray<int>(transcribed_seq));

            return new Dictionary<string, dynamic>()
            {
                {"word_distance", word_distance},
                {"phoneme_distance", distance},
                {"word_error_rate", (float)Math.Round(word_error_rate, 4)},
                {"phoneme_error_rate", (float)Math.Round(phoneme_error_rate, 4)},
                {"errors", errors},
                {"feedback", feedback},
                {"transcribe", transcription},
                {"expected_vector", TorchHelper.GetFloatArray(expected_vector.astype(np.float32).tolist())},
                {"transcribed_vector", TorchHelper.GetFloatArray(transcribed_vector.astype(np.float32).tolist())},
                {"expected_phonemes", expected_phonemes},
                {"transcribed_phonemes", transcribed_phonemes},
                {"words_with_errors", words_with_errors},
            };
        }

        /// <summary>
        /// Align two 1-D numeric sequences (given as lists of ``[x]``) with DTW.
        /// </summary>
        /// <param name="seq1"></param>
        /// <param name="seq2"></param>
        /// <returns></returns>
        public static (NDArray, NDArray) align_sequences_dtw(int[,] seq1, int[,] seq2)
        {
            if (seq1.Length == 0 || seq2.Length == 0)
            {
                return (np.empty(0), np.empty(0));
            }

            var (_, path) = FastDtw.fastdtw(seq1, seq2, dist: euclidean);

            var aligned_seq1 = np.array(path.Select(item => seq1[item.Item1, 0]).ToArray());
            var aligned_seq2 = np.array(path.Select(item => seq2[item.Item2, 0]).ToArray());

            return (aligned_seq1, aligned_seq2);
        }

        private static string _feedback(List<string> words_with_errors)
        {
            var feedback = "🔊 Feedback on your pronunciation:\n";

            if (words_with_errors.HasValue())
            {
                feedback += "❌ You need to better pronounce these words: " + words_with_errors.ToFridenlyString() + "\n";
            }
            else
            {
                feedback += "✅ Your pronunciation is excellent! 🎉\n";
            }

            return feedback;
        }

        /// <summary>
        /// Map every expected phoneme index to the set of transcribed phoneme indices it aligns with.
        /// </summary>
        /// <param name="expected_phonemes"></param>
        /// <param name="transcribed_phonemes"></param>
        /// <returns></returns>
        private static List<HashSet<int>> _align_phoneme_indices(List<string> expected_phonemes, List<string> transcribed_phonemes)
        {
            var alignment_map = RangeHelper.GetRange(expected_phonemes.Count).Select(item => new HashSet<int>()).ToList();

            foreach (Opcode item in LevenshteinOpcodes.opcodes(expected_phonemes, transcribed_phonemes)._opcodes)
            {
                var tag = item.tag;
                var i1 = item.src_start;
                var i2 = item.src_end;
                var j1 = item.dest_start;
                var j2 = item.dest_end;

                if (tag == "equal")
                {
                    foreach (var (k, l) in RangeHelper.GetRange(i1, i2).Zip(RangeHelper.GetRange(j1, j2)))
                    {
                        alignment_map[k].Add(l);
                    }
                }
                else if (tag == "replace")
                {
                    // Spread the replaced range proportionally, so that "I'm" (3 phonemes)
                    // can align with "I M" (4 phonemes) without mapping everything to everything.
                    var len_i = i2 - i1;
                    var len_j = j2 - j1;

                    foreach (var k in RangeHelper.GetRange(i1, i2))
                    {
                        var start_j = j1 + (int)((k - i1) * len_j * 1.0 / len_i);
                        var end_j = j1 + (int)((k - i1 + 1) * len_j * 1.0 / len_i);

                        if (start_j == end_j && len_j > 0)
                        {
                            alignment_map[k].Add(Math.Min(start_j, j2 - 1));
                        }
                        else
                        {
                            alignment_map[k].update(RangeHelper.GetRange(start_j, end_j).ToHashSet());
                        }
                    }
                }
                else if (tag == "insert")
                {
                    // Extra transcribed words ("hell no" for "hello") are attached to the
                    // previous expected word so the feedback shows everything that was heard.
                    var k = i1 > 0 ? i1 - 1 : i1;

                    if (k < alignment_map.Count)
                    {
                        alignment_map[k].update(RangeHelper.GetRange(j1, j2).ToHashSet());
                    }
                }

                // "delete" (missing expected phonemes) produces no mapping.
            }

            return alignment_map;
        }

        /// <summary>
        /// Turn a phoneme sequence into a numeric (n, 1) array (codepoints) usable by DTW.
        /// </summary>
        /// <param name="phoneme_seq"></param>
        /// <returns></returns>
        public static NDArray get_phoneme_embeddings(string phoneme_seq)
        {
            return np.array(phoneme_seq.Select(p => (int)p).ToArray(), dtype: typeof(float)).reshape(-1, 1);
        }

        /// <summary>
        /// Return ``(phonemes, phoneme_to_word)`` where ``phoneme_to_word[i]`` is the word phoneme ``i`` belongs to.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="lang"></param>
        /// <returns></returns>
        public static (List<string>, Dictionary<int, string>) get_phonemes_with_word_mapping(string text, string lang = languages.DEFAULT_LANGUAGE)
        {
            var phonemes = new List<string>();
            var phoneme_to_word = new Dictionary<int, string>();

            foreach (var word in _words(text))
            {
                foreach (var phoneme in _phonemize_word(word, lang))
                {
                    phoneme_to_word[phonemes.Count] = word;

                    phonemes.Add(phoneme);
                }
            }

            return (phonemes, phoneme_to_word);
        }

        /// <summary>
        /// Return the IPA phonemes of a single word (espeak first, festival as fallback).
        /// 
        /// The word is lower-cased first: espeak spells upper-case words letter by letter
        /// ("IT" -> /aɪtiː/), which would flag every word of an upper-case sentence as wrong.
        /// </summary>
        /// <param name="word"></param>
        /// <param name="lang"></param>
        /// <returns></returns>
        private static string[] _phonemize_word(string word, string lang = languages.DEFAULT_LANGUAGE)
        {
            word = word.ToLower();
            var language = languages.get_language(lang).espeak;

            foreach (var backend in ArrayHelper.GetArray("espeak"/*, "festival"*/)) ////to do
            {
                var result = Phonemizer.phonemize(word, language: language, backend: backend, strip: true, preserve_punctuation: false);

                return result.Split(' ');
            }

            return null;
        }

        /// <summary>
        /// Split text into words, ignoring punctuation (mirrors the frontend logic).
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        private static List<string> _words(string text)
        {
            return Regex.Matches(text, _WORD_RE).Select(item => item.Value).ToList();
        }     

        /// <summary>
        /// Transcribe a 16 kHz waveform into text with the Wav2Vec2 CTC model of ``lang``.
        /// 
        /// The English model emits upper-case text; the other checkpoints emit lower-case.
        /// </summary>
        /// <param name="audio_waveform"></param>
        /// <param name="lang"></param>
        /// <returns></returns>
        public static async Task<dynamic> transcribe(NDArray audio_waveform, string lang = languages.DEFAULT_LANGUAGE)
        {
            var processor = await _get_processor(lang);

            var kwargs = new Dictionary<string, dynamic>()
            {
                { "sampling_rate",SAMPLING_RATE },
                { "return_tensors","pt" },
                { "padding", true}
            };

            var inputs = processor.call(audio_waveform, kwargs: kwargs);

            torch.Tensor logits = null;

            using (torch.no_grad())
            {
                logits = (await _get_model_ctc(lang)).forward(inputs["input_values"].to(Utility.get_device())).logits;
            }

            var predicted_ids = torch.argmax(logits, dim: -1).cpu();

            var kwargs2 = new Dictionary<string, dynamic>() { { "output_char_offsets", false }, { "output_word_offsets", false } };

            var result = processor.batch_decode(new List<dynamic>() { predicted_ids }, kwargs2);

            if (ObjectHelper.IsList(result))
            {
                return result[0][0];
            }
            else if (result is Wav2Vec2CTCTokenizerOutput output)
            {
                return output.text;
            }

            return result;
        }

        private static async Task<Wav2Vec2ForCTC> _get_model_ctc(string lang = languages.DEFAULT_LANGUAGE)
        {
            return (await _load_models(languages.get_language(lang).asr_model)).model_ctc;
        }

        /// <summary>
        /// Embeddings & transcription
        /// </summary>
        /// <param name="audio_waveform"></param>
        /// <param name="sampling_rate"></param>
        /// <returns></returns>
        public static async Task<float[,]> extract_embeddings(dynamic audio_waveform, int sampling_rate = SAMPLING_RATE)
        {
            //Extract raw Wav2Vec2 hidden states, shape (frames, features).
            var processor = await _get_processor();

            var kwargs = new Dictionary<string, dynamic>();

            kwargs["sampling_rate"] = 16000;
            kwargs["return_tensors"] = "pt";
            kwargs["padding"] = true;

            var inputs = processor.call(audio_waveform, kwargs: kwargs);

            var input_values = (inputs as BatchFeature)["input_values"] as torch.Tensor;

            if (input_values.shape.Length > 2)
            {
                input_values = input_values.squeeze(0);
            }

            torch.Tensor features = null;

            using (torch.no_grad())
            {
                var model = await _get_model();

                features = model.forward(input_values.to(Utility.get_device())).last_hidden_state; // (batch, time, features)
            }

            var tensor = features.squeeze(0).cpu();
            var result = TorchHelper.Get2DimensonArray<float>(tensor);

            return result;
        }

        private static async Task<Wav2Vec2Processor> _get_processor(string lang = languages.DEFAULT_LANGUAGE)
        {
            var result = await _load_models(languages.get_language(lang).asr_model);

            return result.Item1;
        }
    }
}

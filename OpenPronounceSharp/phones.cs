using HuggingFaceHubSharp;
using NumSharp;
using OpenPronounceSharp.Levenshtein;
using OpenPronounceSharp.Model;
using PhonemizerSharp;
using SharpUtility.Extension;
using SharpUtility.Helper;
using System.Text.RegularExpressions;
using TorchSharp;
using TransformersSharp.Helper;
using TransformersSharp.models.wav2vec2;
using static TorchSharp.torch;

namespace OpenPronounceSharp
{
    public class phones
    {
        #region Fields & Properties
        private const string _TONE_RE = @"\d";
        private const string _WORD_RE = @"\b[\w']+\b";
        private static Wav2Vec2Processor _processor = null;
        private static Wav2Vec2ForCTC _model_ctc = null;

        /// <summary>
        /// Recognizer tokens that are not phones of any supported language: Mandarin tone
        /// numbers, aspiration marks and two-letter spellings of English diphthongs.
        /// </summary>
        private static readonly Dictionary<string, string> _TOKEN_ALIASES = new Dictionary<string, string>()
        {
            {"th", "t" },
            {"kh", "k" },
            {"ph", "p" },
            {"tʰ", "t" },
            {"kʰ", "k" },
            {"pʰ", "p" },
            {"ai", "aɪ" },
            {"ei", "eɪ" },
            {"au", "aʊ" },
            {"ou", "oʊ" }
        };

        private static readonly Dictionary<string, Dictionary<string, string>> _PHONE_MAP = new Dictionary<string, Dictionary<string, string>>()
        {
            { "en", new Dictionary<string, string>()
                {
                    {"ᵻ", "ɪ"},   // espeak's reduced /ɪ/
                    {"ɐ", "ə"},   // espeak's reduced /a/ (article "a")
                    {"ɔ", "ɑ"},   // cot-caught merger (American English)
                    {"ɔɹ", "oɹ"},  // horse-hoarse merger ("for" / "four")
                    {"ɜ", "ɚ"},   // nurse vowel, espeak writes ɜː}, the recognizer ɚ
                    {"ɾ", "t"},   // flapped t
                    {"ɫ", "l"}
                }
            },
            { "fr", new Dictionary<string,string>()
                {
                    // Mid-vowel pairs whose distribution follows the "loi de position": speakers
                    // and the recognizer swap them freely, learners should not be penalised.
                    { "ɛ", "e" },
                    {"œ", "ø" },
                    {"ə", "ø" },
                    {"ɔ", "o" }
                }
            }
        };

        /// <summary>
        /// Groups of phones close enough that substituting one for another is a minor error
        /// (cost NEAR_PHONE_COST): voicing pairs, tense/lax vowels, dental fricatives said as
        /// stops or sibilants, and other typical L2 approximations that raters accept.
        /// </summary>
        private static readonly Dictionary<string, List<List<string>>> _NEAR_PHONES = new Dictionary<string, List<List<string>>>()
        {
            { "fr", new List<List<string>>()
                {
                    new List<string>(){"e", "i"},
                    new List<string>(){"ø", "y"},
                    new List<string>(){"ø", "e"},
                    new List<string>(){"o", "u"},
                    new List<string>(){"ɑ̃", "ɔ̃"},
                    new List<string>(){"ɛ̃", "ɑ̃"},
                    new List<string>(){"ʁ", "r", "ɹ"},
                    new List<string>(){"b", "p"},
                    new List<string>(){"d", "t"},
                    new List<string>(){"ɡ", "k"},
                    new List<string>(){"z", "s"},
                    new List<string>(){"v", "f"},
                    new List<string>(){"ʒ", "ʃ"}
                }
            },
            { "en", new List<List<string>>()
                {
                    new List<string>(){"ɪ", "i"},
                    new List<string>(){"ʊ", "u"},
                    new List<string>(){"ɛ", "æ"},
                    new List<string>(){"ʌ", "ɑ", "ə", "a"},
                    new List<string>(){"æ", "a"},
                    new List<string>(){"ɛ", "eɪ"},
                    new List<string>(){"ð", "z", "d"},
                    new List<string>(){"θ", "s", "t"},
                    new List<string>(){"b", "p"},
                    new List<string>(){"d", "t"},
                    new List<string>(){"ɡ", "k"},
                    new List<string>(){"z", "s"},
                    new List<string>(){"v", "f"},
                    new List<string>(){"ʒ", "ʃ"},
                    new List<string>(){"dʒ", "tʃ"},
                    new List<string>(){"n", "ŋ"},
                    new List<string>(){"h", "x"},
                    new List<string>(){"ɹ", "r"},
                    new List<string>(){"v", "w"}
                }
            }
        };

        public static readonly string PHONE_MODEL_NAME = Environment.GetEnvironmentVariable("OPENPRONOUNCE_PHONEME_MODEL") ?? "facebook/wav2vec2-lv-60-espeak-cv-ft";
        public const int SAMPLING_RATE = 16000;

        /// <summary>
        /// Every wrong phone of a word gets an error confidence (0-1, see :func:`compare_phones`).
        /// A word is reported when these confidences add up to at least this share of its phones...
        /// </summary>
        public const float PHONE_ERROR_THRESHOLD = 0.4f;

        /// <summary>
        /// ...or to at least this many phones, whatever the length of the word.
        /// </summary>
        public const int PHONE_ERROR_MIN_EDITS = 2;

        /// <summary>
        /// Substituting a phone for a close one (voicing, tense/lax vowel...) counts for this
        /// much of a full error: raters tolerate them, and the recognizer confuses them too.
        /// </summary>
        public const float NEAR_PHONE_COST = 0.5f;

        /// <summary>
        /// Extra phones heard after the last phone of a word (epenthetic vowel, onset of the
        /// next word caught by the alignment) and a dropped final consonant count for this much.
        /// </summary>
        public const float FINAL_EXTRA_COST = 0.25f;
        public const float FINAL_DELETION_COST = 0.5f;

        /// <summary>
        /// An expected phone whose posterior reaches this value where the recognizer heard
        /// something else is not a confident error (Goodness-of-Pronunciation style); the
        /// confidence decreases linearly from 1 (posterior 0) to 0 (posterior at this value).
        /// </summary>
        public const float PHONE_PLAUSIBLE_POSTERIOR = 0.05f;

        /// <summary>
        /// Function words with more than one accepted pronunciation (already normalized), per language.
        /// </summary>
        public static readonly Dictionary<string, Dictionary<string, List<List<string>>>> ALTERNATE_PRONUNCIATIONS = new Dictionary<string, Dictionary<string, List<List<string>>>>()
        {
            { "en", new Dictionary<string,List<List<string>>>()
                {
                    {"a", [["ə"], ["eɪ"]] },
                    {"an", [["ən"], ["æn"]]},
                    {"the", [["ð", "ə"], ["ð", "i"], ["ð", "ɪ"]]},
                    {"to", [["t", "ə"], ["t", "u"]]},
                    {"of", [["ʌ", "v"], ["ə", "v"]]},
                    {"and", [["æ", "n", "d"], ["ə", "n", "d"], ["ə", "n"]]},
                    {"for", [["f", "ɔ", "ɹ"], ["f", "ɚ"]]},
                    {"you", [["j", "u"], ["j", "ə"]]},
                    {"are", [["ɑ", "ɹ"], ["ɚ"]]},
                    {"was", [["w", "ʌ", "z"], ["w", "ə", "z"]]},
                    {"that", [["ð", "æ", "t"], ["ð", "ə", "t"]]},
                    {"can", [["k", "æ", "n"], ["k", "ə", "n"]]},
                    {"have", [["h", "æ", "v"], ["h", "ə", "v"]]},
                    {"or", [["ɔ", "ɹ"], ["ɚ"]]}
                }
            },
            { "fr", new Dictionary<string, List<List<string>>>()
                {
                    {"je", [["ʒ"]] },
                    {"le", [["l"]]},
                    {"de", [["d"]]},
                    {"ne", [["n"]]},
                    {"ce", [["s"]]},
                    {"se", [["s"]]},
                    {"me", [["m"]]},
                    {"te", [["t"]]},
                    {"que", [["k"]]}
                }
            }
        }; 
        #endregion

        public static bool is_enabled()
        {
            return !(new string[] { "", "0", "off", "false", "no" }).Contains(PHONE_MODEL_NAME);
        }

        private static async Task<(Wav2Vec2Processor, Wav2Vec2ForCTC)> _load_model()
        {
            Dictionary<string, dynamic> kwargs = new Dictionary<string, dynamic>();

            if (_processor is null)
            {
                LogHelper.LogInfo($"Loading {PHONE_MODEL_NAME}...");

                kwargs["cls"] = typeof(Wav2Vec2Processor);

                _processor = await Wav2Vec2Processor.from_pretrained(PHONE_MODEL_NAME, kwargs: kwargs) as Wav2Vec2Processor;
            }

            if (_model_ctc is null)
            {
                kwargs["cls"] = typeof(Wav2Vec2ForCTC);
                kwargs["LocalModelDatFilePath"] = Environment.GetEnvironmentVariable("Phones_LocalModelDatFilePath");
                kwargs["ShowStateDictReport"] = constants.is_true(Environment.GetEnvironmentVariable("SHOW_STATE_DICT_REPORT‌"));

                _model_ctc = ((await Wav2Vec2ForCTC.from_pretrained(PHONE_MODEL_NAME, kwargs: kwargs)).Item1 as torch.nn.Module).to(Utility.get_device()) as Wav2Vec2ForCTC;

                _model_ctc.eval();
            }

            return (_processor, _model_ctc);
        }

        /// <summary>
        /// Recognize the phones of a 16 kHz waveform with their confidences and frame posteriors.
        /// </summary>
        /// <param name="audio_waveform"></param>
        /// <param name="sampling_rate"></param>
        /// <param name="normalize"></param>
        /// <param name="lang"></param>
        /// <returns></returns>
        public static async Task<PhoneRecognition> recognize_phones(NDArray audio_waveform, int sampling_rate = SAMPLING_RATE, bool normalize = true, string lang = languages.DEFAULT_LANGUAGE)
        {
            var (processor, _) = await _load_model();

            var log_posteriors = await phone_log_posteriors(audio_waveform, sampling_rate);

            return decode_ctc(log_posteriors, await phone_vocab(), processor.tokenizer.pad_token_id, lang, normalize);
        }

        /// <summary>
        /// Compare recognized phones with the phones expected for ``text_reference`` in ``lang``, word by word.
        /// </summary>
        /// <param name="heard_phones"></param>
        /// <param name="text_reference"></param>
        /// <param name="lang"></param>
        /// <returns></returns>
        public static Dictionary<string, dynamic> compare_phones(PhoneRecognition heard_phones, string text_reference, string lang = languages.DEFAULT_LANGUAGE)
        {
            var (_, heard, heard_confidences) = _as_recognition(heard_phones);

            var (words, groups) = get_expected_phones(text_reference, lang);
            var expected = groups.SelectMany(g => g).ToList();

            float phone_error_rate = 0.0f;

            if (expected.Count > 0)
            {
                phone_error_rate = (float)(LevenshteinDistance.distance(expected, heard) * 1.0 / expected.Count);
            }
            else
            {
                phone_error_rate = heard.Count > 0 ? 1.0f : 0.0f;
            }

            var errors = new List<Dictionary<string, dynamic>>();
            var words_with_errors = new List<string>();

            foreach (var report in _word_reports(heard_phones, text_reference, lang))
            {
                float edits = report["weighted_edits"];
                int length = report["expected"].Count;

                if (report["distance"] !=0 && (edits / length >= PHONE_ERROR_THRESHOLD || edits >= PHONE_ERROR_MIN_EDITS))
                {
                    errors.Add(new Dictionary<string, dynamic>()
                    {
                        {"position", report["position"] },
                        {"word", report["word"] },
                        {"expected", string.Join("", report["expected"]) },
                        {"actual", string.Join("", report["actual"]) },
                        {"actual_word", "" },
                        {"phone_distance", report["distance"] },
                        {"confidence", Math.Round(Math.Min(1.0f, Math.Max(edits*1.0f / length, edits / PHONE_ERROR_MIN_EDITS)), 3) },
                        {"phones",(report["phones"] as List<Dictionary<string,dynamic>>).Select(p=>p.ToDictionary(item=>item.Key, item=> Math.Round(p["confidence"])))}
                    });

                    words_with_errors.Add(report["word"]);
                }
            }

            return new Dictionary<string, dynamic>()
            {
                {"expected_phones",groups },
                {"heard_phones", heard },
                {"heard_phones_confidence",heard_confidences.Select(c=>(float)Math.Round(c, 3)).ToList() },
                {"phone_error_rate", (float)Math.Round(phone_error_rate, 4) },
                {"errors", errors },
                {"words_with_errors", words_with_errors}
            };
        }

        /// <summary>
        /// Compare what was heard with ``text_reference`` word by word.
        /// </summary>
        /// <param name="heard_phones"></param>
        /// <param name="text_reference"></param>
        /// <param name="lang"></param>
        /// <returns></returns>
        private static List<Dictionary<string, dynamic>> _word_reports(PhoneRecognition heard_phones, string text_reference, string lang = languages.DEFAULT_LANGUAGE)
        {
            var (recognition, heard, _) = _as_recognition(heard_phones);
            var n_frames = recognition is not null ? LengthHelper.GetNDArrayLength(recognition.log_posteriors) : 0;
            var (words, groups) = get_expected_phones(text_reference, lang);
            var expected = groups.SelectMany(g => g).ToList();
            var alignment = _align(expected, heard);

            var reports = new List<Dictionary<string, dynamic>>();
            var offset = 0;
            string previous_last_phone = null;

            int position = 0;

            foreach (var (word, group) in words.Zip(groups))
            {
                if (!group.HasValue())
                {
                    continue;
                }

                var indices = RangeHelper.GetRange(offset, offset + group.Count);
                offset += group.Count;
                var matched = indices.SelectMany(i => alignment[i].Select(t => t)).OrderBy(item => item).ToHashSet().ToList();
                var actual = matched.Select(j => heard[j]).ToList();

                var candidates = _pronunciations(word, group, previous_last_phone, lang);
                var distances = candidates.Select(c => LevenshteinDistance.distance(c, actual)).ToList();
                var distance = distances.Min();
                var candidate = candidates[distances.IndexOf(distance)];
                previous_last_phone = group.LastOrDefault();

                var region = (0, 0);

                if (recognition is null)
                {
                    region = (0, 0);
                }
                else if (matched.HasValue())
                {
                    region = (recognition.spans[matched[0]].Item1, recognition.spans[matched.LastOrDefault()].Item2);
                }
                else
                {
                    var before = RangeHelper.GetRange(indices[0]).SelectMany(i => alignment[i]).ToList();
                    var after = RangeHelper.GetRange(indices.LastOrDefault() + 1, expected.Count).SelectMany(i => alignment[i]).ToList();
                    region = (before.HasValue() ? recognition.spans[before.Max()].Item1 : 0, after.HasValue() ? recognition.spans[after.Min()].Item2 : n_frames);
                }

                var phone_reports = distance != 0 ? _phone_reports(candidate, actual, matched, recognition, region, lang) : [];

                reports.Add(new Dictionary<string, dynamic>()
                {
                    {"position", position },
                    {"word", word },
                    {"expected", group },
                    {"actual", actual },
                    {"distance", distance },
                    {"phones", phone_reports },
                    {"weighted_edits", phone_reports.Sum(p=>(float)p["confidence"]) }
                });

                position++;
            }

            return reports;
        }

        /// <summary>
        /// Per-phone report of one word: ``{expected, heard, confidence}`` for every phone of ``candidate``.
        /// </summary>
        /// <param name="candidate"></param>
        /// <param name="actual"></param>
        /// <param name="matched"></param>
        /// <param name="recognition"></param>
        /// <param name="region"></param>
        /// <param name="lang"></param>
        /// <returns></returns>
        private static List<Dictionary<string, dynamic>> _phone_reports(List<string> candidate, List<string> actual, List<int> matched, PhoneRecognition recognition, (int, int) region, string lang)
        {
            var local = _align(candidate, actual);
            var spans = recognition is not null ? recognition.spans : null;

            Func<int, (int, int)> frames = (j) =>
            {
                return spans is not null ? spans[matched[j]] : (0, 0);
            };

            var reports = new List<Dictionary<string, dynamic>>();
            var last = candidate.Count - 1;

            int k = 0;
            foreach (var expected in candidate)
            {
                var heard_indices = local[k].OrderBy(item => item).ToList();
                var heard = new List<string>();

                heard = heard_indices.Select(j => actual[j]).ToList();

                float confidence = 0.0f;

                if (Enumerable.SequenceEqual(heard, [expected]))
                {
                    confidence = 0.0f;
                }
                else if (heard.Contains(expected))
                {
                    confidence = k == last ? FINAL_EXTRA_COST : 1.0f;
                }
                else
                {
                    float cost = 0.0f;
                    int start = 0;
                    int end = 0;

                    if (heard.HasValue())
                    {
                        cost = heard.Select(h => _substitution_cost(expected, h, lang)).Max();
                        start = heard_indices.Select(j => frames(j).Item1).Min();
                        end = heard_indices.Select(j => frames(j).Item2).Max();
                    }
                    else
                    {
                        cost = k == last ? FINAL_DELETION_COST : 1.0f;
                        var previous = RangeHelper.GetRange(k).SelectMany(kk => local[kk]).ToList();
                        var following = RangeHelper.GetRange(k + 1, candidate.Count).SelectMany(kk => local[kk]).ToList();
                        start = previous.HasValue() ? frames(previous.Max()).Item1 : region.Item1;
                        end = following.HasValue() ? frames(following.Min()).Item2 : region.Item2;
                    }

                    var plausibility = _plausibility(recognition, expected, start, end, lang);
                    confidence = cost * (1.0f - (float)Math.Min(1.0, plausibility / PHONE_PLAUSIBLE_POSTERIOR));
                }

                reports.Add(new Dictionary<string, dynamic>() { { "expected", expected }, { "heard", string.Join("", heard) }, { "confidence", confidence } });

                k++;
            }

            return reports;
        }

        /// <summary>
        /// Highest posterior of ``phone`` (any of its tokens) over frames ``start:end``, 0 without posteriors.
        /// </summary>
        /// <param name="recognition"></param>
        /// <param name="phone"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="lang"></param>
        /// <returns></returns>
        private static float _plausibility(PhoneRecognition recognition, string phone, int start, int end, string lang)
        {
            if (recognition is null || end <= start)
            {
                return 0.0f;
            }

            var ids = _token_ids_by_phone(recognition.vocab, lang).GetValueOrDefault(phone) ?? new HashSet<int>();

            if (!ids.HasValue())
            {
                return 0.0f;
            }

            return np.exp(recognition.log_posteriors[$"{start}:{end}"][$":,{ids.FirstOrDefault()}"].max()).item<float>();
        }

        /// <summary>
        /// Map every normalized phone to the ids of the vocabulary tokens that normalize to it.
        /// </summary>
        /// <param name="vocab"></param>
        /// <param name="lang"></param>
        /// <returns></returns>
        private static Dictionary<string, HashSet<int>> _token_ids_by_phone(List<string> vocab, string lang)
        {
            var ids = new Dictionary<string, HashSet<int>>();

            int index = 0;
            foreach (var token in vocab)
            {
                if (!_is_special(token))
                {
                    ids.setdefault<HashSet<int>>(normalize_phone(token, lang), []).Add(index);
                }

                index++;
            }

            return ids;
        }

        /// <summary>
        /// Cost of hearing ``heard`` instead of ``expected``: 1, or NEAR_PHONE_COST for close phones.
        /// </summary>
        /// <param name="expected"></param>
        /// <param name="heard"></param>
        /// <param name="lang"></param>
        /// <returns></returns>
        private static float _substitution_cost(string expected, string heard, string lang)
        {
            foreach (var group in _NEAR_PHONES.GetValueOrDefault(lang) ?? new List<List<string>>())
            {
                if (group.Contains(expected) && group.Contains(heard))
                {
                    return NEAR_PHONE_COST;
                }
            }

            return 1.0f;
        }

        /// <summary>
        /// Accepted phone sequences for ``word``: the expected one, its alternates, and, when the
        /// word starts with the phone the previous word ended with ("heat to"), the merged form.
        /// </summary>
        /// <param name="word"></param>
        /// <param name="expected_seg"></param>
        /// <param name="previous_last_phone"></param>
        /// <param name="lang"></param>
        /// <returns></returns>
        private static List<List<string>> _pronunciations(string word, List<string> expected_seg, string previous_last_phone = null, string lang = languages.DEFAULT_LANGUAGE)
        {
            var alternates = (ALTERNATE_PRONUNCIATIONS.GetValueOrDefault(lang) ?? new Dictionary<string, List<List<string>>>()).GetValueOrDefault(word) ?? new List<List<string>>();
            var candidates = EnumerableHelper.Concat([expected_seg], alternates.Select(alt => normalize_phones(alt, lang))).ToList();

            if (previous_last_phone is not null && expected_seg.Count > 1 && expected_seg[0] == previous_last_phone)
            {
                candidates.Add(expected_seg.Skip(1).ToList());
            }

            return candidates;
        }

        /// <summary>
        /// Map every expected phone index to the heard phone indices it aligns with (insertions go to the previous phone).
        /// </summary>
        /// <returns></returns>
        private static List<HashSet<int>> _align(List<string> expected, List<string> heard)
        {
            var alignment = expected.Select(item => new HashSet<int>()).ToList();

            foreach (var item in LevenshteinOpcodes.opcodes(expected, heard)._opcodes)
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
                        alignment[k].Add(l);
                    }
                }
                else if (tag == "replace")
                {
                    var len_i = i2 - i1;
                    var len_j = j2 - j1;

                    foreach (var k in RangeHelper.GetRange(i1, i2))
                    {
                        var start = j1 + (int)((k - i1) * len_j * 1.0 / len_i);
                        var end = j1 + (int)((k - i1 + 1) * len_j * 1.0 / len_i);

                        if (start == end)
                        {
                            alignment[k].Add(Math.Min(start, j2 - 1));
                        }
                        else
                        {
                            alignment[k].update(RangeHelper.GetRange(start, end).ToHashSet());
                        }
                    }
                }
                else if (tag == "insert")
                {
                    var k = i1 > 0 ? i1 - 1 : i1;

                    if (k < alignment.Count)
                    {
                        alignment[k].update(RangeHelper.GetRange(j1, j2).ToHashSet());
                    }
                }
            }

            return alignment;
        }

        /// <summary>
        /// Return ``(words, phones_per_word)`` for ``text``. Words with no phones are kept as empty tuples.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="lang"></param>
        /// <returns></returns>
        public static (List<string>, List<List<string>>) get_expected_phones(string text, string lang = languages.DEFAULT_LANGUAGE)
        {
            var (words, groups) = _expected_phones_by_word(text, lang);

            return (words, groups);
        }

        /// <summary>
        /// Return ``(words, phones_per_word)`` for ``text``, phones normalized.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="lang"></param>
        /// <returns></returns>
        private static (List<string>, List<List<string>>) _expected_phones_by_word(string text, string lang = languages.DEFAULT_LANGUAGE)
        {
            var words = Regex.Matches(text, _WORD_RE).Select(w => w.Value.ToLower()).ToList();

            if (words.Count == 0)
            {
                return ([], []);
            }

            var language = languages.get_language(lang).espeak;

            var groups = new List<string[]>();

            try
            {
                var @out = Phonemizer.phonemize
                (
                    string.Join(" ", words),
                    language: language,
                    backend: "espeak",
                    strip: true,
                    preserve_punctuation: false,
                    separator: new Separator(phone: " ", word: " | ", syllable: "")
                );

                groups = (@out as string).Split('|').Select(g => g.Split(' ')).ToList();

                if (groups.Count != words.Count)
                {
                    throw new ValueError($"expected {words.Count} words, phonemizer returned {groups.Count} groups");
                }
            }
            catch (Exception ex)
            {
                foreach (var word in words)
                {
                    try
                    {
                        var @out = Phonemizer.phonemize(word, language: language, backend: "espeak", strip: true, preserve_punctuation: false, separator: new Separator(phone: " ", word: "", syllable: ""));

                        groups.Add(@out);
                    }
                    catch (Exception e)
                    {
                        groups.Add([]);
                    }
                }
            }

            return (words, groups.Select(g => normalize_phones(g.ToList(), lang)).ToList());
        }

        /// <summary>
        /// Normalize a phone sequence and collapse immediate repetitions ("ɚ ɹ" -> "ɚ", "d d" -> "d").
        /// </summary>
        /// <param name="phones"></param>
        /// <param name="lang"></param>
        /// <returns></returns>
        public static List<string> normalize_phones(List<string> phones, string lang = languages.DEFAULT_LANGUAGE)
        {
            return _normalize_indexed(phones, lang).Select(item => item.Item1).ToList();
        }

        /// <summary>
        /// Return ``(recognition or None, heard phones, heard confidences)`` for a list of phones or a recognition.
        /// </summary>
        /// <param name="heard_phones"></param>
        /// <returns></returns>
        private static (PhoneRecognition, List<string>, List<float>) _as_recognition(PhoneRecognition heard_phones)
        {
            return (heard_phones, heard_phones.phones, heard_phones.confidences);
        }

        /// <summary>
        /// Vocabulary of the phone model as a tuple of tokens indexed by id.
        /// </summary>
        public static async Task<List<string>> phone_vocab()
        {
            var (processor, _) = await _load_model();
            var vocab = processor.tokenizer.getVocab();

            return vocab.OrderBy(item => item.Value).Select(item => item.Key).ToList();
        }

        public static PhoneRecognition decode_ctc(NDArray log_posteriors, List<string> vocab, int? blank_id = 0, string lang = languages.DEFAULT_LANGUAGE, bool normalize = true)
        {
            log_posteriors = np.asarray(log_posteriors, dtype: np.float32);
            var ids = log_posteriors.argmax(axis: 1);
            var tokens = new List<string>();
            var confidences = new List<float>();
            var spans = new List<(int, int)>();
            var start = 0;
            var ids_len = LengthHelper.GetNDArrayLength(ids);

            foreach (var end in RangeHelper.GetRange(1, ids_len + 1))
            {
                if (end < ids_len && (ids[end] == ids[start]).item<bool>())
                {
                    continue;
                }

                var ids_start = ids[start].item<int>();

                var token = vocab[ids_start];

                if (ids_start != blank_id && !_is_special(token))
                {
                    tokens.Add(token);
                    confidences.Add((np.exp(log_posteriors[$"{start}:{end}", ids[start]].max())).item<float>());
                    spans.Add((start, end));
                }

                start = end;
            }

            if (!normalize)
            {
                return new PhoneRecognition() { phones = tokens, confidences = confidences, spans = spans, log_posteriors = log_posteriors, vocab = vocab };
            }

            var phones = new List<string>();
            var merged_confidences = new List<float>();
            var merged_spans = new List<(int, int)>();

            foreach (var (phone, sources) in _normalize_indexed(tokens, lang))
            {
                phones.Add(phone);
                merged_confidences.Add(sources.Select(i => confidences[i]).Max());
                merged_spans.Add((spans[sources[0]].Item1, spans[sources.LastOrDefault()].Item2));
            }

            return new PhoneRecognition() { phones = phones, confidences = merged_confidences, spans = merged_spans, log_posteriors = log_posteriors, vocab = vocab };
        }

        /// <summary>
        /// Normalize ``phones`` and return ``(phone, source_indices)`` pairs, one per surviving phone.
        /// </summary>
        /// <param name="phones"></param>
        /// <param name="lang"></param>
        /// <returns></returns>
        private static List<(string, List<int>)> _normalize_indexed(List<string> phones, string lang)
        {
            var @out = new List<(string, List<int>)>();

            for (var index = 0; index < phones.Count; index++)
            {
                var phone = phones[index];
                phone = normalize_phone(phone, lang);

                if (string.IsNullOrEmpty(phone))
                {
                    continue;
                }

                if (@out.Count > 0 && (@out.LastOrDefault().Item1 == phone || (@out.LastOrDefault().Item1 == "ɚ" && phone == "ɹ")))
                {
                    @out.LastOrDefault().Item2.Add(index);
                    continue;
                }

                @out.Add((phone, [index]));
            }

            return @out;
        }

        /// <summary>
        /// Map a phone to its canonical form (drop length marks, merge near-identical phones of ``lang``).
        /// </summary>
        /// <param name="phone"></param>
        /// <param name="lang"></param>
        /// <returns></returns>
        public static string normalize_phone(string phone, string lang = languages.DEFAULT_LANGUAGE)
        {
            phone = Regex.Replace(phone.Replace(":", ""), _TONE_RE, "");
            phone = _TOKEN_ALIASES.GetValueOrDefault(phone, phone);

            return (_PHONE_MAP.GetValueOrDefault(lang) ?? new Dictionary<string, string>())?.GetValueOrDefault(phone, phone);
        }        

        /// <summary>
        /// Frame-level log posteriors of the phone model for a waveform, as a ``(frames, vocab)`` numpy array.
        /// </summary>
        /// <param name="audio_waveform"></param>
        /// <param name="sampling_rate"></param>
        /// <returns></returns>
        public static async Task<NDArray> phone_log_posteriors(NDArray audio_waveform, int sampling_rate = SAMPLING_RATE)
        {
            var (processor, model) = await _load_model();

            var kwargs = new Dictionary<string, dynamic>();

            kwargs["sampling_rate"] = 16000;
            kwargs["return_tensors"] = "pt";
            kwargs["padding"] = true;

            var inputs = processor.call(audio_waveform, kwargs: kwargs) as Dictionary<string, dynamic>;

            torch.Tensor logits = null;

            using (torch.no_grad())
            {
                logits = model.forward(inputs["input_values"].to(Utility.get_device())).logits[0];
            }

            torch.Tensor tensor = torch.log(torch.softmax(logits.@float(), dim: -1)).cpu();

            return TorchHelper.Get2DimensonArray<float>(tensor);
        }

        private static bool _is_special(string token)
        {
            return token.StartsWith("<") && token.EndsWith(">");
        }
    }
}

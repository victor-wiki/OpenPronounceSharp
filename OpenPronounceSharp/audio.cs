using FFMpegCore;
using FFMpegCore.Pipes;
using NAudio.SoundFile;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NumSharp;
using OpenPronounceSharp.Model;
using SharpUtility.Helper;
using System.Text;
using static TorchSharp.torch.utils;

namespace OpenPronounceSharp
{
    public class audio
    {
        private const string sample_format = "f32le";
        private const string sample_pcm_format = "pcm_f32le";
        public const int TARGET_SR = 16000;       
        public static AudioParserType AudioParserType { get; set; }

        public static async Task<(NDArray, int?)> _decode(string file_path = null, Stream stream = null, int? sr = null, bool mono = true, bool needParseSR = false, bool loadWaveform = true)
        {
            int? sampleRate = null;

            if (string.IsNullOrEmpty(file_path) && stream is null)
            {
                throw new ArgumentException("file_path or stream is required!");
            }

            byte[] bytes = null;

            if (AudioParserType == AudioParserType.None || AudioParserType == AudioParserType.FFMpeg)
            {
                if (needParseSR)
                {
                    try
                    {
                        var probe = !string.IsNullOrEmpty(file_path) ? FFProbe.Analyse(file_path) : FFProbe.Analyse(stream);
                        var audioStream = probe.AudioStreams.FirstOrDefault();

                        sampleRate = audioStream.SampleRateHz;
                    }
                    catch (Exception ex)
                    {
                    }
                }

                if(loadWaveform)
                {
                    using (var ms = new MemoryStream())
                    {
                        string inputArgs = "-v error";

                        string monoArg = mono ? "-ac 1" : "";
                        string sampleRateArg = sr.HasValue ? $"-ar {sr}" : "";
                        string outArgs = $"-f {sample_format} {monoArg} -acodec {sample_pcm_format} {sampleRateArg}";

                        var args = (!string.IsNullOrEmpty(file_path) ?
                              FFMpegArguments.FromFileInput(file_path, true, options => options.WithCustomArgument(inputArgs))
                              : FFMpegArguments.FromPipeInput(new StreamPipeSource(stream), options => options.WithCustomArgument(inputArgs))
                            )
                            .OutputToPipe(new StreamPipeSink(ms), options => options.WithCustomArgument(outArgs));

                        await args.ProcessAsynchronously();

                        bytes = ms.ToArray();
                    }
                }                
            }
            else if (AudioParserType == AudioParserType.NAudio)
            {
                using (var reader = !string.IsNullOrEmpty(file_path) ? new SoundFileReader(file_path) : new SoundFileReader(stream))
                {
                    if (needParseSR)
                    {
                        sampleRate = reader.WaveFormat.SampleRate;
                    }

                    if(loadWaveform)
                    {
                        sr ??= (sampleRate ?? TARGET_SR);

                        IWaveProvider wave16 = GetSampleWavProvider(reader, sr.Value, mono);

                        using (var ms = new MemoryStream())
                        {
                            byte[] buffer = new byte[8192];

                            int read;

                            while ((read = wave16.Read(buffer.AsSpan())) > 0)
                            {
                                ms.Write(buffer, 0, read);
                            }

                            bytes = ms.ToArray();
                        }
                    }                    
                }
            }

            return (loadWaveform? np.frombuffer(bytes, NPTypeCode.Float).copy(): null, sampleRate);
        }

        private static SampleToWaveProvider GetSampleWavProvider(SoundFileReader reader, int sr, bool mono)
        {
            var resampler = new WdlResamplingSampleProvider(reader, sr);

            ISampleProvider provider = null;

            if (mono)
            {
                if (resampler.WaveFormat.Channels == 1)
                {
                    provider = resampler;
                }
                else
                {
                    provider = new StereoToMonoSampleProvider(resampler) { LeftVolume = 0.5f, RightVolume = 0.5f };
                }
            }
            else
            {
                provider = resampler;
            }

            SampleToWaveProvider wave16 = new SampleToWaveProvider(provider);

            return wave16;
        }

        /// <summary>
        /// Load any audio file (wav, mp3, flac, ogg, webm, m4a...) as a mono float32 waveform at ``sr`` Hz.
        /// 
        /// libsndfile (through librosa) handles wav/flac/ogg/mp3; anything it cannot open
        /// (browser webm/opus recordings, m4a...) is decoded with ffmpeg.
        /// </summary>
        /// <param name="file_path"></param>
        /// <param name="sr"></param>
        public static async Task<(NDArray, int?)> load(string file_path, int? sr, bool mono = true, bool needParseSR = false, bool loadWaveform = true)
        {
            return await _decode(file_path: file_path, sr: sr, mono: mono, needParseSR:needParseSR, loadWaveform:loadWaveform);
        }

        public static async Task<(NDArray, int?)> load(Stream stream, int? sr, bool mono = true, bool needParseSR = false, bool loadWaveform = true)
        {
            return await _decode(stream: stream, sr: sr, mono: mono, needParseSR: needParseSR, loadWaveform: loadWaveform);
        }

        /// <summary>
        /// The synthesizer is chosen with ``backend`` (``gtts``, ``piper`` or ``kokoro``), falling
        /// back to the ``OPENPRONOUNCE_TTS`` environment variable, then to gTTS; the voice with
        ///  ``voice`` / ``OPENPRONOUNCE_TTS_VOICE``, then to a per-language default. See
        ///  :mod:`openpronounce.tts`. Results are cached in ``CACHE_DIR`` keyed by
        ///  ``(backend, voice, lang, text)`` so that repeated comparisons against the same
        ///  sentence are free.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="lang"></param>
        /// <param name="filename"></param>
        /// <param name="target_sr"></param>
        /// <param name="backend"></param>
        /// <param name="voice"></param>
        /// <returns></returns>
        public static async Task<string> text2speech(string text, string lang = "en", string filename = null, int target_sr = TARGET_SR, string backend = null, string voice = null)
        {
            (backend, voice) = tts.resolve(lang, backend: backend, voice: voice);

            if (filename == null)
            {
                string key = StringHelper.Sha1Hex($"{backend}\x00{voice}\x00{lang}\x00{target_sr}\x00{text}");

                filename = Path.Combine(Helper.LocationHelper.CACHE_DIR, $"tts-{key}.wav");

                if (File.Exists(filename))
                {
                    return filename;
                }
            }

            LogHelper.LogInfo(string.Format("Synthesizing reference with {0} (voice {1}, lang {2})", backend, voice, lang));

            string tempFilePath = await tts.synthesize(text, lang, backend, voice);           

            if (File.Exists(tempFilePath))
            {
                if (AudioParserType == AudioParserType.None || AudioParserType == AudioParserType.FFMpeg)
                {
                    await convert_audio_with_ffmpeg(tempFilePath, filename, target_sr);
                }
                else
                {
                    using (var reader = new SoundFileReader(tempFilePath))
                    {
                        SampleToWaveProvider provider16 = GetSampleWavProvider(reader, target_sr, true);

                        SoundFileWriter.CreateSoundFile16(filename, provider16.ToSampleProvider());
                    }
                }               

                try
                {
                    File.Delete(tempFilePath);
                }
                catch (Exception ex) { }
            }

            return filename;
        }

        private static async Task convert_audio_with_ffmpeg(string input_file, string output_file, int sr)
        {
            string outArgs = $"-ac 1 -acodec pcm_s16le -ar {sr}";

            await FFMpegArguments.FromFileInput(input_file, true, options => options.WithCustomArgument("-v error"))
                           .OutputToFile(output_file, true, options => options.WithCustomArgument(outArgs))
                           .ProcessAsynchronously();
        }

        /// <summary>
        /// Convert a browser-recorded file (webm/ogg/wav/...) to a 16 kHz mono ``*.16k.wav`` file next to it.
        /// </summary>
        /// <param name="file_path"></param>
        /// <returns></returns>
        public static async Task<string> webm2wav(string file_path)
        {
            string output_path = Path.Combine(Path.GetDirectoryName(file_path), Path.GetFileNameWithoutExtension(file_path) + ".16k.wav");

            await convert_audio_with_ffmpeg(file_path, output_path, TARGET_SR);

            return output_path;
        }
    }
}

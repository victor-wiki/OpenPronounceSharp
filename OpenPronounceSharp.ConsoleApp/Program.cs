using OpenPronounceSharp.Helper;
using SharpUtility.Helper;
using System.Runtime.InteropServices;

namespace OpenPronounceSharp.ConsoleApp
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            LoadLibraries();

            LogHelper.EnableLog = true;
            LogHelper.LogMode = LogMode.Console;

            //// The folder of the model to download, if the value is null, use the default folder: C:\Users\user_name\.cache\huggingface\hub
            //Environment.SetEnvironmentVariable("HF_HUB_CACHE‌", null);

            // If proxy is required, use HF_ENDPOINT and HF_HUB_DISABLE_XET
            Environment.SetEnvironmentVariable("HF_ENDPOINT", "https://hf-mirror.com");
            Environment.SetEnvironmentVariable("HF_HUB_DISABLE_XET", "1");

            // if model has already been downloded, use HF_HUB_OFFLINE and TRANSFORMERS_OFFLINE, it won't check internet files.
            Environment.SetEnvironmentVariable("HF_HUB_OFFLINE", "1");
            Environment.SetEnvironmentVariable("TRANSFORMERS_OFFLINE", "1");

            // for console application, set SHOW_DOWNLOAD_PROGRESS_BAR to 1 to show download progress. 
            Environment.SetEnvironmentVariable("SHOW_DOWNLOAD_PROGRESS_BAR", "1");

            // show report of loaing state dict.
            Environment.SetEnvironmentVariable("SHOW_STATE_DICT_REPORT‌", "1");

            // because TorchSharp use dat file, use https://github.com/victor-wiki/PyModelToSharp to convert a download model to a dat file, and then use dat file directly.
            string speech_LocalModelDatFilePath = @"E:/resources/dat/facebook--wav2vec2-large-960h.dat";
            string phones_LocalModelDatFilePath = @"E:/resources/dat/facebook--wav2vec2-lv-60-espeak-cv-ft.dat";

            if (File.Exists(speech_LocalModelDatFilePath))
            {
                Environment.SetEnvironmentVariable("Speech_LocalModelDatFilePath", speech_LocalModelDatFilePath);
            }

            if (File.Exists(phones_LocalModelDatFilePath))
            {
                Environment.SetEnvironmentVariable("Phones_LocalModelDatFilePath", phones_LocalModelDatFilePath);
            }

            //audio.AudioParserType = Model.AudioParserType.NAudio;

            string filePath = Path.Combine(LocationHelper.Location, @"test/jfk.flac");

            var audio_waveform = (await audio.load(filePath, audio.TARGET_SR)).Item1;

            Dictionary<string, dynamic> kwargs = new Dictionary<string, dynamic>();

            var result = await speech.compare_audio_with_text(audio_waveform, "And so my fellow Americans ask not what your country can do for you ask what you can do for your country");

            Console.WriteLine($"transcribe:{(result["transcribe"] as string)?.ToLower()}");
            Console.WriteLine($"score:{result["score"]}");

            //Console.ReadLine();
        }

        static void LoadLibraries()
        {
            try
            {
                string folder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "runtimes", "win-x64", "native");

                if (Directory.Exists(folder))
                {
                    var files = new DirectoryInfo(folder).GetFiles("*.dll");

                    foreach (var file in files)
                    {
                        NativeLibrary.Load(file.FullName);
                    }
                }
            }
            catch (Exception ex)
            {

            }
        }
    }
}

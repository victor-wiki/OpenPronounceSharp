using HuggingFaceHubSharp;
using HuggingFaceHubSharp.utils;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using OpenPronounceSharp.Helper;
using OpenPronounceSharp.Manager;
using OpenPronounceSharp.Model;
using OpenPronounceSharp.Web.Models;
using SharpUtility.Helper;
using System.Diagnostics;
using langs = OpenPronounceSharp.languages;

namespace OpenPronounceSharp.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IConfiguration _config;

        public HomeController(IConfiguration config)
        {
            _config = config;
        }

        public IActionResult Index()
        {
            return View();
        }       

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        /// <summary>
        /// List the supported languages (``code`` is the value of the ``lang`` form field).
        /// </summary>
        /// <returns></returns>
        public async Task<IActionResult> languages()
        {
            var languages = OpenPronounceSharp.languages.LANGUAGES.Values.Select(item => new { item.code, item.name }).ToList();

            return new JsonResult(new Dictionary<string, dynamic>() { { "default", OpenPronounceSharp.languages.DEFAULT_LANGUAGE }, { "languages", languages } });
        }

        /// <summary>
        /// Score ``file`` against ``expected_text`` in ``lang``. Returns the full analysis (score, errors, prosody).
        /// </summary>
        /// <param name="file"></param>
        /// <param name="expected_text"></param>
        /// <param name="lang"></param>
        /// <returns></returns>
        public async Task<IActionResult> pronunciation(IFormFile‌ file, string expected_text, string lang)
        {
            lang = this._validate_lang(lang);

            var wav_file = await this._save_upload_as_wav(file);

            if (wav_file != null)
            {
                string configFielPath =  _config["MySettings:ConfigFilePath"];

                if(System.IO.File.Exists(configFielPath))
                {
                    SettingManager.LoadConfig(configFielPath);

                    Setting setting = SettingManager.Setting;

                    if (setting.HF_HUB_CACHE != null)
                    {
                        Environment.SetEnvironmentVariable("HF_HUB_CACHE‌", setting.HF_HUB_CACHE);
                    }

                    if (setting.HF_ENDPOINT != null)
                    {
                        Environment.SetEnvironmentVariable("HF_ENDPOINT", setting.HF_ENDPOINT);
                        Environment.SetEnvironmentVariable("HF_HUB_DISABLE_XET", "1");
                    }

                    if (setting.IsOffline)
                    {
                        Environment.SetEnvironmentVariable("HF_HUB_OFFLINE", "1");
                        Environment.SetEnvironmentVariable("TRANSFORMERS_OFFLINE", "1");
                    }

                    if(setting.Speech_LocalModelDatFilePath != null)
                    {
                        Environment.SetEnvironmentVariable("Speech_LocalModelDatFilePath", setting.Speech_LocalModelDatFilePath);
                    }

                    if (setting.Phones_LocalModelDatFilePath != null)
                    {
                        Environment.SetEnvironmentVariable("Phones_LocalModelDatFilePath", setting.Phones_LocalModelDatFilePath);
                    }

                    LogHelper.EnableLog = setting.EnableLog;                  

                    audio.AudioParserType = AudioParserType.FFMpeg; //use ffmpeg, because the webm->wav has used it already.
                }               

                Environment.SetEnvironmentVariable("SHOW_DOWNLOAD_PROGRESS_BAR", "0");               

                var sound = await audio.load(wav_file, sr: null);
                var result = await speech.compare_audio_with_text(sound.Item1, expected_text, lang: lang);

                try
                {
                    System.IO.File.Delete(wav_file);
                }
                catch (Exception ex)
                {
                }

                return new JsonResult(result);
            }

            return new JsonResult(new { });
        }

        /// <summary>
        /// Return a 16 kHz wav reference pronunciation of ``text`` in ``lang``.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="lang"></param>
        /// <returns></returns>
        public async Task<IActionResult> tts(string text, string lang = langs.DEFAULT_LANGUAGE)
        {
            lang = _validate_lang(lang);

            return new PhysicalFileResult(await audio.text2speech(text, lang : lang), "audio/wav");
        }
       
        private async Task<string> _save_upload_as_wav(IFormFile upload)
        {
            string suffix = Path.GetExtension(upload.FileName) ?? ".webm";
            string tempFileName = Path.GetTempFileName();
            var path = Path.Combine(LocationHelper.TEMP_DIR, $"openpronounce-upload-{Path.GetFileNameWithoutExtension(tempFileName)}{suffix}");

            using (var fs = new FileStream(path, FileMode.OpenOrCreate))
            {
                upload.CopyTo(fs);

                fs.Flush();
                fs.Close();
            }

            try
            {
                return await audio.webm2wav(path);
            }
            catch (Exception ex)
            {
                return null;
            }
            finally
            {
                try
                {
                    System.IO.File.Delete(path);
                }
                catch (Exception ex)
                {
                }
            }
        }

        private string _validate_lang(string lang)
        {
            return OpenPronounceSharp.languages.get_language(lang).code;
        }
    }
}

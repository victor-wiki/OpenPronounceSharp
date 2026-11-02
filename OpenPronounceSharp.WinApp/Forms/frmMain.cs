using Microsoft.Web.WebView2.Core;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Newtonsoft.Json;
using OpenPronounceSharp.Helper;
using OpenPronounceSharp.Manager;
using OpenPronounceSharp.Model;
using OpenPronounceSharp.WinApp.Adapter;
using SharpUtility.Helper;
using System.Web;

namespace OpenPronounceSharp.WinApp
{
    public partial class frmMain : Form
    {
        private const int rate = audio.TARGET_SR;
        private const int channels = 1;
        private IWaveIn captureDevice;
        private WaveFileWriter writer;
        private MMDevice device;
        private string outputFolder => LocationHelper.TEMP_DIR;
        private bool loadingDevices;
        private bool isRecording;
        private string tempAudioFileName;
        private bool devicesChangedWhileRecording;
        private readonly MMDeviceEnumerator deviceEnumerator = new();
        private IWavePlayer wavePlayer;
        private AudioFileReader audioFileReader;

        public frmMain()
        {
            InitializeComponent();

            this.Init();
        }

        private void frmMain_Load(object sender, EventArgs e)
        {

        }

        private void Init()
        {
            this.LoadDevice();

            this.InitWebView();
        }

        private async Task InitWebView()
        {
            await this.webView.EnsureCoreWebView2Async();

            string assetsFolder = Path.Combine(LocationHelper.Location, "Assets");
            string jsFolder = Path.Combine(LocationHelper.Location, "Js");
            string cssFolder = Path.Combine(LocationHelper.Location, "Css");

            this.webView.CoreWebView2.SetVirtualHostNameToFolderMapping("assets.local", assetsFolder, CoreWebView2HostResourceAccessKind.Allow);
            this.webView.CoreWebView2.SetVirtualHostNameToFolderMapping("js.local", jsFolder, CoreWebView2HostResourceAccessKind.Allow);
            this.webView.CoreWebView2.SetVirtualHostNameToFolderMapping("css.local", cssFolder, CoreWebView2HostResourceAccessKind.Allow);

            string html = File.ReadAllText("Template/Result.html");

            this.webView.NavigateToString(html);
        }

        private void LoadDevice()
        {
            this.loadingDevices = true;

            try
            {
                var flow = DataFlow.Capture;
                var devices = this.deviceEnumerator.EnumerateAudioEndPoints(flow, DeviceState.Active).ToArray();

                if (devices.Length > 0)
                {
                    try
                    {
                        this.device = deviceEnumerator.GetDefaultAudioEndpoint(flow, Role.Console);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Audio Recording Device not found!");
                    }
                }
            }
            finally
            {
                this.loadingDevices = false;
            }

            this.OnDeviceChanged();
        }

        private void OnDeviceChanged()
        {
            if (this.loadingDevices)
            {
                return;
            }

            this.Cleanup();
        }

        private void btnStartRecord_Click(object sender, EventArgs e)
        {
            if (!this.ValidateInput())
            {
                return;
            }

            this.Cleanup();

            try
            {
                this.captureDevice = this.CreateCaptureDevice();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Unable to start recording");
                return;
            }

            if (this.device != null)
            {
                try
                {
                    this.device.AudioEndpointVolume.Mute = false;
                }
                catch (Exception ex)
                {
                }
            }

            try
            {
                this.tempAudioFileName = $"{Path.GetRandomFileName()}.wav";

                this.writer = new WaveFileWriter(this.GetTempAudioFilePath(), captureDevice.WaveFormat);

                this.captureDevice.StartRecording();

                this.SetControlStates(true);
            }
            catch (Exception ex)
            {
                this.Cleanup();
                this.SetControlStates(false);

                MessageBox.Show(ex.Message, "Unable to start recording");
            }
        }

        private string GetTempAudioFilePath()
        {
            if (this.tempAudioFileName == null)
            {
                return null;
            }

            return Path.Combine(this.outputFolder, this.tempAudioFileName);
        }

        private void btnStopRecord_Click(object sender, EventArgs e)
        {
            this.StopRecording();
        }

        private void Cleanup()
        {
            if (this.captureDevice != null)
            {
                this.captureDevice.DataAvailable -= this.OnDataAvailable;
                this.captureDevice.RecordingStopped -= this.OnRecordingStopped;
                this.captureDevice.Dispose();
                this.captureDevice = null;
            }

            this.FinalizeWaveFile();
        }

        private void OnDataAvailable(object sender, WaveInEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new EventHandler<WaveInEventArgs>(this.OnDataAvailable), sender, e);

                return;
            }

            if (this.writer == null)
            {
                return;
            }

            this.writer.Write(e.Buffer, 0, e.BytesRecorded);

            int secondsRecorded = (int)(writer.Length / writer.WaveFormat.AverageBytesPerSecond);

            if (secondsRecorded >= 30)
            {
                this.StopRecording();
            }
            else
            {
                this.progressBar1.Value = secondsRecorded;
            }
        }

        private void StopRecording()
        {
            this.captureDevice?.StopRecording();
        }

        private void OnRecordingStopped(object sender, StoppedEventArgs e)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new EventHandler<StoppedEventArgs>(OnRecordingStopped), sender, e);
                return;
            }

            this.FinalizeWaveFile();
            this.progressBar1.Value = 0;

            if (e.Exception != null)
            {
                this.Cleanup();

                MessageBox.Show($"A problem was encountered during recording: {e.Exception.Message}", "Recording Error");
            }

            this.SetControlStates(false);
        }

        private void FinalizeWaveFile()
        {
            this.writer?.Dispose();
            this.writer = null;
        }

        private IWaveIn CreateCaptureDevice()
        {
            var requestedFormat = new WaveFormat(rate, channels);

            var builder = new WasapiRecorderBuilder().WithDevice(this.device).WithFormat(requestedFormat);

            IWaveIn device = new WasapiRecorderAdapter(builder.Build());

            device.DataAvailable += OnDataAvailable;
            device.RecordingStopped += OnRecordingStopped;

            return device;
        }

        private void SetControlStates(bool isRecording)
        {
            this.isRecording = isRecording;

            if (!this.isRecording && this.devicesChangedWhileRecording)
            {
                this.devicesChangedWhileRecording = false;
            }
        }

        private void btnPlayRecording_Click(object sender, EventArgs e)
        {
            string filePath = this.GetTempAudioFilePath();

            if (filePath == null)
            {
                return;
            }

            if (File.Exists(filePath))
            {
                this.PlayFile(filePath);
            }
            else
            {
                MessageBox.Show("File not found!");
            }
        }

        private void PlayFile(string filePath)
        {
            if (this.wavePlayer == null)
            {
                this.wavePlayer = new WaveOut();

                this.wavePlayer.PlaybackStopped += this.WavePlayer_PlaybackStopped;
            }

            this.audioFileReader = new AudioFileReader(filePath);          

            var fadeInOut = new FadeInOutSampleProvider(this.audioFileReader);

            this.wavePlayer.Init(fadeInOut);
            this.wavePlayer.Play();
        }

        private void WavePlayer_PlaybackStopped(object? sender, StoppedEventArgs e)
        {
            try
            {
                if (this.wavePlayer != null)
                {
                    this.wavePlayer.Dispose();
                    this.wavePlayer = null;
                }

                if (this.audioFileReader != null)
                {
                    this.audioFileReader.Dispose();
                    this.audioFileReader = null;
                }
            }
            catch (Exception ex)
            {
            }
        }

        private void tsmiSetting_Click(object sender, EventArgs e)
        {
            frmSetting form = new frmSetting();

            form.ShowDialog();
        }

        private bool ValidateInput()
        {
            string text = this.txtText.Text.Trim();

            if (text.Length == 0)
            {
                MessageBox.Show("Text can't be empty!");

                return false;
            }

            return true;
        }

        private void btnOpenFile_Click(object sender, EventArgs e)
        {
            if (!this.ValidateInput())
            {
                return;
            }

            var result = this.openFileDialog1.ShowDialog();

            if (result == DialogResult.OK)
            {
                this.txtFilePath.Text = this.openFileDialog1.FileName;
            }
        }

        private async Task Analyze(string filePath, bool isFile)
        {
            await this.webView.CoreWebView2.ExecuteScriptAsync("hideResult()");

            await Task.Run(async () => { await this.ExecuteAnalyze(filePath, isFile); });
        }

        private async Task ExecuteAnalyze(string filePath, bool isFile)
        {
            Button button = isFile ? this.btnAnalyzeFile : this.btnAnalyzeRecording;

            button.BeginInvoke(() => { button.Enabled = false; });

            this.progressBar2.BeginInvoke(() =>
            {

                this.progressBar2.Style = ProgressBarStyle.Marquee;
                this.progressBar2.Visible = true;
            });

            try
            {
                string text = this.txtText.Text.Trim();

                if (text.Length == 0)
                {
                    return;
                }

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

                if (setting.Speech_LocalModelDatFilePath != null)
                {
                    Environment.SetEnvironmentVariable("Speech_LocalModelDatFilePath", setting.Speech_LocalModelDatFilePath);
                }

                if (setting.Phones_LocalModelDatFilePath != null)
                {
                    Environment.SetEnvironmentVariable("Phones_LocalModelDatFilePath", setting.Phones_LocalModelDatFilePath);
                }

                if (setting.ShowConsoleWindow)
                {
                    Environment.SetEnvironmentVariable("SHOW_DOWNLOAD_PROGRESS_BAR", "1");
                    Environment.SetEnvironmentVariable("SHOW_STATE_DICT_REPORT‌", "1");
                }

                audio.AudioParserType = setting.AudioParserType;

                LogHelper.EnableLog = setting.EnableLog;

                if (setting.EnableLog)
                {
                    LogHelper.LogMode = LogMode.Console | LogMode.File;
                }
                else
                {
                    LogHelper.EnableLog = true;
                    LogHelper.LogMode = LogMode.Console;
                }

                var sound = await audio.load(filePath, sr: audio.TARGET_SR);
                var result = await speech.compare_audio_with_text(sound.Item1, text);

                string jsonResult = JsonConvert.SerializeObject(result);

                if (setting.ShowConsoleWindow)
                {
                    Console.WriteLine("The result is:");
                    Console.WriteLine(jsonResult);
                }

                await this.ShowResult(jsonResult);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                button.BeginInvoke(() => { button.Enabled = true; });

                this.progressBar2.BeginInvoke(() =>
                {
                    this.progressBar2.Visible = false;
                    this.progressBar2.Style = ProgressBarStyle.Blocks;
                });
            }
        }

        private async Task ShowResult(string result)
        {
            await this.webView.ExecuteScriptAsync($"renderResult('{HttpUtility.UrlEncode(result)}','{HttpUtility.UrlEncode(this.txtText.Text.Trim())}')");
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (Directory.Exists(this.outputFolder))
            {
                var files = new DirectoryInfo(this.outputFolder).GetFiles();

                foreach (var file in files)
                {
                    try
                    {
                        file.Delete();
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
        }

        private void btnAnalyzeFile_Click(object sender, EventArgs e)
        {
            var filePath = this.txtFilePath.Text.Trim();

            if (filePath.Length == 0)
            {
                return;
            }

            if (!File.Exists(filePath))
            {
                MessageBox.Show("File not found!");
                return;
            }

            this.Analyze(filePath, true);
        }

        private void btnAnalyzeRecording_Click(object sender, EventArgs e)
        {
            string filePath = this.GetTempAudioFilePath();

            if (filePath == null)
            {
                return;
            }

            if (!File.Exists(filePath))
            {
                MessageBox.Show("File not found!");
                return;
            }

            this.Analyze(filePath, false);
        }

        private async void btnPlayText_Click(object sender, EventArgs e)
        {
            string text = this.txtText.Text.Trim();

            if (text.Length == 0)
            {
                return;
            }

            string filePath = await audio.text2speech(text);

            this.PlayFile(filePath);
        }

        private void btnPlaySelectedFile_Click(object sender, EventArgs e)
        {
            string filePath = this.txtFilePath.Text.Trim();

            if (File.Exists(filePath))
            {
                this.PlayFile(filePath);
            }
        }
    }
}

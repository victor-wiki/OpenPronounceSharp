using OpenPronounceSharp.Model;
using OpenPronounceSharp.Manager;
using OpenPronounceSharp.Model;

namespace OpenPronounceSharp.WinApp
{
    public partial class frmSetting : Form
    {
        public frmSetting()
        {
            InitializeComponent();
        }

        private void frmSetting_Load(object sender, EventArgs e)
        {
            Setting setting = SettingManager.Setting;

            this.txtCacheFolder.Text = setting.HF_HUB_CACHE;
            this.txtSpeechDatFilePath.Text = setting.Speech_LocalModelDatFilePath;
            this.txtPhonesDatFilePath.Text = setting.Phones_LocalModelDatFilePath;

            this.chkUseProxy.Checked = setting.UseProxy;
            this.txtProxyAddress.Text = setting.HF_ENDPOINT;

            this.chkIsOfflineMode.Checked = setting.IsOffline;

            var audioParserTypes = Enum.GetNames(typeof(AudioParserType));

            this.cboAudioParserType.Items.AddRange(audioParserTypes);

            this.cboAudioParserType.Text = setting.AudioParserType.ToString();
            this.chkEnableLog.Checked = setting.EnableLog;
            this.chkShowConsoleWindow.Checked = setting.ShowConsoleWindow;
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            bool useProxy = this.chkUseProxy.Checked;
            string proxyAddress = this.txtProxyAddress.Text.Trim();
            string cacheFolder = this.txtCacheFolder.Text.Trim();
            string speech_LocalModelDatFilePath = this.txtSpeechDatFilePath.Text.Trim();
            string phones_LocalModelDatFilePath = this.txtPhonesDatFilePath.Text.Trim();

            if (useProxy && proxyAddress.Length == 0)
            {
                MessageBox.Show("Proxy Address can't be empty!");
                return;
            }

            Setting setting = new Setting();

            setting.IsOffline = this.chkIsOfflineMode.Checked;
            setting.UseProxy = this.chkUseProxy.Checked;
            setting.HF_ENDPOINT = useProxy ? proxyAddress : null;
            setting.HF_HUB_CACHE = cacheFolder.Length > 0 ? cacheFolder : null;
            setting.Speech_LocalModelDatFilePath = speech_LocalModelDatFilePath.Length > 0 ? speech_LocalModelDatFilePath : null;
            setting.Phones_LocalModelDatFilePath = phones_LocalModelDatFilePath.Length > 0 ? phones_LocalModelDatFilePath : null;
            setting.AudioParserType =(AudioParserType) Enum.Parse(typeof(AudioParserType), this.cboAudioParserType.Text);
            setting.EnableLog = this.chkEnableLog.Checked;
            setting.ShowConsoleWindow = this.chkShowConsoleWindow.Checked;

            SettingManager.SaveConfig(setting);

            MessageBox.Show("Saved.");

            this.Close();
        }

        private void btnChooseCacheFolder_Click(object sender, EventArgs e)
        {
            var result = this.folderBrowserDialog1.ShowDialog();

            if(result == DialogResult.OK)
            {
                this.txtCacheFolder.Text = this.folderBrowserDialog1.SelectedPath;
            }
        }

        private void btnChooseSpeechDatFile_Click(object sender, EventArgs e)
        {
            this.openFileDialog1.FileName = "";

            var result = this.openFileDialog1.ShowDialog();

            if(result == DialogResult.OK)
            {
                this.txtSpeechDatFilePath.Text = this.openFileDialog1.FileName;
            }
        }

        private void btnChoosePhonesDatFile_Click(object sender, EventArgs e)
        {
            this.openFileDialog1.FileName = "";

            var result = this.openFileDialog1.ShowDialog();

            if (result == DialogResult.OK)
            {
                this.txtPhonesDatFilePath.Text = this.openFileDialog1.FileName;
            }
        }
    }
}

namespace OpenPronounceSharp.WinApp
{
    partial class frmSetting
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            label1 = new Label();
            txtCacheFolder = new TextBox();
            btnChooseCacheFolder = new Button();
            txtProxyAddress = new TextBox();
            chkUseProxy = new CheckBox();
            groupBox1 = new GroupBox();
            label2 = new Label();
            chkIsOfflineMode = new CheckBox();
            label3 = new Label();
            btnChooseSpeechDatFile = new Button();
            txtSpeechDatFilePath = new TextBox();
            btnChoosePhonesDatFile = new Button();
            txtPhonesDatFilePath = new TextBox();
            label4 = new Label();
            btnSave = new Button();
            folderBrowserDialog1 = new FolderBrowserDialog();
            openFileDialog1 = new OpenFileDialog();
            label5 = new Label();
            cboAudioParserType = new ComboBox();
            chkEnableLog = new CheckBox();
            chkShowConsoleWindow = new CheckBox();
            groupBox1.SuspendLayout();
            SuspendLayout();
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 17);
            label1.Name = "label1";
            label1.Size = new Size(129, 17);
            label1.TabIndex = 0;
            label1.Text = "Model Cache Folder:";
            // 
            // txtCacheFolder
            // 
            txtCacheFolder.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtCacheFolder.Location = new Point(146, 14);
            txtCacheFolder.Name = "txtCacheFolder";
            txtCacheFolder.Size = new Size(584, 23);
            txtCacheFolder.TabIndex = 1;
            // 
            // btnChooseCacheFolder
            // 
            btnChooseCacheFolder.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnChooseCacheFolder.Location = new Point(736, 14);
            btnChooseCacheFolder.Name = "btnChooseCacheFolder";
            btnChooseCacheFolder.Size = new Size(75, 23);
            btnChooseCacheFolder.TabIndex = 2;
            btnChooseCacheFolder.Text = "...";
            btnChooseCacheFolder.UseVisualStyleBackColor = true;
            btnChooseCacheFolder.Click += btnChooseCacheFolder_Click;
            // 
            // txtProxyAddress
            // 
            txtProxyAddress.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtProxyAddress.Location = new Point(134, 24);
            txtProxyAddress.Name = "txtProxyAddress";
            txtProxyAddress.Size = new Size(584, 23);
            txtProxyAddress.TabIndex = 4;
            // 
            // chkUseProxy
            // 
            chkUseProxy.AutoSize = true;
            chkUseProxy.Location = new Point(6, 0);
            chkUseProxy.Name = "chkUseProxy";
            chkUseProxy.Size = new Size(85, 21);
            chkUseProxy.TabIndex = 5;
            chkUseProxy.Text = "Use Proxy";
            chkUseProxy.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(label2);
            groupBox1.Controls.Add(chkUseProxy);
            groupBox1.Controls.Add(txtProxyAddress);
            groupBox1.Location = new Point(12, 131);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(799, 68);
            groupBox1.TabIndex = 6;
            groupBox1.TabStop = false;
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(6, 28);
            label2.Name = "label2";
            label2.Size = new Size(95, 17);
            label2.TabIndex = 6;
            label2.Text = "Proxy Address:";
            // 
            // chkIsOfflineMode
            // 
            chkIsOfflineMode.AutoSize = true;
            chkIsOfflineMode.Location = new Point(17, 214);
            chkIsOfflineMode.Name = "chkIsOfflineMode";
            chkIsOfflineMode.Size = new Size(104, 21);
            chkIsOfflineMode.TabIndex = 7;
            chkIsOfflineMode.Text = "Offline Mode";
            chkIsOfflineMode.UseVisualStyleBackColor = true;
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 52);
            label3.Name = "label3";
            label3.Size = new Size(100, 17);
            label3.TabIndex = 8;
            label3.Text = "Speech Dat File:";
            // 
            // btnChooseSpeechDatFile
            // 
            btnChooseSpeechDatFile.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnChooseSpeechDatFile.Location = new Point(736, 49);
            btnChooseSpeechDatFile.Name = "btnChooseSpeechDatFile";
            btnChooseSpeechDatFile.Size = new Size(75, 23);
            btnChooseSpeechDatFile.TabIndex = 10;
            btnChooseSpeechDatFile.Text = "...";
            btnChooseSpeechDatFile.UseVisualStyleBackColor = true;
            btnChooseSpeechDatFile.Click += btnChooseSpeechDatFile_Click;
            // 
            // txtSpeechDatFilePath
            // 
            txtSpeechDatFilePath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtSpeechDatFilePath.Location = new Point(146, 49);
            txtSpeechDatFilePath.Name = "txtSpeechDatFilePath";
            txtSpeechDatFilePath.Size = new Size(584, 23);
            txtSpeechDatFilePath.TabIndex = 9;
            // 
            // btnChoosePhonesDatFile
            // 
            btnChoosePhonesDatFile.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnChoosePhonesDatFile.Location = new Point(736, 88);
            btnChoosePhonesDatFile.Name = "btnChoosePhonesDatFile";
            btnChoosePhonesDatFile.Size = new Size(75, 23);
            btnChoosePhonesDatFile.TabIndex = 13;
            btnChoosePhonesDatFile.Text = "...";
            btnChoosePhonesDatFile.UseVisualStyleBackColor = true;
            btnChoosePhonesDatFile.Click += btnChoosePhonesDatFile_Click;
            // 
            // txtPhonesDatFilePath
            // 
            txtPhonesDatFilePath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtPhonesDatFilePath.Location = new Point(146, 88);
            txtPhonesDatFilePath.Name = "txtPhonesDatFilePath";
            txtPhonesDatFilePath.Size = new Size(584, 23);
            txtPhonesDatFilePath.TabIndex = 12;
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(12, 91);
            label4.Name = "label4";
            label4.Size = new Size(100, 17);
            label4.TabIndex = 11;
            label4.Text = "Phones Dat File:";
            // 
            // btnSave
            // 
            btnSave.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnSave.Location = new Point(428, 393);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(75, 23);
            btnSave.TabIndex = 14;
            btnSave.Text = "Save";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // openFileDialog1
            // 
            openFileDialog1.FileName = "openFileDialog1";
            openFileDialog1.Filter = "Dat file|*.dat";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(16, 316);
            label5.Name = "label5";
            label5.Size = new Size(86, 17);
            label5.TabIndex = 15;
            label5.Text = "Audio Parser:";
            // 
            // cboAudioParserType
            // 
            cboAudioParserType.DropDownStyle = ComboBoxStyle.DropDownList;
            cboAudioParserType.FormattingEnabled = true;
            cboAudioParserType.Location = new Point(146, 313);
            cboAudioParserType.Name = "cboAudioParserType";
            cboAudioParserType.Size = new Size(137, 25);
            cboAudioParserType.TabIndex = 16;
            // 
            // chkEnableLog
            // 
            chkEnableLog.AutoSize = true;
            chkEnableLog.Location = new Point(17, 249);
            chkEnableLog.Name = "chkEnableLog";
            chkEnableLog.Size = new Size(92, 21);
            chkEnableLog.TabIndex = 17;
            chkEnableLog.Text = "Enable Log";
            chkEnableLog.UseVisualStyleBackColor = true;
            // 
            // chkShowConsoleWindow
            // 
            chkShowConsoleWindow.AutoSize = true;
            chkShowConsoleWindow.Location = new Point(17, 279);
            chkShowConsoleWindow.Name = "chkShowConsoleWindow";
            chkShowConsoleWindow.Size = new Size(160, 21);
            chkShowConsoleWindow.TabIndex = 18;
            chkShowConsoleWindow.Text = "Show Console Window";
            chkShowConsoleWindow.UseVisualStyleBackColor = true;
            // 
            // frmSetting
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(833, 428);
            Controls.Add(chkShowConsoleWindow);
            Controls.Add(chkEnableLog);
            Controls.Add(cboAudioParserType);
            Controls.Add(label5);
            Controls.Add(btnSave);
            Controls.Add(btnChoosePhonesDatFile);
            Controls.Add(txtPhonesDatFilePath);
            Controls.Add(label4);
            Controls.Add(btnChooseSpeechDatFile);
            Controls.Add(txtSpeechDatFilePath);
            Controls.Add(label3);
            Controls.Add(chkIsOfflineMode);
            Controls.Add(groupBox1);
            Controls.Add(btnChooseCacheFolder);
            Controls.Add(txtCacheFolder);
            Controls.Add(label1);
            Name = "frmSetting";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Setting";
            Load += frmSetting_Load;
            groupBox1.ResumeLayout(false);
            groupBox1.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private TextBox txtCacheFolder;
        private Button btnChooseCacheFolder;
        private TextBox txtProxyAddress;
        private CheckBox chkUseProxy;
        private GroupBox groupBox1;
        private Label label2;
        private CheckBox chkIsOfflineMode;
        private Label label3;
        private Button btnChooseSpeechDatFile;
        private TextBox txtSpeechDatFilePath;
        private Button btnChoosePhonesDatFile;
        private TextBox txtPhonesDatFilePath;
        private Label label4;
        private Button btnSave;
        private FolderBrowserDialog folderBrowserDialog1;
        private OpenFileDialog openFileDialog1;
        private Label label5;
        private ComboBox cboAudioParserType;
        private CheckBox chkEnableLog;
        private CheckBox chkShowConsoleWindow;
    }
}
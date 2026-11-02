namespace OpenPronounceSharp.WinApp
{
    partial class frmMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            menuStrip1 = new MenuStrip();
            tsmiSetting = new ToolStripMenuItem();
            btnOpenFile = new Button();
            txtFilePath = new TextBox();
            openFileDialog1 = new OpenFileDialog();
            label2 = new Label();
            txtText = new TextBox();
            btnStopRecord = new Button();
            btnStartRecord = new Button();
            btnPlay = new Button();
            progressBar1 = new ProgressBar();
            groupBox1 = new GroupBox();
            btnAnalyzeRecording = new Button();
            groupBox2 = new GroupBox();
            btnAnalyzeFile = new Button();
            webView = new Microsoft.Web.WebView2.WinForms.WebView2();
            btnPlayText = new Button();
            progressBar2 = new ProgressBar();
            btnPlaySelectedFile = new Button();
            menuStrip1.SuspendLayout();
            groupBox1.SuspendLayout();
            groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)webView).BeginInit();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.Items.AddRange(new ToolStripItem[] { tsmiSetting });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(882, 25);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // tsmiSetting
            // 
            tsmiSetting.Name = "tsmiSetting";
            tsmiSetting.Size = new Size(60, 21);
            tsmiSetting.Text = "Setting";
            tsmiSetting.Click += tsmiSetting_Click;
            // 
            // btnOpenFile
            // 
            btnOpenFile.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnOpenFile.Location = new Point(633, 16);
            btnOpenFile.Name = "btnOpenFile";
            btnOpenFile.Size = new Size(65, 23);
            btnOpenFile.TabIndex = 7;
            btnOpenFile.Text = "...";
            btnOpenFile.UseVisualStyleBackColor = true;
            btnOpenFile.Click += btnOpenFile_Click;
            // 
            // txtFilePath
            // 
            txtFilePath.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtFilePath.Location = new Point(51, 16);
            txtFilePath.Name = "txtFilePath";
            txtFilePath.Size = new Size(576, 23);
            txtFilePath.TabIndex = 6;
            // 
            // openFileDialog1
            // 
            openFileDialog1.Filter = "Mp3 file|*.mp3|Wav file|*.wav|Flac file|*.flac|all files|*.*";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 38);
            label2.Name = "label2";
            label2.Size = new Size(35, 17);
            label2.TabIndex = 8;
            label2.Text = "Text:";
            // 
            // txtText
            // 
            txtText.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtText.Location = new Point(63, 35);
            txtText.Multiline = true;
            txtText.Name = "txtText";
            txtText.Size = new Size(728, 47);
            txtText.TabIndex = 9;
            // 
            // btnStopRecord
            // 
            btnStopRecord.Location = new Point(124, 23);
            btnStopRecord.Margin = new Padding(3, 4, 3, 4);
            btnStopRecord.Name = "btnStopRecord";
            btnStopRecord.Size = new Size(68, 24);
            btnStopRecord.TabIndex = 10;
            btnStopRecord.Text = "Stop";
            btnStopRecord.UseVisualStyleBackColor = true;
            btnStopRecord.Click += btnStopRecord_Click;
            // 
            // btnStartRecord
            // 
            btnStartRecord.Location = new Point(50, 23);
            btnStartRecord.Margin = new Padding(3, 4, 3, 4);
            btnStartRecord.Name = "btnStartRecord";
            btnStartRecord.Size = new Size(68, 24);
            btnStartRecord.TabIndex = 11;
            btnStartRecord.Text = "Start";
            btnStartRecord.UseVisualStyleBackColor = true;
            btnStartRecord.Click += btnStartRecord_Click;
            // 
            // btnPlay
            // 
            btnPlay.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnPlay.Location = new Point(800, 23);
            btnPlay.Margin = new Padding(3, 4, 3, 4);
            btnPlay.Name = "btnPlay";
            btnPlay.Size = new Size(50, 24);
            btnPlay.TabIndex = 12;
            btnPlay.Text = "Play";
            btnPlay.UseVisualStyleBackColor = true;
            btnPlay.Click += btnPlayRecording_Click;
            // 
            // progressBar1
            // 
            progressBar1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            progressBar1.Location = new Point(552, 23);
            progressBar1.Margin = new Padding(3, 4, 3, 4);
            progressBar1.Maximum = 30;
            progressBar1.Name = "progressBar1";
            progressBar1.Size = new Size(223, 24);
            progressBar1.TabIndex = 13;
            // 
            // groupBox1
            // 
            groupBox1.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            groupBox1.Controls.Add(btnAnalyzeRecording);
            groupBox1.Controls.Add(btnStartRecord);
            groupBox1.Controls.Add(btnStopRecord);
            groupBox1.Controls.Add(progressBar1);
            groupBox1.Controls.Add(btnPlay);
            groupBox1.Location = new Point(12, 143);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(856, 60);
            groupBox1.TabIndex = 19;
            groupBox1.TabStop = false;
            groupBox1.Text = "Recording";
            // 
            // btnAnalyzeRecording
            // 
            btnAnalyzeRecording.Location = new Point(200, 23);
            btnAnalyzeRecording.Name = "btnAnalyzeRecording";
            btnAnalyzeRecording.Size = new Size(68, 24);
            btnAnalyzeRecording.TabIndex = 14;
            btnAnalyzeRecording.Text = "Analyze";
            btnAnalyzeRecording.UseVisualStyleBackColor = true;
            btnAnalyzeRecording.Click += btnAnalyzeRecording_Click;
            // 
            // groupBox2
            // 
            groupBox2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            groupBox2.Controls.Add(btnPlaySelectedFile);
            groupBox2.Controls.Add(btnAnalyzeFile);
            groupBox2.Controls.Add(txtFilePath);
            groupBox2.Controls.Add(btnOpenFile);
            groupBox2.Location = new Point(12, 92);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(856, 45);
            groupBox2.TabIndex = 20;
            groupBox2.TabStop = false;
            groupBox2.Text = "File";
            // 
            // btnAnalyzeFile
            // 
            btnAnalyzeFile.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnAnalyzeFile.Location = new Point(704, 16);
            btnAnalyzeFile.Name = "btnAnalyzeFile";
            btnAnalyzeFile.Size = new Size(75, 23);
            btnAnalyzeFile.TabIndex = 8;
            btnAnalyzeFile.Text = "Analyze";
            btnAnalyzeFile.UseVisualStyleBackColor = true;
            btnAnalyzeFile.Click += btnAnalyzeFile_Click;
            // 
            // webView
            // 
            webView.AllowExternalDrop = true;
            webView.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            webView.CreationProperties = null;
            webView.DefaultBackgroundColor = Color.White;
            webView.Location = new Point(12, 222);
            webView.Name = "webView";
            webView.Size = new Size(856, 365);
            webView.TabIndex = 21;
            webView.ZoomFactor = 1D;
            // 
            // btnPlayText
            // 
            btnPlayText.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnPlayText.Location = new Point(812, 58);
            btnPlayText.Margin = new Padding(3, 4, 3, 4);
            btnPlayText.Name = "btnPlayText";
            btnPlayText.Size = new Size(50, 24);
            btnPlayText.TabIndex = 22;
            btnPlayText.Text = "Play";
            btnPlayText.UseVisualStyleBackColor = true;
            btnPlayText.Click += btnPlayText_Click;
            // 
            // progressBar2
            // 
            progressBar2.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            progressBar2.Location = new Point(12, 210);
            progressBar2.Margin = new Padding(3, 4, 3, 4);
            progressBar2.MarqueeAnimationSpeed = 10;
            progressBar2.Name = "progressBar2";
            progressBar2.Size = new Size(856, 3);
            progressBar2.Style = ProgressBarStyle.Marquee;
            progressBar2.TabIndex = 23;
            progressBar2.Visible = false;
            // 
            // btnPlaySelectedFile
            // 
            btnPlaySelectedFile.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnPlaySelectedFile.Location = new Point(800, 17);
            btnPlaySelectedFile.Margin = new Padding(3, 4, 3, 4);
            btnPlaySelectedFile.Name = "btnPlaySelectedFile";
            btnPlaySelectedFile.Size = new Size(50, 24);
            btnPlaySelectedFile.TabIndex = 23;
            btnPlaySelectedFile.Text = "Play";
            btnPlaySelectedFile.UseVisualStyleBackColor = true;
            btnPlaySelectedFile.Click += btnPlaySelectedFile_Click;
            // 
            // frmMain
            // 
            AutoScaleDimensions = new SizeF(7F, 17F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(882, 589);
            Controls.Add(progressBar2);
            Controls.Add(btnPlayText);
            Controls.Add(webView);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Controls.Add(txtText);
            Controls.Add(label2);
            Controls.Add(menuStrip1);
            MainMenuStrip = menuStrip1;
            Name = "frmMain";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Open Pronounce";
            FormClosing += frmMain_FormClosing;
            Load += frmMain_Load;
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            groupBox1.ResumeLayout(false);
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)webView).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuStrip1;
        private ToolStripMenuItem tsmiSetting;
        private Button btnOpenFile;
        private TextBox txtFilePath;
        private OpenFileDialog openFileDialog1;
        private Label label2;
        private TextBox txtText;
        private Button btnStopRecord;
        private Button btnStartRecord;
        private Button btnPlay;
        private ProgressBar progressBar1;
        private GroupBox groupBox1;
        private GroupBox groupBox2;
        private Microsoft.Web.WebView2.WinForms.WebView2 webView;
        private Button btnPlayText;
        private Button btnAnalyzeFile;
        private Button btnAnalyzeRecording;
        private ProgressBar progressBar2;
        private Button btnPlaySelectedFile;
    }
}

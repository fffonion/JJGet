namespace jjget
{
    partial class frmMain
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
            this.btnGetIndex = new System.Windows.Forms.Button();
            this.grpBookInfo = new System.Windows.Forms.GroupBox();
            this.lblChapterCnt = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.lblAuthor = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.btnStart = new System.Windows.Forms.Button();
            this.btnChooseSaveLoc = new System.Windows.Forms.Button();
            this.txtSaveLoc = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.grpConfig = new System.Windows.Forms.GroupBox();
            this.label7 = new System.Windows.Forms.Label();
            this.chkUseMobileEdition = new System.Windows.Forms.CheckBox();
            this.txtProxyPort = new System.Windows.Forms.TextBox();
            this.label6 = new System.Windows.Forms.Label();
            this.txtProxyServ = new System.Windows.Forms.TextBox();
            this.chkUseProxy = new System.Windows.Forms.CheckBox();
            this.chkSplitChapter = new System.Windows.Forms.CheckBox();
            this.lblPromptD = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.lblCntDone = new System.Windows.Forms.Label();
            this.picProgress = new System.Windows.Forms.PictureBox();
            this.lblProgBar = new System.Windows.Forms.Label();
            this.txtNovelID = new System.Windows.Forms.TextBox();
            this.lblPromptU = new System.Windows.Forms.Label();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.grpBookInfo.SuspendLayout();
            this.grpConfig.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picProgress)).BeginInit();
            this.SuspendLayout();
            // 
            // btnGetIndex
            // 
            this.btnGetIndex.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.btnGetIndex.Location = new System.Drawing.Point(345, 66);
            this.btnGetIndex.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnGetIndex.Name = "btnGetIndex";
            this.btnGetIndex.Size = new System.Drawing.Size(108, 27);
            this.btnGetIndex.TabIndex = 0;
            this.btnGetIndex.Text = "GET√";
            this.btnGetIndex.UseVisualStyleBackColor = true;
            this.btnGetIndex.Click += new System.EventHandler(this.btnGetIndex_Click);
            // 
            // grpBookInfo
            // 
            this.grpBookInfo.Controls.Add(this.lblChapterCnt);
            this.grpBookInfo.Controls.Add(this.label3);
            this.grpBookInfo.Controls.Add(this.lblAuthor);
            this.grpBookInfo.Controls.Add(this.label2);
            this.grpBookInfo.Controls.Add(this.label1);
            this.grpBookInfo.Location = new System.Drawing.Point(23, 12);
            this.grpBookInfo.Name = "grpBookInfo";
            this.grpBookInfo.Size = new System.Drawing.Size(316, 219);
            this.grpBookInfo.TabIndex = 2;
            this.grpBookInfo.TabStop = false;
            this.grpBookInfo.Text = "书名";
            // 
            // lblChapterCnt
            // 
            this.lblChapterCnt.AutoSize = true;
            this.lblChapterCnt.Location = new System.Drawing.Point(257, 22);
            this.lblChapterCnt.Name = "lblChapterCnt";
            this.lblChapterCnt.Size = new System.Drawing.Size(18, 20);
            this.lblChapterCnt.TabIndex = 4;
            this.lblChapterCnt.Text = "0";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label3.Location = new System.Drawing.Point(207, 23);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(54, 19);
            this.label3.TabIndex = 3;
            this.label3.Text = "章节：";
            // 
            // lblAuthor
            // 
            this.lblAuthor.AutoSize = true;
            this.lblAuthor.Location = new System.Drawing.Point(60, 23);
            this.lblAuthor.Name = "lblAuthor";
            this.lblAuthor.Size = new System.Drawing.Size(54, 20);
            this.lblAuthor.TabIndex = 2;
            this.lblAuthor.Text = "深井冰";
            // 
            // label2
            // 
            this.label2.Location = new System.Drawing.Point(10, 47);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(285, 169);
            this.label2.TabIndex = 1;
            this.label2.Text = "（＊￣︶￣）ｙ　";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label1.Location = new System.Drawing.Point(6, 23);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(54, 19);
            this.label1.TabIndex = 0;
            this.label1.Text = "作者：";
            // 
            // btnStart
            // 
            this.btnStart.Font = new System.Drawing.Font("微软雅黑", 15F);
            this.btnStart.Location = new System.Drawing.Point(345, 107);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(108, 45);
            this.btnStart.TabIndex = 3;
            this.btnStart.Text = "开始";
            this.btnStart.UseVisualStyleBackColor = true;
            this.btnStart.Click += new System.EventHandler(this.button1_Click);
            // 
            // btnChooseSaveLoc
            // 
            this.btnChooseSaveLoc.Location = new System.Drawing.Point(343, 18);
            this.btnChooseSaveLoc.Name = "btnChooseSaveLoc";
            this.btnChooseSaveLoc.Size = new System.Drawing.Size(88, 28);
            this.btnChooseSaveLoc.TabIndex = 4;
            this.btnChooseSaveLoc.Text = "浏览…";
            this.btnChooseSaveLoc.UseVisualStyleBackColor = true;
            this.btnChooseSaveLoc.Click += new System.EventHandler(this.btnChooseSaveLoc_Click);
            // 
            // txtSaveLoc
            // 
            this.txtSaveLoc.Location = new System.Drawing.Point(18, 48);
            this.txtSaveLoc.Name = "txtSaveLoc";
            this.txtSaveLoc.Size = new System.Drawing.Size(413, 27);
            this.txtSaveLoc.TabIndex = 5;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.label4.Location = new System.Drawing.Point(10, 23);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(73, 19);
            this.label4.TabIndex = 6;
            this.label4.Text = "保存路径:";
            // 
            // grpConfig
            // 
            this.grpConfig.Controls.Add(this.label7);
            this.grpConfig.Controls.Add(this.chkUseMobileEdition);
            this.grpConfig.Controls.Add(this.txtProxyPort);
            this.grpConfig.Controls.Add(this.label6);
            this.grpConfig.Controls.Add(this.txtProxyServ);
            this.grpConfig.Controls.Add(this.chkUseProxy);
            this.grpConfig.Controls.Add(this.chkSplitChapter);
            this.grpConfig.Controls.Add(this.label4);
            this.grpConfig.Controls.Add(this.btnChooseSaveLoc);
            this.grpConfig.Controls.Add(this.txtSaveLoc);
            this.grpConfig.Location = new System.Drawing.Point(16, 366);
            this.grpConfig.Name = "grpConfig";
            this.grpConfig.Size = new System.Drawing.Size(437, 145);
            this.grpConfig.TabIndex = 7;
            this.grpConfig.TabStop = false;
            this.grpConfig.Text = "设置";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(99, 113);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(56, 20);
            this.label7.TabIndex = 13;
            this.label7.Text = "http://";
            // 
            // chkUseMobileEdition
            // 
            this.chkUseMobileEdition.AutoSize = true;
            this.chkUseMobileEdition.Location = new System.Drawing.Point(157, 85);
            this.chkUseMobileEdition.Name = "chkUseMobileEdition";
            this.chkUseMobileEdition.Size = new System.Drawing.Size(106, 24);
            this.chkUseMobileEdition.TabIndex = 12;
            this.chkUseMobileEdition.Text = "使用手机版";
            this.chkUseMobileEdition.UseVisualStyleBackColor = true;
            // 
            // txtProxyPort
            // 
            this.txtProxyPort.Enabled = false;
            this.txtProxyPort.Location = new System.Drawing.Point(353, 108);
            this.txtProxyPort.Name = "txtProxyPort";
            this.txtProxyPort.Size = new System.Drawing.Size(55, 27);
            this.txtProxyPort.TabIndex = 11;
            this.txtProxyPort.Text = "80";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(338, 112);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(13, 20);
            this.label6.TabIndex = 10;
            this.label6.Text = ":";
            // 
            // txtProxyServ
            // 
            this.txtProxyServ.Enabled = false;
            this.txtProxyServ.Location = new System.Drawing.Point(155, 110);
            this.txtProxyServ.Name = "txtProxyServ";
            this.txtProxyServ.Size = new System.Drawing.Size(179, 27);
            this.txtProxyServ.TabIndex = 9;
            // 
            // chkUseProxy
            // 
            this.chkUseProxy.AutoSize = true;
            this.chkUseProxy.Location = new System.Drawing.Point(14, 112);
            this.chkUseProxy.Name = "chkUseProxy";
            this.chkUseProxy.Size = new System.Drawing.Size(91, 24);
            this.chkUseProxy.TabIndex = 8;
            this.chkUseProxy.Text = "使用代理";
            this.chkUseProxy.UseVisualStyleBackColor = true;
            this.chkUseProxy.CheckedChanged += new System.EventHandler(this.chkUseProxy_CheckedChanged);
            // 
            // chkSplitChapter
            // 
            this.chkSplitChapter.AutoSize = true;
            this.chkSplitChapter.Location = new System.Drawing.Point(14, 85);
            this.chkSplitChapter.Name = "chkSplitChapter";
            this.chkSplitChapter.Size = new System.Drawing.Size(136, 24);
            this.chkSplitChapter.TabIndex = 7;
            this.chkSplitChapter.Text = "每章节一个文件";
            this.chkSplitChapter.UseVisualStyleBackColor = true;
            // 
            // lblPromptD
            // 
            this.lblPromptD.AutoEllipsis = true;
            this.lblPromptD.ForeColor = System.Drawing.Color.DarkGreen;
            this.lblPromptD.Location = new System.Drawing.Point(23, 262);
            this.lblPromptD.Name = "lblPromptD";
            this.lblPromptD.Size = new System.Drawing.Size(424, 28);
            this.lblPromptD.TabIndex = 8;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(357, 169);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(69, 20);
            this.label5.TabIndex = 9;
            this.label5.Text = "已完成：";
            // 
            // lblCntDone
            // 
            this.lblCntDone.AutoSize = true;
            this.lblCntDone.Location = new System.Drawing.Point(415, 170);
            this.lblCntDone.Name = "lblCntDone";
            this.lblCntDone.Size = new System.Drawing.Size(18, 20);
            this.lblCntDone.TabIndex = 10;
            this.lblCntDone.Text = "0";
            // 
            // picProgress
            // 
            this.picProgress.Image = ((System.Drawing.Image)(resources.GetObject("picProgress.Image")));
            this.picProgress.Location = new System.Drawing.Point(12, 297);
            this.picProgress.Name = "picProgress";
            this.picProgress.Size = new System.Drawing.Size(53, 65);
            this.picProgress.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picProgress.TabIndex = 11;
            this.picProgress.TabStop = false;
            this.picProgress.Visible = false;
            // 
            // lblProgBar
            // 
            this.lblProgBar.AutoSize = true;
            this.lblProgBar.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblProgBar.Location = new System.Drawing.Point(68, 342);
            this.lblProgBar.Name = "lblProgBar";
            this.lblProgBar.Size = new System.Drawing.Size(24, 18);
            this.lblProgBar.TabIndex = 12;
            this.lblProgBar.Text = "0%";
            this.lblProgBar.Visible = false;
            // 
            // txtNovelID
            // 
            this.txtNovelID.Location = new System.Drawing.Point(345, 25);
            this.txtNovelID.Name = "txtNovelID";
            this.txtNovelID.Size = new System.Drawing.Size(108, 27);
            this.txtNovelID.TabIndex = 13;
            // 
            // lblPromptU
            // 
            this.lblPromptU.AutoEllipsis = true;
            this.lblPromptU.ForeColor = System.Drawing.Color.DarkGreen;
            this.lblPromptU.Location = new System.Drawing.Point(23, 234);
            this.lblPromptU.Name = "lblPromptU";
            this.lblPromptU.Size = new System.Drawing.Size(424, 28);
            this.lblPromptU.TabIndex = 14;
            // 
            // frmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(467, 524);
            this.Controls.Add(this.lblPromptU);
            this.Controls.Add(this.txtNovelID);
            this.Controls.Add(this.lblProgBar);
            this.Controls.Add(this.picProgress);
            this.Controls.Add(this.lblCntDone);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.lblPromptD);
            this.Controls.Add(this.grpConfig);
            this.Controls.Add(this.btnStart);
            this.Controls.Add(this.grpBookInfo);
            this.Controls.Add(this.btnGetIndex);
            this.DoubleBuffered = true;
            this.Font = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "frmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "JJGET";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.frmMain_FormClosing);
            this.Load += new System.EventHandler(this.frmMain_Load);
            this.grpBookInfo.ResumeLayout(false);
            this.grpBookInfo.PerformLayout();
            this.grpConfig.ResumeLayout(false);
            this.grpConfig.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picProgress)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnGetIndex;
        private System.Windows.Forms.GroupBox grpBookInfo;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label lblAuthor;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label lblChapterCnt;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.Button btnChooseSaveLoc;
        private System.Windows.Forms.TextBox txtSaveLoc;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.GroupBox grpConfig;
        private System.Windows.Forms.Label lblPromptD;
        private System.Windows.Forms.CheckBox chkSplitChapter;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label lblCntDone;
        private System.Windows.Forms.PictureBox picProgress;
        private System.Windows.Forms.Label lblProgBar;
        private System.Windows.Forms.CheckBox chkUseProxy;
        private System.Windows.Forms.TextBox txtProxyPort;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox txtProxyServ;
        private System.Windows.Forms.TextBox txtNovelID;
        private System.Windows.Forms.CheckBox chkUseMobileEdition;
        private System.Windows.Forms.Label lblPromptU;
        private System.Windows.Forms.ToolTip toolTip1;
        private System.Windows.Forms.Label label7;
    }
}


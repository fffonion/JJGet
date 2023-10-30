﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Windows.Forms;
using Microsoft.VisualBasic;

namespace jjget
{
    public partial class frmMain : Form
    {
        Novel novel;
        bool need_stop = false;
        bool should_draw_verifycode_retry = false;
        Thread downloadThread = null;
        List<PictureBox> progboxes = new List<PictureBox>();
        ComponentResourceManager res;
        string customCookieStr;
        public frmMain()
        {
            novel = new Novel();
            novel.registerSetProgressDelegate(this.setPrompt);
            novel.registerSetVerifyCodeDelegate(this.setVerifyCode);

            InitializeComponent();
            res = new ComponentResourceManager(typeof(frmMain));
            picVerifyCode.BringToFront();
            chkIgnoreFontDecodingError.BringToFront();
        }

        private void setProgressBar(float prog)
        {
            this.lblProgBar.Text = ((int)(prog * 100)).ToString() + "%";
            int cnt = (int)(prog * 7);
            if (!picProgress.Visible || !lblProgBar.Visible)
            {
                picProgress.Visible = true;
                lblProgBar.Visible = true;
            }
            if (cnt == 0) return;
            while (cnt > progboxes.Count && cnt <= 7)
            {
                PictureBox pb = new PictureBox();
                pb.Image = (Image)res.GetObject("picProgress.Image");
                pb.Location = new Point(12 + (1 + progboxes.Count) * 53, picProgress.Location.Y);
                pb.Size = new Size(53, 65);
                pb.SizeMode = PictureBoxSizeMode.Zoom;
                progboxes.Add(pb);
                this.Controls.Add(pb);
                lblProgBar.Location = new Point(68 + cnt * 53, lblProgBar.Location.Y);
            }
        }

        private void resetProgressBar()
        {
            foreach (PictureBox p in progboxes)
                this.Controls.Remove(p);
            progboxes.Clear();
            lblProgBar.Location = new Point(68, lblProgBar.Location.Y);
            picProgress.Visible = false;
            lblProgBar.Visible = false;
        }

        private void setPrompt(string text)
        {
            setPrompt(text, Color.DarkGreen);
        }

        private void setPrompt(string text, Color c)
        {
            this.Invoke(new Action(() =>
            {
                lblPromptU.Text = lblPromptM.Text;
                lblPromptU.ForeColor = lblPromptM.ForeColor;
                lblPromptM.Text = lblPromptD.Text;
                lblPromptM.ForeColor = lblPromptD.ForeColor;
                lblPromptD.Text = text;
                lblPromptD.ForeColor = c;
                toolTip1.SetToolTip(lblPromptU, lblPromptU.Text);
                toolTip1.SetToolTip(lblPromptM, lblPromptM.Text);
                toolTip1.SetToolTip(lblPromptD, lblPromptD.Text);
            }
            ));
        }

        private void setVerifyCode(byte[] text)
        {
            this.Invoke(new Action(() =>
            {
                should_draw_verifycode_retry = false;
                txtVerifyCode.Text = "";
                if (text.Length == 0)
                {
                    picProgress.Image = null;
                    txtVerifyCode.Enabled = false;
                }
                else
                {
                    txtVerifyCode.Visible = true;
                    txtVerifyCode.Enabled = true;
                    MemoryStream m = new MemoryStream(text);
                    picVerifyCode.Image = Image.FromStream(m);
                    m.Dispose();
                }

            }
            ));
        }


        private void updateNovelSettings()
        {
            novel.registerSetProgressDelegate(this.setPrompt);
            novel.registerSetVerifyCodeDelegate(this.setVerifyCode);
            novel.setUseMobile(chkUseMobileEdition.Checked);
            if (!chkUseProxy.Checked)
                return;
            try
            {
                novel.setHttpUtilProxy("http://" + txtProxyServ.Text + ":" + txtProxyPort.Text);
            }
            catch (UriFormatException)
            {
                MessageBox.Show("代理输入错误", "JJGET-ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void getVerifyCodeAndDisplay()
        {
            try
            {
                novel.checkVerifyCode(txtUsername.Text, false);
            }
            catch (Exception ex)
            {
                MessageBox.Show("验证码获取失败，请重试\n" + ex.ToString(), "JJGET-ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                should_draw_verifycode_retry = true;
                picVerifyCode.Invalidate();
                return;
            }
            
            should_draw_verifycode_retry = false;
        }

        private void btnGetIndex_Click(object sender, EventArgs e)
        {
            novel = new Novel();
            updateNovelSettings();
            if (txtNovelID.Text == "")
            {
                MessageBox.Show("请输入小说id", "JJGET-ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtNovelID.Focus();
                return;
            }
            novel.setSaveLoc(txtSaveLoc.Text);
            try
            {
                if (!novel.getIndex(int.Parse(txtNovelID.Text)))
                    throw new ArgumentNullException("正则匹配失败？");
            }
            catch (Exception ex)
            {
                MessageBox.Show("处理失败！大概是NovelID输错了？\n" + ex.Message, "JJGET-ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                setPrompt("处理失败！ ", Color.Red);
                return;
            }
            grpBookInfo.Text = novel.name;
            label2.Text = novel.descriptions;
            toolTip1.SetToolTip(label2, label2.Text);
            lblAuthor.Text = novel.author;
            lblChapterCnt.Text = novel.chapterCount.ToString();
            lblCntDone.Text = novel.chapterDone.ToString();
            lblFinnished.Visible = novel.isFinnished;
            btnStart.Enabled = true;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int startchap = 1, endchap = novel.chapterCount;
            if(txtStartChap.Text!= "") int.TryParse(txtStartChap.Text, out startchap);
            if(txtEndChap.Text != "") int.TryParse(txtEndChap.Text, out endchap);

            if (startchap < 1 || startchap > novel.chapterCount)
            {
                setPrompt("起始章节有误");
                return;
            }
            if (endchap < 1 || endchap > novel.chapterCount)
            {
                setPrompt("截至章节有误");
                return;
            }
            if (startchap > endchap)
            {
                setPrompt("起始章节大于结束章节");
                return;
            }
            novel.startDlChapter = startchap - 1;
            novel.endDlChapter = endchap;

            updateNovelSettings();
            if (btnStart.Text == "暂停")
            {
                this.need_stop = true;
                return;
            }
            Novel.Chapter chpt = new Novel.Chapter();
            btnStart.Text = "暂停";
            resetProgressBar();
            setProgressBar(0);
            downloadThread = new Thread(new ThreadStart(() =>
            {
                for (int i = novel.startDlChapter + 1; i <= novel.endDlChapter; i++)
                {
                    Thread.Sleep(233);
                    bool terminate = false;
                    for (int retries = 0; retries < 3; retries++)
                    {
                        try
                        {
                            chpt = novel.getSingleChapter(i);
                            if (chpt.title == "") continue;
                            break;
                        }
                        catch (System.Net.WebException ex)
                        {
                            setPrompt(("在章节" + i + "处遇到错误 " + ex.Message.ToString().Split('\n')[0] + "。重试" + (retries + 1) + "次"), Color.Red);
                        }
                        catch (NullReferenceException)
                        {
                            if (novel.isVipChapter(i))
                                setPrompt(("章节" + i + "是VIP章节，" +
                                    (novel.hasLogin ? "可能是账号没有购买，或者登陆过期；请尝试稍后重试" : "需要登录后才能下载")), Color.Red);
                            else
                                MessageBox.Show("处理章节" + i + "时正则匹配失败，请联系作者", "JJGET-ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            terminate = true;
                            break;
                        }
                        catch (Exception ex)
                        {
                            setPrompt(("在章节" + i + "处遇到错误 " + ex.Message).Split('\n')[0], Color.Red);
                            MessageBox.Show("在章节" + i + "处遇到错误\n" + ex.Message, "JJGET-ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            terminate = true;
                            break;
                        }
                    }

                    if (terminate)
                    {
                        this.Invoke(new Action(() =>
                        {
                            btnStart.Text = "开始";
                            novel.writeDebugInfo("jjdebug.zip");
                            setPrompt(("调试信息已经保存到jjdebug.zip，虽然其中不包含登录数据，但请勿公开分享"), Color.Red);
                        }));
                        return;
                    }
                    if (chpt.title == "")
                    {
                        setPrompt(("在章节" + i + "得到空白内容"), Color.Red);
                        return;
                    }
                    novel.saveChapter(chpt, chkSplitChapter.Checked);
                    if (need_stop)
                    {
                        need_stop = false;
                        this.Invoke(new Action(() =>
                        {
                            btnStart.Text = "开始";
                            setPrompt("已暂停www");
                        }));
                        return;
                    }
                    this.Invoke(new Action(() =>
                    {
                        setProgressBar((float)(i-novel.startDlChapter+1) / (novel.endDlChapter-novel.startDlChapter+1));
                        lblCntDone.Text = i.ToString();
                    }));
                }
                this.Invoke(new Action(() =>
                {
                    btnStart.Text = "开始";
                    lblProgBar.Visible = false;
                }
                ));
                setPrompt("《" + novel.name + "》下载结束(*￣▽￣)y ");
                if (novel.isFinnished)
                    novel.deleteProgress();
            }));
            downloadThread.Start();


        }

        private void btnChooseSaveLoc_Click(object sender, EventArgs e)
        {
            string placeholder = "就决定是这里了";
            SaveFileDialog sf = new SaveFileDialog();
            sf.Title = "选择保存目录";
            sf.FileName = placeholder;
            sf.RestoreDirectory = true;
            sf.OverwritePrompt = false;
            sf.CreatePrompt = false;
            sf.Filter = "文件夹|*.";
            if (sf.ShowDialog() == DialogResult.OK)
            {
                txtSaveLoc.Text = Path.GetDirectoryName(sf.FileName);
                novel.setSaveLoc(Path.GetDirectoryName(sf.FileName));
                lblCntDone.Text = novel.chapterDone.ToString();
            }
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            this.Text += " v" + Application.ProductVersion;
            txtSaveLoc.Text = Environment.CurrentDirectory;
            //txtSaveLoc.Text = Path.GetDirectoryName(System.Windows.Forms.Application.StartupPath);
            novel.setSaveLoc(txtSaveLoc.Text);
            Program.SetCueText(txtNovelID, "输入NovelID");
            Program.SetCueText(txtProxyServ, "代理IP");
            Program.SetCueText(txtProxyPort, "代理端口");
            lblLoginInfo.Size = new Size(417, 74);
            lblLoginInfo.Location = new Point(label8.Location.X + 3, lblLoginInfo.Location.Y);
            chkIgnoreFontDecodingError.Location = new Point(
                lblLoginInfo.Location.X + 3, chkIgnoreFontDecodingError.Location.Y);
            loginRoutine(true);
            //lblLoginInfo.Visible = true;
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (downloadThread != null && downloadThread.IsAlive)
                downloadThread.Abort();
        }

        private void chkUseProxy_CheckedChanged(object sender, EventArgs e)
        {
            txtProxyPort.Enabled = chkUseProxy.Checked;
            txtProxyServ.Enabled = chkUseProxy.Checked;
        }


        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            (new frmAbout()).ShowDialog();
        }
        private bool loginRoutine(bool isCheck)
        {
            Action setctls = new Action(() =>
            {
                lblLoginInfo.Visible = true;
                lblLoginInfo.Text = novel.userDetail;
                chkIgnoreFontDecodingError.Visible = true;
                btnLogin.Text = "退出";
                btnLogin.Enabled = true;
                picVerifyCode.Image = null;
                txtVerifyCode.Text = "";
            });
            if (novel.hasLogin)
            {
                this.Invoke(setctls);
                return true;
            }
            if (isCheck)//check only
                return false;
            if (novel.jjLogin(txtUsername.Text, txtPwd.Text, txtVerifyCode.Text, customCookieStr))
            {
                this.Invoke(setctls);
                return true;
            }
            return false;
        }

        private void chkUsePwdMask_CheckedChanged(object sender, EventArgs e)
        {
            if (chkUsePwdMask.Checked)
                txtPwd.PasswordChar = '*';
            else
                txtPwd.PasswordChar = '\0';
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            if (btnLogin.Text == "登陆")
            {
                if (txtUsername.Text == "" || txtPwd.Text == "")
                {
                    MessageBox.Show("请输入用户名密码ww", "JJGET-WARNING", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    return;
                }
                btnLogin.Enabled = false;
                new Thread(new ThreadStart(() =>
                {
                    if (!loginRoutine(false))
                    {
                        MessageBox.Show("登录失败！", "JJGET-WARNING", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        // reset verify code
                        getVerifyCodeAndDisplay();
                    }
                    this.Invoke(new Action(() => btnLogin.Enabled = true));
                })).Start();
            }
            else if (btnLogin.Text == "Cookie登陆")
            {
                string cookieJS = @"javascript: (function(){const input=document.createElement(""input"");input.value=document.cookie;document.body.appendChild(input);input.focus();input.select();var result=document.execCommand(""copy"");document.body.removeChild(input);if(result){prompt(""Cookie已经复制到剪贴板:\n\n"",input.value)}else{prompt(""Cookie复制失败,请手动复制:\n\n"",input.value)}})();";

                Clipboard.SetText(cookieJS);
                while (true)
                {
                    customCookieStr = Interaction.InputBox("请在浏览器中新建一个书签，并将网址填写为下列内容，名称随便（已复制到剪贴板，可直接粘贴）:\n\n" + 
                        cookieJS + "\n\n然后在浏览器中登录晋江之后，点击刚才新建的书签，将Cookie填入程序的弹框中，并按确定。", "请输入Cookie字符串", null);
                    if (customCookieStr == null) return; //escape hatch
                    if (customCookieStr.IndexOf("token", 0) > -1) break;
                    MessageBox.Show("Cookie不太对，请检查复制的Cookie中是否包含token！", "JJGET-WARNING", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                }
                btnLogin.Enabled = false;
                new Thread(new ThreadStart(() =>
                {
                    if (!loginRoutine(false))
                    {
                        MessageBox.Show("登录失败！", "JJGET-WARNING", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        // reset verify code
                        getVerifyCodeAndDisplay();
                    }
                    this.Invoke(new Action(() => btnLogin.Enabled = true));
                })).Start();
            }
            else
            {
                novel.deleteSavedUser();
                lblLoginInfo.Visible = false;
                lblLoginInfo.Text = "";
                chkIgnoreFontDecodingError.Visible = false;
                //btnLogin.Text = "登陆";
                btnLogin.Text = "Cookie登陆";
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {
            MessageBox.Show(label2.Text, "展开全部信息", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MessageBox.Show("【下载】\n" +
                "·使用手机版有时可以加快下载速度\n" +
                "·必须登录已购买VIP章节的账户才能下载VIP章节\n" +
                "·勾选使用手机版时，若遇到VIP章节，会自动切换电脑版\n\n" +
                "【连载】\n" +
                "保留同一目录下的jjget文件即可在下次文章更新后继续下载\n\n" +
                "【字体xxx中的字符xxx无法解码】\n" +
                "如果在下载VIP章节时需要此问题，可以尝试勾选\n" +
                "\"使用？替代V章中解码失败的字符\"，\n" +
                "并人工验证下载文本的可读性\n", "JJGET-HELP", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnOpenDir_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(txtSaveLoc.Text);
        }

        private void txtUsername_Leave(object sender, EventArgs e)
        {
            new Thread(new ThreadStart(() =>
            {
                this.Invoke(new Action(() => btnLogin.Enabled = false));
                getVerifyCodeAndDisplay();
                this.Invoke(new Action(() => btnLogin.Enabled = true));
            })).Start();
        }

        private void LblPrompt_Click(object sender, EventArgs e)
        {
            if (((Label)sender).Text != "")
            {
                MessageBox.Show(((Label)sender).Text, "展开全部信息", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void chkIgnoreFontDecodingError_CheckedChanged(object sender, EventArgs e)
        {
            novel.getFontDecoder().setIgnoreDecodingErrors(chkIgnoreFontDecodingError.Checked);
        }

        private void chkIncludeAuthorsWords_CheckedChanged(object sender, EventArgs e)
        {
            novel.setIncludeAuthorsWords(chkIncludeAuthorsWords.Checked);
        }



        private void picVerifyCode_Click(object sender, EventArgs e)
        {
            getVerifyCodeAndDisplay();
        }

        private void picVerifyCode_Paint(object sender, PaintEventArgs e)
        {
            if (!should_draw_verifycode_retry) return;

            using (Font myFont = new Font("Microsoft Yahei", 10))
            {
                e.Graphics.DrawString("点这里刷新验证码", myFont, Brushes.Green, new Point(2, 2));
            }
        }
    }
}

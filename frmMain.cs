using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Threading;

namespace jjget
{
    public partial class frmMain : Form
    {
        Novel novel;
        bool need_stop = false;
        Thread downloadThread = null;
        List<PictureBox> progboxes = new List<PictureBox>();
        ComponentResourceManager res;
        public frmMain()
        {
            novel = new Novel();
            novel.registerSetProgressDelegate(this.setPrompt);
            novel.registerSetVerifyCodeDelegate(this.setVerifyCode);
            InitializeComponent();
            res = new ComponentResourceManager(typeof(frmMain));
            picVerifyCode.BringToFront();
        }

        private void setProgressBar(float prog)
        {
            this.lblProgBar.Text = ((int)(prog*100)).ToString() + "%";
            int cnt = (int)(prog * 7) ;
            if (!picProgress.Visible || !lblProgBar.Visible)
            {
                picProgress.Visible = true;
                lblProgBar.Visible = true;
            }
            if (cnt == 0) return;
            while (cnt > progboxes.Count && cnt<=7)
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
            foreach(PictureBox p in progboxes)
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
            this.Invoke(new Action(() => {
                    lblPromptU.Text = lblPromptD.Text;
                    lblPromptU.ForeColor = lblPromptD.ForeColor;
                    lblPromptD.Text = text;
                    lblPromptD.ForeColor = c;
                    toolTip1.SetToolTip(lblPromptU, lblPromptU.Text);
                    toolTip1.SetToolTip(lblPromptD, lblPromptD.Text);
                }
            ));
        }

        private void setVerifyCode(byte[] text)
        {
            this.Invoke(new Action(() => {
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

        private void btnGetIndex_Click(object sender, EventArgs e)
        {
            novel = new Novel();
            updateNovelSettings();
            if (txtNovelID.Text == "")
            {
                MessageBox.Show("请输入小说id","JJGET-ERROR",MessageBoxButtons.OK,MessageBoxIcon.Error);
                txtNovelID.Focus();
                return;
            }
            novel.setSaveLoc(txtSaveLoc.Text);
            try
            {
                if(!novel.getIndex(int.Parse(txtNovelID.Text)))
                    throw new ArgumentNullException("正则匹配失败？");
            }catch(Exception ex){
                MessageBox.Show("处理失败！大概是NovelID输错了？\n"+ex.Message, "JJGET-ERROR",MessageBoxButtons.OK,MessageBoxIcon.Error);
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
            //novel = new Novel();
            //novel.chapterCount = 999;
            updateNovelSettings();
            if (btnStart.Text == "暂停")
            {
                this.need_stop = true;
                return;
            }
            Novel.Chapter chpt=new Novel.Chapter();
            btnStart.Text = "暂停";
            resetProgressBar();
            setProgressBar((float)novel.chapterDone / novel.chapterCount);
            downloadThread = new Thread(new ThreadStart(() =>
            {
                for (int i = novel.chapterDone + 1; i <= novel.chapterCount; i++)
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
                            if(novel.isVipChapter(i))
                                setPrompt(("章节" + i + "是VIP章节，"+ 
                                    (novel.hasLogin?"账号没有购买，或者登陆过期":"需要登录后才能下载")), Color.Red);
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
                            setPrompt(("已中断下载"), Color.Red);
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
                        setProgressBar((float)i / novel.chapterCount);
                        lblCntDone.Text = i.ToString();
                    }));
                }
                this.Invoke(new Action(() => { 
                    btnStart.Text = "开始";
                    lblProgBar.Visible = false;
                }
                ));
                setPrompt("《"+novel.name + "》下载结束(*￣▽￣)y ");
                if(novel.isFinnished)
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
            lblLoginInfo.Location = new Point(label8.Location.X+3, lblLoginInfo.Location.Y);
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
            Action setctls = new Action(() => {
                lblLoginInfo.Visible = true;
                lblLoginInfo.Text = novel.userDetail;
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
            if (novel.jjLogin(txtUsername.Text, txtPwd.Text, txtVerifyCode.Text))
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
            if(btnLogin.Text == "登陆"){
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
                        novel.checkVerifyCode(txtUsername.Text, false);
                    }
                    this.Invoke(new Action(() => btnLogin.Enabled = true));
                })).Start();
            }else
            {
                novel.deleteSavedUser();
                lblLoginInfo.Visible = false;
                lblLoginInfo.Text = "";
                btnLogin.Text = "登陆";
            }
        }

        private void label2_Click(object sender, EventArgs e)
        {
            MessageBox.Show(label2.Text, "展开全部信息", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
        }

        private void linkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            MessageBox.Show("【下载】\n"+
                "·使用手机版有时可以加快下载速度\n"+
                "·必须登录已购买VIP章节的账户才能下载VIP章节\n"+
                "·勾选使用手机版时，若遇到VIP章节，会自动切换电脑版\n\n"+
                "【连载】\n"+
                "保留同一目录下的jjget文件即可在下次文章更新后继续下载", "JJGET-HELP", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnOpenDir_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(txtSaveLoc.Text);
        }

        private void txtUsername_Leave(object sender, EventArgs e)
        {
            new Thread(new ThreadStart(() => {
                this.Invoke(new Action(() => btnLogin.Enabled = false));
                novel.checkVerifyCode(txtUsername.Text, false);
                this.Invoke(new Action(() => btnLogin.Enabled = true));
            })).Start();
        }

        private void LblPromptU_Click(object sender, EventArgs e)
        {
            if(lblPromptU.Text!= "")
            {
                MessageBox.Show(lblPromptU.Text, "展开全部信息", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }

        private void LblPromptD_Click(object sender, EventArgs e)
        {
            if (lblPromptD.Text != "")
            {
                MessageBox.Show(lblPromptD.Text, "展开全部信息", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
using HtmlAgilityPack;

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
            novel.setDelegate(this.setPrompt);
            InitializeComponent();
            res = new System.ComponentModel.ComponentResourceManager(typeof(frmMain));
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
                pb.Image = ((System.Drawing.Image)(res.GetObject("picProgress.Image")));
                pb.Location = new System.Drawing.Point(12 + (1 + progboxes.Count) * 53, picProgress.Location.Y);
                pb.Size = new System.Drawing.Size(53, 65);
                pb.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
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

        private void updateNovelSettings()
        {
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
                MessageBox.Show("处理失败！\n"+ex.Message, "JJGET-ERROR",MessageBoxButtons.OK,MessageBoxIcon.Error);
                setPrompt("处理失败！ " + ex.Message, Color.Red);
                return;
            }
            grpBookInfo.Text = novel.name;
            label2.Text = novel.descriptions; 
            lblAuthor.Text = novel.author;
            lblChapterCnt.Text = novel.chapterCount.ToString();
            lblCntDone.Text = novel.chapterDone.ToString();
            lblFinnished.Visible = novel.isFinnished;
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
                            setPrompt(("在章节" + i + "处遇到错误 " + ex.Message.ToString().Split('\n')[0]+"。重试"+(retries+1)+"次"), Color.Red);
                        }
                        catch (Exception ex)
                        {
                            setPrompt(("在章节" + i + "处遇到错误 " + ex.Message).Split('\n')[0], Color.Red);
                            MessageBox.Show("在章节" + i + "处遇到错误\n" + ex.Message, "JJGET-ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            break;
                        }
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
                setPrompt(novel.name + "下载结束(*￣▽￣)y ");
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
            txtSaveLoc.Text = Path.GetDirectoryName(System.Windows.Forms.Application.StartupPath);
            Program.SetCueText(txtNovelID, "输入NovelID");
            Program.SetCueText(txtProxyServ, "代理IP");
            Program.SetCueText(txtProxyPort, "代理端口");
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




    }
}

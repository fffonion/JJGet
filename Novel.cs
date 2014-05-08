using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Net;
using System.Drawing;
using HtmlAgilityPack;

namespace jjget
{
    class Novel
    {
        public string name;
        public string author;
        public string descriptions;
        public int chapterCount;
        public int chapterDone;
        private int novelid;
        private string savePath;
        private bool useMobileEdition = true;
        private HttpUtil _savedHttpUtil = null;
        private WebProxy proxy = null;
        private Action<String, Color> setProgressDelegate;
        public struct Chapter
        {
            public int chapterIndex;
            public string title;
            public string content;
            public override string ToString()
            {
                return chapterIndex +" " +title+"\r\n"+content;
            }
        }

        public void setSaveLoc(string loc)
        {
            this.savePath = loc;
            this.readProgress();
        }

        public void setUseMobile(bool use)
        {
            this.useMobileEdition = use;
        }

        public void setDelegate(Action<String, Color> d)
        {
            this.setProgressDelegate = d;
        }

        private void setPrompt(string text)
        {
            setProgressDelegate(text, Color.DarkGreen);
        }

        private void setPrompt(string text, Color c)
        {
            setProgressDelegate(text, c);
        }

        public void setHttpUtilProxy(string uri)
        {
            proxy = new WebProxy();
            proxy.Address = new Uri(uri);
        }

        private string getNovelURL()
        {
            if (this.useMobileEdition)
                return "http://m.jjwxc.com/book2/" + this.novelid;
            else
                return "http://www.jjwxc.net/onebook.php?novelid=" + this.novelid;
        }

        private string getChapterURL(int chap)
        {
            if (this.useMobileEdition)
                return getNovelURL() + "/" + chap;
            else
                return getNovelURL() + "&chapterid=" + chap;
        }

        private string getReferer()
        {
            if (this.useMobileEdition)
                return "http://m.jjwxc.com/";
            else
                return "http://www.jjwxc.net/";
        }

        private HttpUtil getHTTPUtil(){
            if (this.useMobileEdition || _savedHttpUtil == null)
            {
                HttpUtil hu = new HttpUtil();
                hu.setProxy(this.proxy);
                hu.setEncoding("gb2312");
                _savedHttpUtil = hu;
                return hu;
            }
            else
                return _savedHttpUtil;
        }
        private string parseText_Img_Link(HtmlNode node)
        {
            HtmlNode nd = null;
            if (node == null) return "";
            while ((nd = node.SelectSingleNode("./hr")) != null)
            {
                try
                {
                    node.RemoveChild(nd);
                }
                catch (ArgumentOutOfRangeException) { break; }
            }
            string result = node.InnerHtml;
            HtmlNodeCollection hnc = node.SelectNodes("./img|./input");
            if (hnc != null)
            {
                List<HtmlNode> hnl = hnc.ToList();
                if (hnl != null)
                {
                    foreach (HtmlNode hn in hnl)
                    {
                        string onclick = hn.GetAttributeValue("onclick", "window.open(\"\")");
                        result = result.Replace(hn.OuterHtml,
                            hn.GetAttributeValue("src", "1")
                            + hn.GetAttributeValue("value", "")
                            + onclick.Substring(13, onclick.Length - 15));
                    }
                }
            }
            return "\r\n" + result.Replace("<br>", "\r\n").Replace("</br>", "\r\n").Trim();
        }
        public bool getIndex(int novelid){
            this.novelid = novelid;
            setPrompt("下载首页中……", Color.Orange);
            HttpUtil hu = getHTTPUtil();
            string index = hu.Get(getNovelURL(), getReferer());
            //StreamReader sFile = new StreamReader("z://1.htm", Encoding.GetEncoding("gb2312"));
            //string index = sFile.ReadToEnd();
            HtmlAgilityPack.HtmlDocument hd = new HtmlAgilityPack.HtmlDocument();
            hd.LoadHtml(index);
            //setPrompt("分析中……",Color.Orange);
            HtmlNode hn;
            HtmlNodeCollection hnc;
            HtmlNode root = hd.DocumentNode;
            if (useMobileEdition)
            {
                //book title
                hn = root.SelectSingleNode("//h2");
                this.name = hn.InnerText.Substring(3);
                //descriptions
                hnc = root.SelectNodes("//div[@class='b module']/ul/li");
                foreach (HtmlNode n in hnc)
                {
                    if (n.SelectSingleNode("a") != null && this.author == "")
                    {
                        this.author = hnc[0].SelectSingleNode("a").InnerText;
                        continue;
                    }
                    this.descriptions += n.InnerText.Replace("\n", "").Replace("\r", "").
                        Replace(" ", "").Replace(":", "：") + "\n";
                }
                //chapters
                this.chapterCount = root.SelectNodes("//div[@style='padding-left:10px']/a").Count;
            }
            else 
            {
                this.name = root.SelectSingleNode("//span[@itemprop='articleSection']").InnerText;
                this.author = root.SelectSingleNode("//span[@itemprop='author']").InnerText;
                hnc = root.SelectNodes("//ul[@class='rightul']/li");
                for (int idx=0;idx<6;idx++)
                {
                    HtmlNode n = hnc[idx];
                    this.descriptions += HtmlEntity.DeEntitize(n.InnerText.Replace("\n", "").Replace("\r", "").
                        Replace(" ", "").Replace(":", "："))+ "\n";
                }
                this.descriptions += root.SelectSingleNode("//div[@class='smallreadbody']/font").InnerText;
                this.chapterCount = root.SelectNodes("//table[@class='cytable']/tbody/tr[@itemprop='chapter']").Count+1;
            }
            setPrompt("首页分析完成，可以开始了");
            readProgress();
            return true;
        }
        public Chapter getSingleChapter(int chapter)
        {
            if (chapter > this.chapterCount)
            {
                throw new IndexOutOfRangeException("章节号"+chapter+"超出总数("+this.chapterCount+")");
            }
            HttpUtil hu = getHTTPUtil();
            setPrompt("章节" + chapter + "下载中……", Color.Orange);
            string html = hu.Get(getChapterURL(chapter), getNovelURL());
            //StreamReader sFile = new StreamReader("z://2.htm", Encoding.GetEncoding("gb2312"));
            //string html = sFile.ReadToEnd();
            //setPrompt("分析中……", Color.Orange);
            HtmlAgilityPack.HtmlDocument hd = new HtmlAgilityPack.HtmlDocument();
            hd.LoadHtml(html);
            HtmlNodeCollection hnc;
            HtmlNode root = hd.DocumentNode;
            Chapter chpt = new Chapter();
            if (useMobileEdition)
            {
                chpt.title = root.SelectSingleNode("//h2[@class='big o']").InnerText;
                chpt.title = chpt.title.Substring(chpt.title.IndexOf("、") + 1, chpt.title.Length - 4 - chpt.title.IndexOf("、")).Trim();
                hnc = root.SelectNodes("//body/div/div/div/div/ul/li");
                chpt.content = HtmlEntity.DeEntitize(hnc[0].InnerHtml.Replace("<br>", "\r\n"));
                string author_words = HtmlEntity.DeEntitize(hnc[1].InnerText);
                if (author_words.Length > 9)
                    chpt.content += "\r\n" + author_words;
            }
            else
            {
                HtmlNode novelnode = root.SelectSingleNode("//div[@class='noveltext']");
                chpt.title = novelnode.SelectSingleNode("./div/h2").InnerText;
                //作者的话
                HtmlNode hn2 = root.SelectSingleNode("//div[@class='readsmall']");
                chpt.content = parseText_Img_Link(hn2);
                //内容
                List<HtmlNode> hnl = novelnode.SelectNodes("./div|./font|./hr").ToList();
                foreach(HtmlNode hn in hnl){
                    novelnode.RemoveChild(hn);
                }
                chpt.content = "　　" + HtmlEntity.DeEntitize(
                        novelnode.InnerHtml.Replace("<br>","\r\n").Replace("</br>","\r\n"))
                    .Trim() +  chpt.content;
                
                
            }
            chpt.chapterIndex = chapter;
            setPrompt("章节"+chapter+"("+chpt.title+")已完成");
            chapterDone = chapter;
            return chpt;
        }
        public void saveChapter(Chapter chpt, bool split)
        {
            string savepath;
            if (split)
                savepath = this.savePath + "\\" + this.name + "-" + chpt.chapterIndex.ToString("D3") + ".txt";
            else
                savepath = this.savePath + "\\" + this.name + ".txt";
            FileStream fs = new FileStream(savepath, FileMode.Append, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs);   
            sw.Write(chpt.ToString());
            sw.Close();
            fs.Close();
            //setPrompt("章节" + chpt.chapterIndex + "(" + chpt.title + ")已写入磁盘");
            saveProgress(chpt.chapterIndex);
        }
        private void saveProgress(int chpt)
        {
            string savepath = this.savePath + "\\" + this.name + ".jjget";
            FileStream fs = new FileStream(savepath, FileMode.Create, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs);
            sw.Write(chpt.ToString());
            sw.Close();
            fs.Close();
        }
        private void readProgress()
        {
            string savepath = this.savePath + "\\" + this.name + ".jjget";
            try
            {
                FileStream fs = new FileStream(savepath, FileMode.Open);
                StreamReader sr = new StreamReader(fs);
                chapterDone = int.Parse(sr.ReadToEnd());
                sr.Close();
                fs.Close();
            }
            catch (Exception)
            {
                chapterDone = 0;
            }

        }
    }
}

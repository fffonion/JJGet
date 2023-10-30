using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Net;
using System.Drawing;
using HtmlAgilityPack;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.IO.Compression;

namespace jjget
{
    class Novel
    {
        public string name;
        public string author;
        public string descriptions;
        public int chapterCount;
        public int chapterDone;
        public int startDlChapter;
        public int endDlChapter;
        private int novelid;
        private string savePath;
        public bool isFinnished;
        private bool useMobileEdition = true;
        private bool includeAuthorsWords = true;
        private HttpUtil _savedHttpUtil = null;
        private string cookiestr;
        public string userDetail;
        public bool hasLogin = false;
        private string lastParsedHTML;
        private WebProxy proxy = null;
        private Action<string, Color> setProgressDelegate;
        private Action<byte[]> setVerifyCodeDelegate;
        private List<int> vipChapters = new List<int>();
        private FontDecoder fontDecoder = new FontDecoder();
        private Decryptor decryptor = new Decryptor();
        public struct Chapter
        {
            public int chapterIndex;
            public string title;
            public string content;
            public bool isVip;
            public override string ToString()
            {
                return "第" + chapterIndex + "章 " + (isVip ? "[VIP]" : "") + " " + title + "\r\n" + content;
            }
        }

        public void setSaveLoc(string loc)
        {
            this.savePath = loc;
            this.readProgress();
            this.readSavedUser();
        }

        public void setUseMobile(bool use)
        {
            this.useMobileEdition = use;
        }

        public void setIncludeAuthorsWords(bool inc)
        {
            this.includeAuthorsWords = inc;
        }

        public void registerSetProgressDelegate(Action<String, Color> d)
        {
            this.setProgressDelegate = d;

            this.fontDecoder.registerSetProgressDelegate(d);

            this.decryptor.registerSetProgressDelegate(d);
        }

        private void setPrompt(string text)
        {
            setProgressDelegate(text, Color.DarkGreen);
        }

        private void setPrompt(string text, Color c)
        {
            setProgressDelegate(text, c);
        }

        public void registerSetVerifyCodeDelegate(Action<byte[]> d)
        {
            this.setVerifyCodeDelegate = d;
        }

        public void setVerifyCode(byte[] b)
        {
            this.setVerifyCodeDelegate(b);
        }

        public void setHttpUtilProxy(string uri)
        {
            proxy = new WebProxy();
            proxy.Address = new Uri(uri);
        }

        private string getNovelURL()
        {
            return getNovelURL(false, true, true);
        }

        private string getNovelURL(bool isVip, bool more, bool whole)
        {
            if (this.useMobileEdition)
                if (isVip)
                    return "http://m.jjwxc.com/vip/" + this.novelid + (whole ? "?whole=1" : "");
                else
                    return "http://m.jjwxc.com/book2/" + this.novelid + 
                        (whole ? "?whole=1": "") +
                        (more ? (whole ? "&":"?") + "more=1" : "");
            else
            {
                if (isVip)
                    return "http://my.jjwxc.net/onebook_vip.php?novelid=" + this.novelid;
                else
                    return "http://www.jjwxc.net/onebook.php?novelid=" + this.novelid;
            }
        }

        private string getChapterURL(int chap, bool isVip)
        {
            if (this.useMobileEdition)
                return getNovelURL(isVip, false, false) + "/" + chap +
                    (isVip ? ("?ctime=" + Math.Floor(new Random().NextDouble() * 2000000000)) : "");
            else
                return getNovelURL(isVip, false, false) + "&chapterid=" + chap;
        }

        private string getReferer()
        {
            if (this.useMobileEdition)
                return "http://m.jjwxc.com/";
            else
                return "http://www.jjwxc.net/";
        }

        private HttpUtil getHTTPUtil() {
            if (this.useMobileEdition || _savedHttpUtil == null)
            {
                HttpUtil hu = new HttpUtil();
                hu.cookiestr = cookiestr;
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

        private string decodeCustomFont(string ct, string fontName)
        {
            return fontDecoder.Decode(ct, fontName);
        }

        public bool checkVerifyCode(string username, bool force)
        {
            _savedHttpUtil= null; // force a new httputil to not reuse connection pool
            HttpUtil hu = getHTTPUtil();
            Random rnd = new Random();
            string result = hu.Get("http://my.jjwxc.net/login.php?action=checkneedauthnum&" +
                "r=" + rnd.NextDouble() + "00&username=" + username);
            Regex regex = new Regex(@"isneed""\s*:\s*true");
            if (regex.Match(result).Success || force)
            {
                cookiestr = "";
                hu.cookiestr = "";
                byte[] img = hu.GetBinary("http://my.jjwxc.net/include/checkImage.php?random=" +
                    rnd.NextDouble() + "00", "image/webp,image/apng,image/*,*/*;q=0.8");
                this.setVerifyCode(img);

                cookiestr = hu.cookiestr;
                return true;
            }
            return false;
        }

        public bool jjLogin(string username, string pwd, string verifycode, string customCookiestr)
        {
            setPrompt("正在登陆……");
            HttpUtil hu = getHTTPUtil();
            if(customCookiestr != null)
            {
                hu.cookiestr = customCookiestr;
                cookiestr = customCookiestr;
            } else
            {
                string result = hu.Get("http://my.jjwxc.net/login.php?" +
                     "action=login&login_mode=ajax&loginname=" + username + "&loginpassword=" + pwd + "&" +
                     "Ekey=&Challenge=&auth_num=" + verifycode + "&cookietime=1&" +
                     "client_time=" + HttpUtil.getTimeStamp() +
                     "&jsonp=jQuery18008677560358499031_" + HttpUtil.getTimeStamp() + "123" +
                     "&_=" + HttpUtil.getTimeStamp() + "123");
                Regex jsonpRegex = new Regex(@"^[^(]+\((.+)\)$");
                Match m = jsonpRegex.Match(result);

                if (!m.Success)
                {
                    setPrompt("登陆响应无法解析，试试用邮箱登陆？ " + result);
                }
                else
                {
                    result = m.Groups[1].Captures[0].ToString();
                    JObject jresult = JObject.Parse(result);
                    if (jresult["state"].ToString() == "10")
                    {
                        checkVerifyCode(username, true);
                    }
                    setPrompt("登陆失败QAQ: " + jresult["message"].ToString(), Color.Red);

                }
                cookiestr = hu.cookiestr;
            }

            hasLogin = cookiestr != null && cookiestr.IndexOf("token", 0) > -1;
            if (hasLogin)
            {
                setPrompt("正在获取用户信息……");
                getUserDetail();
                writeSavedUser();
                setPrompt("登陆成功(*￣▽￣)y ");
                return true;
            }

            return false;

        }
        private void getUserDetail()
        {
            HttpUtil hu = getHTTPUtil();
            string my = hu.Get("http://my.jjwxc.net/backend/userinfo.php");
            HtmlDocument hd = new HtmlDocument();
            hd.LoadHtml(my);
            HtmlNode root = hd.DocumentNode;
            HtmlNode tb = root.SelectSingleNode("//table[@bgcolor='#009900']");
            bool isWriter = false;
            var detailTr = tb.SelectSingleNode(".//tr//font[@class='readerid']");
            if (detailTr == null)
            {
                detailTr = tb.SelectSingleNode("./tr[1]//div[1]");
                isWriter = true;
            }
            userDetail = "已登录 ID: " + (detailTr != null ? detailTr.InnerText.Trim(): "未知ID");
            var _nickInput = tb.SelectSingleNode("//input[@name='birthday']");
            string _nick = _nickInput == null ? _nickInput.GetAttributeValue("value", "未知昵称") : "未知昵称";
            if (_nick != "您还没有设置昵称")
                userDetail += " " + _nick;
            // disable email for now
            // userDetail += " "+tb.SelectSingleNode("./tr[" + (isWriter ? "8" : "7") + "]//td[@id='emailtd']/text()").InnerText;
        }

        private string stripEmpty(string input)
        {
            return input.Replace("\n", "").Replace("\r", "").
                        Replace(" ", "");
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
                hnc = root.SelectNodes("//div[@class='b module']/ul//li");
                foreach (HtmlNode n in hnc)
                {
                    if (n.SelectSingleNode("a") != null && this.author == "")
                    {
                        this.author = hnc[0].SelectSingleNode("a").InnerText;
                        continue;
                    }
                    this.descriptions += stripEmpty(n.InnerText).Replace(":", "：") + "\n";
                }
                //chapters
                HtmlNodeCollection chaps = root.SelectNodes("//div[@style='padding-left:10px']/a");
                this.chapterCount = chaps.Count;
                foreach (HtmlNode c in chaps)
                {
                    if (c.OuterHtml.IndexOf("vip") != -1)
                        vipChapters.Add(chaps.IndexOf(c)+1);
                }
            }
            else 
            {
                var nameElem = root.SelectSingleNode("//span[@itemprop='articleSection']");
                if (nameElem == null)
                {
                    nameElem = root.SelectSingleNode("//span[@class='bigtext']");
                }
                this.name = stripEmpty(nameElem.InnerText);
                var authorElem = root.SelectSingleNode("//span[@itemprop='author']");
                if(authorElem == null)
                {
                    authorElem = root.SelectSingleNode("//td[@class='noveltitle']/a");
                }
                this.author = authorElem.InnerText;
                hnc = root.SelectNodes("//ul[@class='rightul']/li");
                for (int idx=0;idx<8;idx++)
                {
                    HtmlNode n = hnc[idx];
                    this.descriptions += HtmlEntity.DeEntitize(n.InnerText.Replace("\n", "").Replace("\r", "").
                        Replace(" ", "").Replace(":", "："));
                    // 出版信息
                    var imgs = n.SelectNodes(".//img");
                    if(imgs != null )
                    {
                        foreach (var img in imgs)
                        {
                            this.descriptions += img.GetAttributeValue("title", "") + " ";
                        }
                    }
                    this.descriptions += "\n";
                }
                this.descriptions += root.SelectSingleNode("//div[@class='smallreadbody']/div[@id='novelintro']").InnerText;
                HtmlNodeCollection chaps = root.SelectNodes("//table[@class='cytable']/tbody/tr[@itemtype='http://schema.org/Chapter']");
                if(chaps != null)
                {
                    foreach (HtmlNode c in chaps)
                    {
                        if (c.InnerHtml.IndexOf("onebook_vip") != -1)
                            vipChapters.Add(int.Parse(c.SelectSingleNode("./td[1]").InnerText.Trim()));
                    }
                    this.chapterCount = chaps.Count;
                } else
                {
                    // 没有独立章节
                    this.chapterCount = 1;
                }
            }
            isFinnished = descriptions.IndexOf("连载中") == -1;
            setPrompt("首页分析完成，可以开始了");
            readProgress();
            return true;
        }

        private string getInputLabelValue(HtmlNode root, string name)
        {
            var node = root.SelectSingleNode("//input[@name='" + name + "']");
            if(node == null)
            {
                return "";
            }
            return node.GetAttributeValue("value", "");
        }

        public Chapter getSingleChapter(int chapter)
        {
            if (chapter > this.chapterCount)
                throw new IndexOutOfRangeException("章节号"+chapter+"超出总数("+this.chapterCount+")");
            bool isVip = isVipChapter(chapter);
            if (isVip)
            {
                if(!hasLogin)
                    throw new Exception("章节" + chapter + "是VIP章节，请登陆ww");
                if (useMobileEdition)
                {
                    setPrompt("已切换回电脑版", Color.Cyan);
                    useMobileEdition = false;
                }
            }
            HttpUtil hu = getHTTPUtil();
            setPrompt("章节" + chapter + (isVip?"[VIP]":"") + "下载中……", Color.Orange);
            // VIP章节忽略返回的cookie，好像是假的
            string html = hu.Get(getChapterURL(chapter, isVip), getNovelURL(), isVip);
            //FileStream fs = new FileStream(@"D:\Workspace\JJGet\s.txt", FileMode.Open);
            //StreamReader sFile = new StreamReader(fs);
            //string html = sFile.ReadToEnd();
            //setPrompt("分析中……", Color.Orange);
            lastParsedHTML = html;
            HtmlAgilityPack.HtmlDocument hd = new HtmlAgilityPack.HtmlDocument();
            hd.LoadHtml(html);
            HtmlNodeCollection hnc;
            HtmlNode root = hd.DocumentNode;
            Chapter chpt = new Chapter();
            chpt.isVip = isVip;
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
                HtmlNode novelnode = root.SelectSingleNode("//div[@class='novelbody']/div");
                if(novelnode == null)
                {
                    throw new Exception("章节" + chapter + (isVip ? "是VIP章节，请登陆ww":"解析失败"));
                }
                //自定义字体
                string fontName = "";
                foreach (string cls in novelnode.GetClasses())
                {
                    if (cls.StartsWith("jjwxcfont_"))
                    {
                        fontName = cls;
                    }
                }
                chpt.title = novelnode.SelectSingleNode("./div/h2").InnerText;

                //作者的话
                if (this.includeAuthorsWords)
                {
                    HtmlNode hn2 = root.SelectSingleNode("//div[@class='readsmall']");
                    chpt.content = parseText_Img_Link(hn2);
                } else
                {
                    chpt.content = "";
                }

                //内容
                string mainbody;

                var encryptedContent = getInputLabelValue(root, "content");
                if(encryptedContent != "")
                {
                    string[] contextKeys = {
                        "jsver", "cryptInfo", "accessKey",
                        "contentid", "novelid", "chapterid",
                    };
                    var context = new Dictionary<string, string>();
                    foreach(string key in contextKeys)
                    {
                        context[key] = getInputLabelValue(root, key);
                    }
                    // jsid 就是用户ID
                    Regex jsidRegex = new Regex(@"//my\.jjwxc\.net/backend/favorite\.php\?jsid=(\d+)");
                    var match = jsidRegex.Match(root.InnerHtml);
                    if(!match.Success)
                    {
                        throw new Exception("jsid提取失败");
                    }
                    context["jsid"] = match.Groups[1].Value;

                    mainbody = this.decryptor.Decrypt(encryptedContent, context);
                } else
                {
                    novelnode = novelnode.Clone(); // 避免修改DocumentNode
                    List<HtmlNode> hnl = novelnode.SelectNodes("./div|./font|./hr|./img|./script").ToList();
                    foreach (HtmlNode hn in hnl)
                    {
                        novelnode.RemoveChild(hn);
                    }
                    mainbody = novelnode.InnerHtml;
                }

                var styles = "";
                foreach (var e in root.SelectNodes("//style"))
                {
                    styles += e.InnerText;
                }
                var cssRegexStr = @"[^{]+{\s*content\s*:\s*['""](.*?)['""]\s*;*\s*}";
                mainbody = Regex.Replace(mainbody, @"<span\s+class='([^']+)'>([^<]+)</span>", m => {
                    var clsName = m.Groups[1].Value;
                    var text = m.Groups[2].Value;
                    var mb = Regex.Match(styles, clsName + ":before" + cssRegexStr);
                    if(mb.Success)
                    {
                        text = mb.Groups[1].Value + text;
                    }
                    var ma = Regex.Match(styles, clsName + ":after" + cssRegexStr);
                    if (ma.Success)
                    {
                        text += ma.Groups[1].Value;
                    }
                    return text;
                 });


                mainbody = HtmlEntity.DeEntitize(
                       mainbody.Replace("<br>", "\r\n").Replace("</br>", "\r\n").Replace("&zwnj;", ""));

                if (fontName != "")
                {
                    mainbody = decodeCustomFont(mainbody, fontName);
                }
                chpt.content = "　　" +
                    mainbody.Replace("@无限好文，尽在晋江文学城", "").Replace("@无限好文，尽晋江文学城","").Trim() + 
                    chpt.content;
            }
            chpt.chapterIndex = chapter;
            setPrompt("章节"+chapter+"("+chpt.title+")已完成");
            chapterDone = chapter;
            return chpt;
        }

        public bool isVipChapter(int chptindx)
        {
            return vipChapters.IndexOf(chptindx) != -1;
        }

        Regex legalPath = new Regex(@"[|:?\\/*'<>]|\.+(?:$)");
        public void saveChapter(Chapter chpt, bool split)
        {
            string savepath;
            var legalName = legalPath.Replace(this.name, "_", -1);
            if (split)
                savepath = this.savePath + "\\" + legalName + "-" + chpt.chapterIndex.ToString("D3") + ".txt";
            else
                savepath = this.savePath + "\\" + legalName + ".txt";

            FileStream fs = new FileStream(savepath, FileMode.Append, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs);
            if(chpt.chapterIndex == 1)
            {
                sw.WriteLine(this.descriptions + "\r\n");
            }
            sw.WriteLine(chpt.ToString() + "\r\n");
            sw.Close();
            fs.Close();
            //setPrompt("章节" + chpt.chapterIndex + "(" + chpt.title + ")已写入磁盘");
            saveProgress(chpt.chapterIndex);
        }
        private void saveProgress(int chpt)
        {
            var legalName = legalPath.Replace(this.name, "_", -1);
            string savepath = this.savePath + "\\" + legalName + ".jjget";
            FileStream fs = new FileStream(savepath, FileMode.Create, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs);
            sw.Write(chpt.ToString());
            sw.Close();
            fs.Close();
        }
        private void readProgress()
        {
            if (this.name == "" || this.name == null) return;
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

        public void deleteProgress()
        {
            FileInfo file = new FileInfo(this.savePath + "\\" + this.name + ".jjget");
            file.Delete();
        }

        public void readSavedUser()
        {
            try
            {
                FileStream fs = new FileStream(this.savePath + "\\.jjsave", FileMode.Open);
                StreamReader sr = new StreamReader(fs);
                cookiestr = sr.ReadLine();
                userDetail = sr.ReadLine();
                sr.Close();
                fs.Close();
                hasLogin = true;
            }
            catch (Exception)
            {
            }
        }
        public void writeSavedUser()
        {
            if (cookiestr == "")
                return;
            FileStream fs = new FileStream(this.savePath + "\\.jjsave", FileMode.Create, FileAccess.Write);
            StreamWriter sw = new StreamWriter(fs);
            sw.WriteLine(cookiestr);
            sw.WriteLine(userDetail);
            sw.Close();
            fs.Close();
        }
        public void deleteSavedUser()
        {
            FileInfo file = new FileInfo(this.savePath + "\\.jjsave");
            file.Delete();
            cookiestr = "";
            userDetail = "";
            hasLogin = false;
        }

        public FontDecoder getFontDecoder()
        {
            return fontDecoder;
        }

        public void writeDebugInfo(string path)
        {
            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, true))
                {
                    using (var entryStream = archive.CreateEntry("singlePage.html").Open())
                    using (var streamWriter = new StreamWriter(entryStream))
                    {
                        streamWriter.Write(lastParsedHTML);
                    }

                    using (var entryStream = archive.CreateEntry("config.txt").Open())
                    using (var streamWriter = new StreamWriter(entryStream))
                    {
                        streamWriter.WriteLine("novelid: " + novelid);
                        streamWriter.WriteLine("useMobileEdition: " + useMobileEdition);
                        streamWriter.WriteLine("hasLogin: " + hasLogin);
                    }
                }

                using (var fileStream = new FileStream(path, FileMode.Create))
                {
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    memoryStream.CopyTo(fileStream);
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

namespace jjget
{
    using Mapping = Dictionary<string, Dictionary<string, string>>;

    class MappingParseResult
    {
        public int status;
        public Dictionary<string, string> data;
        public string error;
    }

    class FontDecoder
    {

        private HttpUtil hu;
        private Mapping map;
        private Action<string, Color> setProgressDelegate;

        public FontDecoder()
        {
            this.hu = new HttpUtil();
            hu.setEncoding("utf-8");

            this.loadMappings();
        }

        Regex customChar = new Regex(@"&#x([a-f0-9]+)");
        public string Decode(string ct, string fontName)
        {
            if (!map.ContainsKey(fontName))
            {
                setPrompt("下载字体" + fontName + "(已有" + this.map.Count + ")");
                this.loadFont(fontName);
            }
            var lookup = this.map[fontName];
            setPrompt("解码字体" + fontName);
            //&#xeb5d&zwnj;
            return customChar.Replace(ct, m =>
            {
                var r = m.Groups[1].Value.ToLower();
                if(!lookup.ContainsKey(r))
                {
                    throw new FontDecoderException("字体" + fontName + "中的字符" + r + "无法解码");
                }
                return lookup[r];
            });
        }

        private void loadFont(string fontName)
        {
            string ct = null;
            for (int i = 1; i<3; i++)
            {
                try
                {
                    ct = hu.Get("https://jjwxc.yooooo.us/" + fontName + ".json");
                }catch(Exception ex)
                {
                    setPrompt("字体下载失败" + ex.ToString(), Color.Orange);
                }
            }
            if(ct == null)
            {
                throw new FontDecoderException("无法下载字体" + fontName);
            }
            MappingParseResult r = JsonConvert.DeserializeObject<MappingParseResult>(ct);
            if (r.status != 0)
            {
                throw new FontDecoderException("无法解析字体" + fontName + ": " + ct);
            }
            this.map[fontName] = r.data;

            this.saveMappings();
        }

        private void loadMappings()
        {
            try
            {
                FileStream fs = new FileStream(".jjfont", FileMode.Open);
                using (StreamReader sr = new StreamReader(fs))
                {
                    var s = sr.ReadToEnd();
                    this.map = JsonConvert.DeserializeObject<Mapping>(s);
                }
                fs.Close();
            }
            catch (Exception)
            {
                this.map = new Mapping();
            }
        }
        public void saveMappings()
        {
            FileStream fs = new FileStream(".jjfont", FileMode.Create, FileAccess.Write);
            JsonSerializer serializer = new JsonSerializer();
            using (StreamWriter sw = new StreamWriter(fs))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, this.map);
            }
            fs.Close();
        }
        public void registerSetProgressDelegate(Action<String, Color> d)
        {
            this.setProgressDelegate = d;
        }
        private void setPrompt(string text)
        {
            setProgressDelegate(text, Color.Green);
        }

        private void setPrompt(string text, Color c)
        {
            setProgressDelegate("FD:" + text, c);
        }
    }

    public class FontDecoderException : Exception
    {
        public FontDecoderException() : base() { }
        public FontDecoderException(string s) : base(s) { }
    }
}

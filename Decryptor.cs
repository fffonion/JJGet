using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace jjget
{

    class DecryptorGetKeyResult
    {
        public string key;
        public string iv;
        public string err;
    }

    class Decryptor
    {

        private HttpUtil hu;
        private Action<string, Color> setProgressDelegate;

        public Decryptor()
        {
            this.hu = new HttpUtil();
            hu.setEncoding("utf-8");
        }
        public string Decrypt(string ct, Dictionary<string, string> context)
        {
            var r = calculateKey(context);

            var ctb = Convert.FromBase64String(ct);

            DESCryptoServiceProvider des = new DESCryptoServiceProvider();
           
            des.Key = ASCIIEncoding.ASCII.GetBytes(r.key);
            des.IV = ASCIIEncoding.ASCII.GetBytes(r.iv);
            MemoryStream ms = new MemoryStream();
            CryptoStream cs = new CryptoStream(ms, des.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(ctb, 0, ctb.Length);
            cs.FlushFinalBlock();
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        private DecryptorGetKeyResult calculateKey(Dictionary<string, string> context)
        {
            string query = "";
            foreach (KeyValuePair<string, string> entry in context)
            {
                query = query + entry.Key + "=" + entry.Value + "&";
            }
            string ct = null;
            for (int i = 1; i<3; i++)
            {
                try
                {
                    ct = hu.Get("https://jjwxc.yooooo.us/get_key?" + query + "version=" + Application.ProductVersion);
                    break;
                }catch(Exception ex)
                {
                    setPrompt("请求解密服务失败" + ex.ToString(), Color.Orange);
                }
            }
            if(ct == null)
            {
                throw new DecryptorException("无法请求解密服务");
            }
            DecryptorGetKeyResult r = JsonConvert.DeserializeObject<DecryptorGetKeyResult>(ct);
            if (r.err != null)
            {
                throw new DecryptorException("无法获取解密key" + ": " + r.err);
            }

            return r;
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
            setProgressDelegate("DE:" + text, c);
        }
    }

    public class DecryptorException : Exception
    {
        public DecryptorException() : base() { }
        public DecryptorException(string s) : base(s) { }
    }
}

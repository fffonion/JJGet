using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.IO.Compression;
namespace jjget
{
    class HttpUtil
    {
        string encoding = "utf-8";
        WebProxy proxy = null;
        public string cookiestr = "";

        public void setProxy(WebProxy proxy)
        {
            this.proxy = proxy;
        }

        public void setEncoding(string encoding)
        {
            this.encoding = encoding;
        }

        private Stream getStream(HttpWebResponse resp, int timeout)
        {
            Stream stream;
            switch (resp.ContentEncoding.ToUpperInvariant())
            {
                case "GZIP":
                    stream = new GZipStream(resp.GetResponseStream(), CompressionMode.Decompress);
                    break;
                case "DEFLATE":
                    stream = new DeflateStream(resp.GetResponseStream(), CompressionMode.Decompress);
                    break;

                default:
                    stream = resp.GetResponseStream();
                    stream.ReadTimeout = timeout;
                    break;
            }
            return stream;
        }

        private Stream get(string url, string referer, string accept, bool ignoreSetCookie)
        {
            HttpWebRequest request;
            Encoding encoding = Encoding.Default;
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
            request = (HttpWebRequest)HttpWebRequest.Create(url);
            request.KeepAlive = true;
            request.Method = "get";
            request.Timeout = 45000; // 45s
            //request.CookieContainer = new CookieContainer();
            if (proxy != null)
                request.Proxy = proxy;
            request.Headers.Add("Accept-encoding:gzip,deflate");
            if (cookiestr != "")
                request.Headers.Add("Cookie:" + cookiestr);
            if (referer != null)
                request.Referer = referer;
            if (accept == null)
                accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            request.Accept = accept;
            request.UserAgent = "Mozilla/5.0 (Linux; U; Android 4.4.2; zh-CN; JJGET) AppleWebKit/533.1 (KHTML, like Gecko) Version/4.0 UCBrowser/9.7.5.418 U3/0.8.0 Mobile Safari/533.1";
            HttpWebResponse resp = (HttpWebResponse)request.GetResponse();
            string set_cookie = resp.Headers["set-cookie"];
            if (set_cookie != null && set_cookie.IndexOf(",") != -1 && !ignoreSetCookie)
            {
                cookiestr = "testcookie=yes;";
                foreach (string l in set_cookie.Split(','))
                {
                    if (l.Split(';')[0].IndexOf("=") == -1 || l.Split(';')[0].IndexOf("deleted") != -1) continue;
                    cookiestr += l.Split(';')[0] + ";";
                }
            }
            return getStream(resp, 10000);
        }

        public string Get(string url)
        {
            return Get(url, null, false);
        }

        public string Get(string url, string referer)
        {
            return Get(url, referer, false);
        }
        public string Get(string url, string referer, bool ignoreSetCookie) {
            Stream respStream = get(url, referer, null, ignoreSetCookie);
            using (System.IO.StreamReader reader = new System.IO.StreamReader(respStream, Encoding.GetEncoding(this.encoding)))
            {
                return reader.ReadToEnd();
            }
        }

        public byte[] GetBinary(string url, string accept)
        {
            Stream respStream = get(url, null, accept, false);
            try
            {
                int bytesBuffer = 1024;
                byte[] buffer = new byte[bytesBuffer];
                using (MemoryStream ms = new MemoryStream())
                {
                    int readBytes;
                    while ((readBytes = respStream.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        ms.Write(buffer, 0, readBytes);
                    }
                    return ms.ToArray();
                }
            }
            catch (Exception)
            {
                return new byte[0];
            }
        }
        public static int getTimeStamp(){
            return (int)(DateTime.Now - TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1))).TotalSeconds;
        }
    }
}

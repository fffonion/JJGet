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
        string encoding = "utf8";
        WebProxy proxy = null;

        public void setProxy(WebProxy proxy)
        {
            this.proxy = proxy;
        }

        public void setEncoding(string encoding)
        {
            this.encoding = encoding;
        }

        public string Get(string url)
        {
            return Get(url, null);
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
        public string Get(string url, string referer)
        {
            HttpWebRequest request;
            Encoding encoding = Encoding.Default;
            request = (HttpWebRequest)HttpWebRequest.Create(url);
            request.KeepAlive = true;
            request.Method = "get";
            request.Timeout = 12345;
            if (proxy!=null)
                request.Proxy = proxy;
            request.Headers.Add("Accept-encoding:gzip,deflate");
            if (referer != null)
                request.Referer = referer;
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            request.UserAgent = "Mozilla/5.0 (Linux; U; Android 4.4.2; zh-CN; JJGET) AppleWebKit/533.1 (KHTML, like Gecko) Version/4.0 UCBrowser/9.7.5.418 U3/0.8.0 Mobile Safari/533.1";
            HttpWebResponse resp = (HttpWebResponse)request.GetResponse();
            Stream respStream = getStream(resp, 10000);
            using (System.IO.StreamReader reader = new System.IO.StreamReader(respStream, Encoding.GetEncoding(this.encoding)))
            {
                return reader.ReadToEnd();
            }
        }
    }
}

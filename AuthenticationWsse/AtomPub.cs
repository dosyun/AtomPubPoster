using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace AuthenticationWsse
{
    class AtomPub
    {
        readonly string Location = "http://livedoor.blogcms.jp/atom/blog/{0}/article";
        
        readonly string Username = "user";     // ユーザ名
        readonly string Password = "pass";     // APIキー(NOT password)

        readonly string Atom = "http://www.w3.org/2005/Atom";
        readonly string App = "http://www.w3.org/2007/app"; 

        public void Post(string title, string content)
        {
            string uri = Location;
            string header = CreateWsseHeader();
            var xdoc = new XmlDocument();

            var xdeclaration = xdoc.CreateXmlDeclaration("1.0", "utf-8", null);
            xdoc.AppendChild(xdeclaration);

            var xentry = xdoc.CreateElement("entry");
            xentry.SetAttribute("xmlns", Atom);
            xentry.SetAttribute("xmlns:app", App);
            xdoc.AppendChild(xentry);

            // Entry要素内
            var xtitle = xdoc.CreateElement("title");
            xentry.AppendChild(xtitle);
            xtitle.InnerText = title;     // 記事タイトル

            
            var xauthor = xdoc.CreateElement("author");
            xentry.AppendChild(xauthor);
            var xname = xdoc.CreateElement("name");
            xauthor.AppendChild(xname);
            xname.InnerText = Username;   // はてなID
            

            var xcontent = xdoc.CreateElement("content");
            xentry.AppendChild(xcontent);
            xcontent.SetAttribute("type", "text/plain");
            xcontent.InnerText = content; // エントリー本文

            var xupdated = xdoc.CreateElement("updated");
            xentry.AppendChild(xupdated);
            var dt = DateTime.Now;       // 現在時刻
            xupdated.InnerText = dt.ToString("s"); // YYYY-MM-DDTHH:MM:SSの形式
            var xcategory = xdoc.CreateElement("category");
            xentry.AppendChild(xcategory);
            xcategory.SetAttribute("term", "カテゴリー");
            var xappcontrol = xdoc.CreateElement("app:control", App);
            xentry.AppendChild(xappcontrol);
            var xdraft = xdoc.CreateElement("app:draft", App);
            xappcontrol.AppendChild(xdraft);
            xdraft.InnerText = "no";
#if DEBUG
            xdoc.Save("test.xml");
#endif
            try
            {
                var req = WebRequest.Create(uri);
                req.Method = "POST";
                req.Headers.Add("X-WSSE", header);
                req.ContentType = "application/x.atom+xml";
                System.Net.ServicePointManager.Expect100Continue = false;

                Stream requestream = req.GetRequestStream();
#if DEBUG
                Console.WriteLine(xdoc.ToStringXml());
#endif
                byte[] data = Encoding.UTF8.GetBytes(xdoc.ToStringXml());

                requestream.Write(data, 0, data.Length);
                requestream.Close();

	            var response = req.GetResponse();
            }
            catch(WebException ex)
            {
                if (ex.Status == WebExceptionStatus.ProtocolError)
                {
                    // HttpWebResponseを取得
                    var error = (HttpWebResponse)ex.Response;
                    // 応答したURIを表示する
                    Console.WriteLine(error.ResponseUri);
                    // 応答ステータスコードを表示する
                    Console.WriteLine("{0} {1}: {2}", (int)error.StatusCode, error.StatusCode, error.StatusDescription);
                    error.Close();
                }
                else
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        
        public string CreateWsseHeader()
        {
            // HTTPリクエスト毎に生成するセキュリティ・トークン（ランダム文字列）
            var rnd = new RNGCryptoServiceProvider();
            // nonceの生成 Base64にする
            byte[] nonce = new byte[40];
            rnd.GetBytes(nonce);
            string nonce64 = Convert.ToBase64String(nonce);

            // digestの生成
            byte[] digest;
            var created = new DateTime(DateTime.Now.Ticks);
            // SHA1化する
            using (var sha1 = new SHA1Managed())
            {
                string digestText = nonce64 + created.ToString("s") + Password;
                // バイト列に変換
                byte[] digestBytes = Encoding.UTF8.GetBytes(digestText);
                digest = sha1.ComputeHash(digestBytes);
            }
            // Base64化
            string digest64 = Convert.ToBase64String(digest);

            string createdString = created.ToString("s");
            string header = string.Format(
                @"UsernameToken Username=""{0}"", PasswordDigest=""{1}"", Nonce=""{2}"", Created=""{3}""",
                Username, digest64, nonce64, createdString);
#if DEBUG
            Console.WriteLine(header);
#endif
            return header;

        }

    }
    
    static class XmlEx
    {
        public static string ToStringXml(this XmlDocument doc)
        {
            StringWriterUTF8 writer = new StringWriterUTF8();
            doc.Save(writer);
            string r = writer.ToString();
            writer.Close();
            return r;
        }

        private class StringWriterUTF8 : StringWriter
        {
            public override System.Text.Encoding Encoding
            {
                get { return System.Text.Encoding.UTF8; }
            }
        }
	  
    
    }
}

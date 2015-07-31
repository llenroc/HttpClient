namespace Yamool.Http.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.IO;
    using System.Text;
    using NUnit.Framework;
    using Yamool.Net.Http;

    [TestFixture]
    public class HttpRequestTests
    {
        [Test]
        public async void TestHttps()
        {
            var request = new HttpRequest(new Uri("https://www.bing.com"));
            var response = await request.GetResponseAsync().ConfigureAwait(false);
            Assert.AreEqual(200, (int)response.StatusCode);
        }

        [Test]
        public async void TestHttp2Https()
        {            
            var request = new HttpRequest(new Uri("http://www.github.com"));            
            var response = await request.GetResponseAsync().ConfigureAwait(false);
            Assert.AreEqual(200, (int)response.StatusCode);
            Assert.AreEqual("https", response.ResponseUri.Scheme);
            Assert.AreEqual("https://github.com/", response.ResponseUri.AbsoluteUri);
        }

        [Test]
        public async void TesDisableRedirect()
        {
            var request = new HttpRequest(new Uri("http://www.bing.com"))
            {
                AllowAutoRedirect = false
            };
            var response = await request.GetResponseAsync().ConfigureAwait(false);
            Assert.AreEqual("http://www.bing.com/", response.ResponseUri.AbsoluteUri);
            Assert.AreEqual(301, (int)response.StatusCode);
            response.Close();
        }

        [Test]
        public async void TestProxy()
        {
            var proxy = new System.Net.WebProxy("127.0.0.1", 8580);
            var request = new HttpRequest(new Uri("http://www.google.com"))
            {
                Proxy = proxy
            };
            var response = await request.GetResponseAsync().ConfigureAwait(false);
            Assert.AreEqual(200, (int)response.StatusCode);
        }

        [Test]
        public async void TestProxyWithRedirect()
        {
            var proxy = new System.Net.WebProxy("127.0.0.1", 8580);
            proxy.BypassList = new string[] { "http://www.bing.com" };

            //use a proxy 
            var request = new HttpRequest(new Uri("http://www.google.com"))
            {               
                Proxy = proxy
            };
            var response = await request.GetResponseAsync().ConfigureAwait(false);
            Assert.AreEqual(200, (int)response.StatusCode);
            Assert.AreEqual("www.google.com", response.ResponseUri.Host);
            response.Close();

            //disable a proxy
           request = new HttpRequest(new Uri("http://www.bing.com"))
            {
                Proxy = proxy                
            };          
           response = await request.GetResponseAsync().ConfigureAwait(false);
            Assert.AreEqual(200, (int)response.StatusCode);
            Assert.AreEqual("cn.bing.com", response.ResponseUri.Host);
            response.Close();
        }

        [Test]
        public async void TestSmallBuffer()
        {
            var request = new HttpRequest(new Uri("http://cn.bing.com"));
            request.BuffSize = 15;
            var response = await request.GetResponseAsync().ConfigureAwait(false);
            Assert.AreEqual(200, (int)response.StatusCode);
        }

        [Test]
        public async void TestAutoDecompression()
        {
            var request = new HttpRequest(new Uri("http://www.bing.com"))
            {
                AutomaticDecompression = true
            };
            request.Headers.AcceptEncoding = "gzip,deflate";            
            var response = await request.GetResponseAsync().ConfigureAwait(false);
            Assert.IsNotNull(response.Headers.ContentEncoding);
            using (var sr = new StreamReader(response.GetResponseStream(), System.Text.Encoding.UTF8))
            {
                var html = sr.ReadToEnd();
                Console.WriteLine(html);
                sr.Close();
            }
            response.Close();
        }

        [Test]
        public async void TestGetString()
        {
            var request = new HttpRequest(new Uri("http://cn.bing.com"));
            var html = await request.GetStringAsync().ConfigureAwait(false);
            Assert.IsNotNull(html);
        }

        [Test]
        public async void TestPost()
        {
            var request = new HttpRequest(new Uri("http://api.jquery.com/"));
            var bytes = Encoding.UTF8.GetBytes("s=ajax");//search ajax keywords
            var response = await request.PostAsync(bytes).ConfigureAwait(false);
            Assert.AreEqual(200, (int)response.StatusCode);
        }
    }
}

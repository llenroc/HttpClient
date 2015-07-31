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
    public class CommonMethodsTests
    {
        [Test]
        public async void TestGET()
        {
            var request = new HttpRequest(new Uri("http://www.bing.com"));
            var response = await request.GetResponseAsync().ConfigureAwait(false);
            Assert.AreEqual("http://cn.bing.com/", response.ResponseUri.AbsoluteUri);
            response.Close();
        }       

        [Test]
        public async void TestHEAD()
        {
            var request = new HttpRequest(new Uri("http://cn.bing.com"))
            {
                Method = HttpMethod.Head
            };
            var response = await request.GetResponseAsync().ConfigureAwait(false);
            Assert.AreEqual("http://cn.bing.com/", response.ResponseUri.AbsoluteUri);
            Assert.IsNull(response.GetResponseStream());
            response.Close();
        }

        [Test]
        public async void TestPOST()
        {
            var request = new HttpRequest(new Uri("http://api.jquery.com/"))
            {
                Method = HttpMethod.Post
            };
            var post_stream = request.GetRequestStream();
            request.Headers.ContentType = "application/x-www-form-urlencoded";            
            var bytes = Encoding.UTF8.GetBytes("s=ajax");//search ajax keywords
            post_stream.Write(bytes, 0, bytes.Length);
            post_stream.Close();
            var response = await request.GetResponseAsync().ConfigureAwait(false);
            Assert.AreEqual(200,(int) response.StatusCode);
            using (var sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
            {               
                var index = sr.ReadToEnd().IndexOf("<span>ajax</span>");
                Assert.IsTrue(index > 0);
            }
            response.Close();
        }

        [Test]
        public async void TestOptions()
        {
            var request = new HttpRequest(new Uri("http://www.bing.com"))
            {
                Method = HttpMethod.Options
            };
            var response = await request.GetResponseAsync().ConfigureAwait(false);
            Assert.AreEqual(405, (int)response.StatusCode);//405 Method Not Allowed
            Assert.AreEqual("GET,HEAD,TRACE", response.Headers["Allow"]);
            response.Close();
        }
    }
}

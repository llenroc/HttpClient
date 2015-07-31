namespace Yamool.Http.Tests
{
    using System;
    using System.Collections.Generic;
    using NUnit.Framework;
    using Yamool.Net.Http;

    [TestFixture]
    public class HttpHeadersTests
    {
        [Test]
        public void TestHeader()
        {
            var headers = new HttpHeadersFake();
            headers.WithHeader("name", "value").WithHeader("name2", "value2");
            headers.Set("name3", "3");
            headers.Set("name3", "4");
            Assert.AreEqual("value", headers["name"]);
            Assert.AreEqual("4", headers["name3"]);
        }

        [Test]
        public void TestAddMultiValuesHeader()
        {
            var headers = new HttpHeadersFake();
            headers.Add("name", "1");
            headers.Add("name", "2");
            Assert.AreEqual("1,2", headers["name"]);
        }
    }

    public class HttpHeadersFake : HttpHeaders
    {

    }
}

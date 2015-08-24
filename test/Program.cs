using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading.Tasks;
using System.Net;

namespace Yamool.Net.Http.Tests
{
    static class Program
    {
        static void Main()
        {
            Task.Factory.StartNew(() => Test());       
            Console.ReadLine();
        }

        private static async void Test()
        {
            var watcher = new System.Diagnostics.Stopwatch();
            watcher.Start();
            var request = new HttpRequest(new Uri("http://www.baidu.com/"));
            request.KeepAlive = true;
            request.CookieContainer = new CookieContainer();
            var response = await request.SendAsync();
            var sum = 0;
            using (var stream = response.GetResponseStream())
            {
                var buffer = new byte[4096];
                var count = 0;
                while ((count =await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    sum += count;
                    //Console.WriteLine(count);
                }
            }
            watcher.Stop();
            Console.WriteLine("#1 total " + sum + " bytes");

            Console.WriteLine("elapsed[1]:" + watcher.Elapsed.TotalMilliseconds + "'ms");

            Test2();
        }

        private static async void Test2()
        {
            var watcher = new System.Diagnostics.Stopwatch();
            watcher.Start();
            var request = new HttpRequest(new Uri("http://www.baidu.com/"));
            request.KeepAlive = true;
            request.CookieContainer = new CookieContainer();
            using (var response = await request.SendAsync())
            {

                var sum = 0;
                using (var stream = response.GetResponseStream())
                {
                    var buffer = new byte[4096];
                    var count = 0;
                    while ((count = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        sum += count;
                        // Console.WriteLine(count);
                    }
                }
                Console.WriteLine("#2 total " + sum + " bytes");
            }
            watcher.Stop();
            Console.WriteLine("elapsed[1]:" + watcher.Elapsed.TotalMilliseconds + "'ms");
        }
    }
}

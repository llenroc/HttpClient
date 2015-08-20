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
            //Task.Factory.StartNew(() => Test2());          
            Console.ReadLine();
        }

        private static async void Test()
        {
            var watcher = new System.Diagnostics.Stopwatch();
            watcher.Start();
            var request = new HttpRequest(new Uri("http://music.163.com/"));
            request.CookieContainer = new CookieContainer();
            var response = await request.SendAsync();
            var sum = 0;
            using (var stream = response.GetResponseStream())
            {
                var buffer = new byte[2048];
                var count = 0;
                while ((count = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                {
                    sum += count;
                    Console.WriteLine(count);
                }
            }
            watcher.Stop();
            Console.WriteLine("#1 total " + sum + " bytes");

            Console.WriteLine("elapsed[1]:" + watcher.Elapsed.TotalMilliseconds + "'ms");
        }

        private static async void Test2()
        {
            var watcher = new System.Diagnostics.Stopwatch();
            watcher.Start();
            var request = (HttpWebRequest)WebRequest.Create("http://stackoverflow.com/questions/9343594/how-to-call-asynchronous-method-from-synchronous-method-in-c");
            request.CookieContainer = new CookieContainer();

            var response = request.GetResponse();
            var sum = 0;
            using (var stream = response.GetResponseStream())
            {
                var buffer = new byte[4096];
                var count = 0;
                while ((count = await stream.ReadAsync(buffer, 0, buffer.Length).ConfigureAwait(false)) > 0)
                {
                    sum += count;
                }
            }
            watcher.Stop();
            Console.WriteLine("#2 total " + sum + " bytes");

            Console.WriteLine("elapsed[2]:" + watcher.Elapsed.TotalMilliseconds + "'ms");
        }
    }
}

//----------------------------------------------------------------
// Copyright (c) Yamool Inc.  All rights reserved.
//----------------------------------------------------------------

namespace Yamool.Net.Http
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides extension methods for the <see cref="HttpRequest"/> class.
    /// </summary>
    public static class HttpRequestExtensions
    {        
        /// <summary>
        /// Sends a GET request to the specified Uri and returns the response body as a string in an asynchronous operation.
        /// </summary>      
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <remarks>
        /// Return <c>Null</c> if the status code of response is not OK(200-299).
        /// </remarks>
        public static async Task<string> GetStringAsync(this HttpRequest request)
        {
            //reset a http method
            request.Method = HttpMethod.Get;
            var response = await request.GetResponseAsync();
            if (response == null || !response.IsSuccessStatusCode)
            {
                if (response != null)
                {
                    response.Close();
                }
                return null;
            }
            var encoding = Encoding.GetEncoding(response.Headers.Charset);
            using (var sr = new StreamReader(new ResponseLeaveStream(response, response.GetResponseStream()), encoding))
            {
                return await sr.ReadToEndAsync();
            }
        }

        /// <summary>
        /// Sends a POST request to the specified Uri as an asynchronous operation.
        /// </summary>
        /// <param name="postData">The byte array that post to remote host.</param>
        /// <returns>The task object representing the asynchronous operation.</returns>
        public static async Task<HttpResponse> PostAsync(this HttpRequest request, byte[] postData)
        {
            request.Method = HttpMethod.Post;
            request.Headers.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = postData.LongLength;
            var postStream = request.GetRequestStream();
            postStream.Write(postData, 0, postData.Length);
            return await request.GetResponseAsync();
        }

        /// <summary>
        /// Sends a GET request to the specified Uri and returns the response body as a stream in an asynchronous operation.
        /// </summary>
        /// <returns>The task object representing the asynchronous operation.</returns>
        /// <remarks>
        /// When close a stream and also will auto disposing a <see cref="HttpResponse"/> object.
        /// </remarks>
        public static async Task<Stream> GetStreamAsync(this HttpRequest request)
        {
            request.Method = HttpMethod.Get;
            var response = await request.GetResponseAsync();
            if (response == null || !response.IsSuccessStatusCode)
            {
                if (response != null)
                {
                    response.Close();
                }
                return null;
            }
            return new ResponseLeaveStream(response, response.GetResponseStream());
        }
    }
}

// The MIT License (MIT)

// Copyright (c) 2016 Ben Abelshausen

// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace Orion
{
    /// <summary>
    /// Contains code to download test files.
    /// </summary>
    public static class Download
    {
        /// <summary>
        /// Downloads a file if it doesn't exist yet.
        /// </summary>
        public static async Task ToFile(string url, string filename)
        {
            if (!File.Exists(filename))
            {
                var client = new HttpClient();
                using (var stream = await client.GetStreamAsync(url))
                using (var outputStream = File.OpenWrite(filename))
                {
                    stream.CopyTo(outputStream);
                    stream.Flush();
                }
            }
        }

        public static async Task GetAttachment(string url, string filename)
        {
            UriBuilder uriBuilder = new UriBuilder(url);

            using (HttpClient client = new HttpClient())
            {
                client.BaseAddress = new Uri(uriBuilder.ToString());
                client.DefaultRequestHeaders.Accept.Clear();
                HttpResponseMessage response = await client.GetAsync(uriBuilder.ToString());

                if (response.IsSuccessStatusCode)
                {
                    System.Net.Http.HttpContent content = response.Content;
                    var contentStream = await content.ReadAsStreamAsync(); // get the actual content stream
                    using (var outputStream = File.OpenWrite(filename))
                    {
                        contentStream.CopyTo(outputStream);
                        contentStream.Flush();
                    }
                }
                else
                {
                    throw new FileNotFoundException();
                }
            }
        }
    }
}

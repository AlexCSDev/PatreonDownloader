using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;

namespace PatreonDownloader
{
    class FileDownloader
    {
        private HttpClient _httpClient;

        public FileDownloader(CookieContainer cookieContainer)
        {
            var handler = new HttpClientHandler();
            handler.UseCookies = true;
            handler.CookieContainer = cookieContainer;
            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/77.0.3865.90 Safari/537.36");
        }

        public async Task DownloadFile(string url, string path)
        {
            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    using (Stream contentStream =
                            await (await _httpClient.SendAsync(request)).Content.ReadAsStreamAsync(),
                        stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                    {
                        await contentStream.CopyToAsync(stream);
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                Log.Instance.Error($"HttpRequestException while downloading file ({url}): {ex}");
            }
        }
    }
}

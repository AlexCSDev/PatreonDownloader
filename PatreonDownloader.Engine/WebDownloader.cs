using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using NLog;
using PatreonDownloader.Engine.Exceptions;
using PatreonDownloader.Engine.Models;

namespace PatreonDownloader.Engine
{
    //TODO: Make disposable?
    internal sealed class WebDownloader : IWebDownloader
    {
        private readonly HttpClient _httpClient;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public WebDownloader(CookieContainer cookieContainer)
        {
            var handler = new HttpClientHandler();
            handler.UseCookies = true;
            handler.CookieContainer = cookieContainer ?? throw new ArgumentNullException(nameof(cookieContainer));
            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/77.0.3865.90 Safari/537.36");
        }

        public async Task DownloadFile(string url, string path)
        {
            if (File.Exists(path))
            {
                throw new DownloadException($"File {path} already exists");
            }

            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            {
                using (Stream contentStream =
                        await (await _httpClient.SendAsync(request)).Content.ReadAsStreamAsync(),
                    stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                {
                    _logger.Debug($"Starting download: {url}");
                    await contentStream.CopyToAsync(stream);
                    _logger.Debug($"Finished download: {url}");
                }
            }
        }


        public async Task<string> DownloadString(string url)
        {
            return await _httpClient.GetStringAsync(url);
        }
    }
}

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Web;
using NLog;
using PatreonDownloader.Engine.Exceptions;
using PatreonDownloader.Engine.Models;
using PatreonDownloader.PuppeteerEngine;
using PatreonDownloader.PuppeteerEngine.Wrappers.Browser;

namespace PatreonDownloader.Engine
{
    //TODO: Make disposable?
    internal sealed class WebDownloader : IWebDownloader
    {
        private readonly HttpClient _httpClient;
        private readonly IPuppeteerEngine _puppeteerEngine;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public WebDownloader(CookieContainer cookieContainer, IPuppeteerEngine puppeteerEngine)
        {
            _puppeteerEngine = puppeteerEngine ?? throw new ArgumentNullException(nameof(puppeteerEngine));

            var handler = new HttpClientHandler();
            handler.UseCookies = true;
            handler.CookieContainer = cookieContainer ?? throw new ArgumentNullException(nameof(cookieContainer));
            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/46.0.2486.0 Safari/537.36 Edge/13.10586");
        }

        /// <summary>
        /// Download file
        /// </summary>
        /// <param name="url">File url</param>
        /// <param name="path">Path where the file should be saved</param>
        public async Task DownloadFile(string url, string path, bool overwrite = false)
        {
            if(string.IsNullOrEmpty(url))
                throw new ArgumentException("Argument cannot be null or empty", nameof(url));
            if(string.IsNullOrEmpty(path))
                throw new ArgumentException("Argument cannot be null or empty", nameof(path));

            if (File.Exists(path))
            {
                if(!overwrite)
                    throw new DownloadException($"File {path} already exists");

                _logger.Warn($"File {path} already exists, will be overwriten!");
            }

            try
            {
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
            catch (Exception ex)
            {
                throw new DownloadException($"Unable to download from {url}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Download url as string data
        /// </summary>
        /// <param name="url">Url to download</param>
        /// <param name="referer">Referer url</param>
        /// <returns>String</returns>
        public async Task<string> DownloadString(string url)
        {
            if(string.IsNullOrEmpty(url))
                throw new ArgumentException("Argument cannot be null or empty", nameof(url));
            try
            {
                IWebBrowser browser = await _puppeteerEngine.GetBrowser();
                IWebPage page = await browser.NewPageAsync();
                await page.GoToAsync(url);

                string content = await page.GetContentAsync();
                await page.CloseAsync();

                content = content
                    .Replace("<html><head></head><body><pre style=\"word-wrap: break-word; white-space: pre-wrap;\">", "")
                    .Replace("</pre></body></html>", "");
                return HttpUtility.HtmlDecode(content);
            }
            catch (Exception ex)
            {
                throw new DownloadException($"Unable to download from {url}: {ex.Message}", ex);
            }
        }
    }
}

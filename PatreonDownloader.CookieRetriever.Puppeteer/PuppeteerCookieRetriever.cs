using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using NLog;
using PatreonDownloader.Common.Interfaces;
using PatreonDownloader.Interfaces;
using PatreonDownloader.PuppeteerCookieRetriever.Wrappers.Browser;
using PuppeteerSharp;

namespace PatreonDownloader.PuppeteerCookieRetriever
{
    public class PuppeteerCookieRetriever : ICookieRetriever, IDisposable
    {
        private Browser _browser;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public async Task<CookieContainer> RetrieveCookies(string url)
        {
            try
            {
                _logger.Debug("Downloading browser");
                await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
                _logger.Debug("Launching browser");
                _browser = await PuppeteerSharp.Puppeteer.LaunchAsync(new LaunchOptions
                {
                    Devtools = true,
                    UserDataDir = Path.Combine(Environment.CurrentDirectory, "chromedata")
                });

                _logger.Debug("Opening new page");
                Page descriptionPage = await _browser.NewPageAsync();
                await descriptionPage.SetContentAsync("<h1>This is a browser of patreon downloader</h1>");

                _logger.Debug("Creating IWebBrowser");
                IWebBrowser browserWrapper = new WebBrowser(_browser);

                _logger.Debug("Initializing cookie retriever");
                ICookieRetriever cookieRetriever = new InternalCookieRetriever(browserWrapper);

                _logger.Debug("Retrieving cookies");
                CookieContainer cookieContainer = await cookieRetriever.RetrieveCookies(url);

                _logger.Debug("Closing browser");
                await _browser.CloseAsync();

                return cookieContainer;
            }
            catch (PuppeteerSharp.PuppeteerException ex)
            {
                _logger.Fatal($"Browser communication error. Exception: {ex}");
                return null;
            }
            catch (TimeoutException ex)
            {
                _logger.Fatal($"Internal operation timed out. Exception: {ex}");
                return null;
            }
        }

        public void Dispose()
        {
            _browser?.Dispose();
        }
    }
}

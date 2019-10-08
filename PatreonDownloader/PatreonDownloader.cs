using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NLog;
using PatreonDownloader.Models;
using PatreonDownloader.Wrappers.Browser;
using PuppeteerSharp;

namespace PatreonDownloader
{
    internal sealed class PatreonDownloader : IPatreonDownloader, IDisposable
    {
        private readonly string _url;

        private Browser _browser;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Create a new downloader for specified url
        /// </summary>
        /// <param name="url">Posts page url</param>
        public PatreonDownloader(string url)
        {
            //TODO: Check that url is valid
            _url = url ?? throw new ArgumentNullException(nameof(url));
        }

        private async Task<CookieContainer> RetrieveCookies()
        {
            try
            {
                _logger.Debug("Downloading browser");
                await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
                _logger.Debug("Launching browser");
                _browser = await Puppeteer.LaunchAsync(new LaunchOptions
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
                ICookieRetriever cookieRetriever = new CookieRetriever(browserWrapper);

                _logger.Debug("Retrieving cookies");
                CookieContainer cookieContainer = await cookieRetriever.RetrieveCookies(_url);

                _logger.Debug("Closing browser");
                await _browser.CloseAsync();

                return cookieContainer;
            }
            catch (PuppeteerSharp.PuppeteerException ex)
            {
                _logger.Fatal($"<RetrieveCookies> Browser communication error. Exception: {ex}");
                return null;
            }
            catch (TimeoutException ex)
            {
                _logger.Fatal($"<RetrieveCookies> Internal operation timed out. Exception: {ex}");
                return null;
            }
        }

        public async Task<bool> Download()
        {
            _logger.Debug("Calling RetrieveCookies()");
            CookieContainer cookieContainer = await RetrieveCookies();

            if (cookieContainer == null)
            {
                _logger.Fatal($"Unable to retrieve cookies for {_url}");
                return false;
            }

            _logger.Debug("Initializing file downloader");
            IWebDownloader webDownloader = new WebDownloader(cookieContainer);

            _logger.Debug("Initializing id retriever");
            ICampaignIdRetriever campaignIdRetriever = new CampaignIdRetriever(webDownloader);

            _logger.Debug($"Retrieving campaign ID");
            long campaignId = await campaignIdRetriever.RetrieveCampaignId(_url);

            if (campaignId == -1)
            {
                _logger.Fatal($"Unable to retrieve campaign id for {_url}");
                return false;
            }
            _logger.Info($"Campaign ID: {campaignId}");

            _logger.Debug("Initializing campaign info retriever");
            ICampaignInfoRetriever campaignInfoRetriever = new CampaignInfoRetriever(webDownloader);

            _logger.Debug($"Retrieving campaign info");
            CampaignInfo campaignInfo = await campaignInfoRetriever.RetrieveCampaignInfo(campaignId);
            _logger.Info($"Campaign name: {campaignInfo.Name}");

            _logger.Debug("Initializing page crawler");
            IPageCrawler pageCrawler = new PageCrawler(webDownloader);

            _logger.Debug("Starting crawler");
            await pageCrawler.Crawl(campaignInfo);

            _logger.Debug("Finished downloading");
            return true;
        }

        public void Dispose()
        {
            _browser?.Dispose();
        }
    }
}

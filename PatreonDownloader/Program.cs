using PuppeteerSharp;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;
using NLog;

namespace PatreonDownloader
{
    class Program
    {
        private static Browser _browser;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        static async Task Main(string[] args)
        {
            _logger.Debug("Patreon downloader started");

            if (args.Length == 0)
            {
                _logger.Fatal("creator posts page url is required");
                return;
            }

            _logger.Info($"Creator page: {args[0]}");

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            await RunPatreonDownloader(args[0]);
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            _logger.Debug("Entered process exit");
            if (_browser != null && !_browser.IsClosed)
            {
                _logger.Debug("Closing browser...");
                try
                {
                    _browser.CloseAsync();
                }
                catch (PuppeteerSharp.PuppeteerException ex)
                {
                    _logger.Fatal($"Browser communication error, browser process might be left open! Exception: {ex}");
                }
            }
        }

        private static async Task RunPatreonDownloader(string url)
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

                _logger.Debug("Initializing id retriever");
                CampaignIdRetriever campaignIdRetriever = new CampaignIdRetriever(_browser);

                _logger.Debug("Initializing campaign info retriever");
                CampaignInfoRetriever campaignInfoRetriever = new CampaignInfoRetriever(_browser);

                _logger.Debug("Initializing page crawler");
                PageCrawler pageCrawler = new PageCrawler(_browser);

                PatreonDownloader patreonDownloader = new PatreonDownloader(campaignIdRetriever, campaignInfoRetriever, pageCrawler);

                await patreonDownloader.Download(url);

                await _browser.CloseAsync();
            }
            catch (PuppeteerSharp.PuppeteerException ex)
            {
                _logger.Fatal($"Browser communication error, application will be closed. Exception: {ex}");
                return;
            }
            catch (TimeoutException ex)
            {
                _logger.Fatal($"Internal operation timed out, application will be closed. Exception: {ex}");
                return;
            }

            _logger.Info($"Completed downloading {url}");
        }
    }
}

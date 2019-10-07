using PuppeteerSharp;
using System;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;
using NLog;
using PatreonDownloader.Wrappers.Browser;

//Alow tests to see internal classes
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: InternalsVisibleTo("PatreonDownloader.Tests")]
namespace PatreonDownloader
{
    class Program
    {
        private static Browser _browser;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();

        static async Task Main(string[] args)
        {
            _logger.Debug("Patreon downloader started");

            //TODO: Proper command system
            //TODO: Login command
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

                Page descriptionPage = await _browser.NewPageAsync();
                await descriptionPage.SetContentAsync("<h1>This is a browser of patreon downloader</h1>");

                _logger.Debug("Creating IWebBrowser");
                IWebBrowser browserWrapper = new WebBrowser(_browser);

                _logger.Debug("Initializing cookie retriever");
                ICookieRetriever cookieRetriever = new CookieRetriever(browserWrapper);

                _logger.Debug("Retrieving cookies");
                CookieContainer cookieContainer = await cookieRetriever.RetrieveCookies(url);

                if (cookieContainer == null)
                {
                    _logger.Fatal($"Unable to retrieve cookies for {url}");
                    return;
                }

                _logger.Debug("Initializing id retriever");
                ICampaignIdRetriever campaignIdRetriever = new CampaignIdRetriever(browserWrapper);

                _logger.Debug($"Retrieving campaign ID");
                long campaignId = await campaignIdRetriever.RetrieveCampaignId(url);

                if (campaignId == -1)
                {
                    _logger.Fatal($"Unable to retrieve campaign id for {url}");
                    return;
                }

                await _browser.CloseAsync();

                _logger.Info($"Campaign ID: {campaignId}");

                IPatreonDownloader patreonDownloader = new PatreonDownloader(campaignId, cookieContainer);

                await patreonDownloader.Download(url);
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

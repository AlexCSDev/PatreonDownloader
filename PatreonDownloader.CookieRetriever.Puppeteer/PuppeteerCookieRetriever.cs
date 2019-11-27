using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

        public PuppeteerCookieRetriever()
        {
            KillChromeIfRunning();
        }

        private void KillChromeIfRunning()
        {
            Process[] processList = Process.GetProcessesByName("chrome");
            if (processList.Length > 0)
            {
                _logger.Debug($"Found {processList.Length} chrome processes (not sure which one yet)");

                processList = processList.Where(x =>
                        x.MainModule != null && x.MainModule.FileName.Contains(AppDomain.CurrentDomain.BaseDirectory))
                    .ToArray();
                if (processList.Length > 0)
                {
                    _logger.Debug($"{processList.Length} chrome processes are in patreondownloader's folder");
                    _logger.Warn("Running PatreonDownloader's Chrome detected. Attempting to close it...");

                    bool failed = false;
                    foreach (Process process in processList)
                    {
                        _logger.Debug($"Attempting to kill PID {process.Id}");
                        try
                        {
                            process.Kill();
                        }
                        catch (Exception ex)
                        {
                            failed = true;
                            _logger.Error($"Error while closing chrome: {ex}");
                        }
                    }

                    if (failed)
                    {
                        _logger.Error("Unable to close some or all PatreonDownloader's Chrome instances. Please close them manually via process manager if you encounter any problems running this application.");
                    }
                    else
                    {
                        _logger.Info("Successfully killed all PatreonDownloader's Chrome instances.");
                    }
                }
            }
        }

        public async Task<CookieContainer> RetrieveCookies()
        {
            try
            {
                _logger.Debug("Downloading browser");
                await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
                _logger.Debug("Launching browser");
                _browser = await PuppeteerSharp.Puppeteer.LaunchAsync(new LaunchOptions
                {
                    //Devtools = true,
                    Headless = false,
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
                CookieContainer cookieContainer = await cookieRetriever.RetrieveCookies();

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

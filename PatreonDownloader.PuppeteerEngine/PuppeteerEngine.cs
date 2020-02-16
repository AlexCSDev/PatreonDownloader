using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NLog;
using PatreonDownloader.Common.Interfaces;
using PatreonDownloader.PuppeteerEngine.Wrappers.Browser;
using PuppeteerSharp;

namespace PatreonDownloader.PuppeteerEngine
{
    public class PuppeteerEngine : IPuppeteerEngine, IDisposable
    {
        private Browser _browser;
        private IWebBrowser _browserWrapper;

        private readonly bool _headless;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public bool IsHeadless
        {
            get { return _headless; }
        }

        /// <summary>
        /// Create a new instance of PuppeteerEngine
        /// </summary>
        /// <param name="headless">If set to false then the browser window will be visible</param>
        public PuppeteerEngine(bool headless = true)
        {
            _headless = headless;
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
                        _logger.Error(
                            "Unable to close some or all PatreonDownloader's Chrome instances. Please close them manually via process manager if you encounter any problems running this application.");
                    }
                    else
                    {
                        _logger.Info("Successfully killed all PatreonDownloader's Chrome instances.");
                    }
                }
            }
        }

        public async Task<IWebBrowser> GetBrowser()
        {
            if (_browser != null && !_browser.IsClosed)
                return _browserWrapper;

            try
            {
                _logger.Debug("Downloading browser");
                await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
                _logger.Debug("Launching browser");
                _browser = await PuppeteerSharp.Puppeteer.LaunchAsync(new LaunchOptions
                {
                    //Devtools = true,
                    Headless = _headless,
                    UserDataDir = Path.Combine(Environment.CurrentDirectory, "chromedata"),
                    //Headless mode changes user agent so we need to force it to use "real" user agent
                    Args = new []{ "--user-agent=\"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/78.0.3882.0 Safari/537.36\"" }
                });

                _logger.Debug("Opening new page");
                Page descriptionPage = await _browser.NewPageAsync();
                await descriptionPage.SetContentAsync("<h1>This is a browser of patreon downloader</h1>");

                _logger.Debug("Creating IWebBrowser");
                _browserWrapper = new WebBrowser(_browser);

                return _browserWrapper;
            }
            catch (PuppeteerSharp.PuppeteerException ex)
            {
                _logger.Fatal($"Browser communication error. Exception: {ex}");
                return null;
            }
        }

        public async Task CloseBrowser()
        {
            if (_browser != null && !_browser.IsClosed)
            {
                await _browser.CloseAsync();
                _browser.Dispose();
                _browser = null;
            }
        }

        public void Dispose()
        {
            _browser?.Dispose();
        }
    }
}

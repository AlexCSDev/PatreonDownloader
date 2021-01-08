using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NLog;
using PatreonDownloader.Common.Interfaces;
using PatreonDownloader.PuppeteerEngine.Wrappers.Browser;
using PuppeteerSharp;

namespace PatreonDownloader.PuppeteerEngine
{
    public class PuppeteerCookieRetriever : ICookieRetriever, IDisposable
    {
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private IPuppeteerEngine _puppeteerEngine;
        private bool _isHeadlessBrowser;
        private bool _isRemoteBrowser;

        /// <summary>
        /// Create new instance of PuppeteerCookieRetriever using remote browser (THIS CLASS IS NOT A PART OF NINJECT DI)
        /// </summary>
        /// <param name="remoteBrowserAddress">Remote browser address</param>
        public PuppeteerCookieRetriever(Uri remoteBrowserAddress)
        {
            _puppeteerEngine = new PuppeteerEngine(remoteBrowserAddress);
            _isHeadlessBrowser = true;
            _isRemoteBrowser = true;
        }

        /// <summary>
        /// Create new instance of PuppeteerCookieRetriever using internal browser (THIS CLASS IS NOT A PART OF NINJECT DI)
        /// </summary>
        /// <param name="headlessBrowser">If set to false then the internal browser will be visible</param>
        public PuppeteerCookieRetriever(bool headlessBrowser = true)
        {
            _puppeteerEngine = new PuppeteerEngine(headlessBrowser);
            _isHeadlessBrowser = headlessBrowser;
            _isRemoteBrowser = false;
        }

        private async Task<IWebBrowser> RestartBrowser(bool headless)
        {
            await _puppeteerEngine.CloseBrowser();
            await Task.Delay(1000); //safety first

            _puppeteerEngine = new PuppeteerEngine(headless);
            return await _puppeteerEngine.GetBrowser();
        }

        private async Task Login()
        {
            _logger.Debug("Retrieving browser");
            IWebBrowser browser = await _puppeteerEngine.GetBrowser();

            IWebPage page = null;
            bool loggedIn = false;
            do
            {
                if (page == null || page.IsClosed)
                    page = await browser.NewPageAsync();

                _logger.Debug("Checking login status");
                IWebResponse response = await page.GoToAsync("https://www.patreon.com/api/current_user");
                if (response.Status == HttpStatusCode.Unauthorized)
                {
                    _logger.Debug("We are NOT logged in, opening login page");
                    if (_isRemoteBrowser)
                    {
                        await page.CloseAsync();
                        throw new Exception("You are not logged in into your patreon account in remote browser. Please login and restart PatreonDownloader.");
                    }
                    if (_puppeteerEngine.IsHeadless)
                    {
                        _logger.Debug("Puppeteer is in headless mode, restarting in full mode");
                        browser = await RestartBrowser(false);
                        page = await browser.NewPageAsync();
                    }

                    await page.GoToAsync("https://www.patreon.com/login");

                    //todo: use another page? home page loading is pretty slow
                    await page.WaitForRequestAsync(request => request.Url == "https://www.patreon.com/home");
                }
                else
                {
                    _logger.Debug("We are logged in");
                    if (_puppeteerEngine.IsHeadless != _isHeadlessBrowser)
                    {
                        browser = await RestartBrowser(_isHeadlessBrowser);
                        page = await browser.NewPageAsync();
                    }

                    loggedIn = true;
                }
            } while (!loggedIn);

            await page.CloseAsync();
        }

        public async Task<CookieContainer> RetrieveCookies()
        {
            try
            {
                CookieContainer cookieContainer = new CookieContainer();

                _logger.Debug("Calling login check");
                try
                {
                    await Login();
                }
                catch (Exception ex)
                {
                    _logger.Fatal($"Login error: {ex.Message}");
                    return null;
                }

                _logger.Debug("Retrieving browser");
                IWebBrowser browser = await _puppeteerEngine.GetBrowser();

                _logger.Debug("Retrieving cookies");
                IWebPage page = await browser.NewPageAsync();
                await page.GoToAsync("https://www.patreon.com/home");

                CookieParam[] browserCookies = await page.GetCookiesAsync();

                if (browserCookies != null && browserCookies.Length > 0)
                {
                    foreach (CookieParam browserCookie in browserCookies)
                    {
                        _logger.Debug($"Adding cookie: {browserCookie.Name}");
                        Cookie cookie = new Cookie(browserCookie.Name, browserCookie.Value, browserCookie.Path, browserCookie.Domain);
                        cookieContainer.Add(cookie);
                    }
                }
                else
                {
                    _logger.Fatal("No cookies were extracted from browser");
                    return null;
                }

                await page.CloseAsync();

                return cookieContainer;
            }
            catch (TimeoutException ex)
            {
                _logger.Fatal($"Internal operation timed out. Exception: {ex}");
                return null;
            }
        }

        public void Dispose()
        {
            _puppeteerEngine?.Dispose();
        }
    }
}

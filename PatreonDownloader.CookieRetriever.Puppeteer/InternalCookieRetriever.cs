using System;
using System.Net;
using System.Threading.Tasks;
using NLog;
using PatreonDownloader.Common.Interfaces;
using PatreonDownloader.Interfaces;
using PatreonDownloader.PuppeteerCookieRetriever.Wrappers.Browser;
using PuppeteerSharp;

namespace PatreonDownloader.PuppeteerCookieRetriever
{
    /// <summary>
    /// Internal cookie retriever, separated from public to help with unit testing
    /// </summary>
    internal sealed class InternalCookieRetriever : ICookieRetriever
    {
        private readonly IWebBrowser _browser;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public InternalCookieRetriever(IWebBrowser browser)
        {
            _browser = browser ?? throw new ArgumentNullException(nameof(browser));
        }

        private async Task Login()
        {
            IWebPage page = null;
            bool loggedIn = false;
            do
            {
                if(page == null || page.IsClosed)
                    page = await _browser.NewPageAsync();
                _logger.Debug("Checking login status");
                IWebResponse response = await page.GoToAsync("https://www.patreon.com/api/current_user");
                if (response.Status == HttpStatusCode.Unauthorized)
                {
                    _logger.Debug("We are NOT logged in, opening login page");
                    await page.GoToAsync("https://www.patreon.com/login");
                    await page.WaitForRequestAsync(request => request.Url == "https://www.patreon.com/home");
                }
                else
                {
                    _logger.Debug("We are logged in");
                    loggedIn = true;
                }
            } while (!loggedIn);

            await page.CloseAsync();
        }

        /// <summary>
        /// Retrieve cookies for supplied url
        /// </summary>
        /// <param name="url">Url of page to retrieve cookies from</param>
        /// <returns></returns>
        public async Task<CookieContainer> RetrieveCookies(string url)
        {
            CookieContainer cookieContainer = new CookieContainer();

            _logger.Debug("Calling login check");
            await Login();

            IWebPage page = await _browser.NewPageAsync();
            await page.GoToAsync(url);

            CookieParam[] browserCookies = await page.GetCookiesAsync();

            //TODO: Check that all required cookies were extracted
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
    }
}

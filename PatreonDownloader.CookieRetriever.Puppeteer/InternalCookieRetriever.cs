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

        public Task Login()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Retrieve cookies for supplied url
        /// </summary>
        /// <param name="url">Url of page to retrieve cookies from</param>
        /// <returns></returns>
        public async Task<CookieContainer> RetrieveCookies(string url)
        {
            CookieContainer cookieContainer = new CookieContainer();
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

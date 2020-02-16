using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PuppeteerSharp;

namespace PatreonDownloader.PuppeteerEngine.Wrappers.Browser
{
    /// <summary>
    /// This class is a wrapper around a Puppeteer Sharp's page object used to implement proper dependency injection mechanism
    /// It should copy any used puppeteer sharp's method definitions for ease of code maintenance
    /// </summary>
    public sealed class WebPage : IWebPage
    {
        private readonly Page _page;
        private bool _configured;
        public WebPage(Page page)
        {
            _page = page ?? throw new ArgumentNullException(nameof(page));
            _configured = false;
        }

        public bool IsClosed
        {
            get { return _page.IsClosed; }
        }

        public async Task<IWebResponse> GoToAsync(string url, int? timeout = null, WaitUntilNavigation[] waitUntil = null)
        {
            //Headless browser does not supply some headers so we need to configure it to do so in order to not trigger cloudflare's protection
            if (!_configured)
            {
                Dictionary<string, string> headerDictionary = new Dictionary<string, string>();
                headerDictionary.Add("Accept-Language", "en-GB,en-US;q=0.9,en;q=0.8");
                await _page.SetExtraHttpHeadersAsync(headerDictionary);

                _configured = true;
            }

            Response response = await _page.GoToAsync(url, timeout, waitUntil);
            IWebResponse webResponse = new WebResponse(response);
            return webResponse;
        }

        public async Task SetUserAgentAsync(string userAgent)
        {
            await _page.SetUserAgentAsync(userAgent);
        }

        public async Task<string> GetContentAsync()
        {
            return await _page.GetContentAsync();
        }

        public async Task<IWebRequest> WaitForRequestAsync(Func<Request, bool> predicate, WaitForOptions options = null)
        {
            Request request = await _page.WaitForRequestAsync(predicate, options);
            IWebRequest webRequest = new WebRequest(request);
            return webRequest;
        }

        public async Task<CookieParam[]> GetCookiesAsync(params string[] urls)
        {
            return await _page.GetCookiesAsync(urls);
        }

        public async Task CloseAsync(PageCloseOptions options = null)
        {
            await _page.CloseAsync(options);
        }
    }
}

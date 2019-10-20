using System;
using System.Threading.Tasks;
using PuppeteerSharp;

namespace PatreonDownloader.PuppeteerCookieRetriever.Wrappers.Browser
{
    /// <summary>
    /// This class is a wrapper around a Puppeteer Sharp's page object used to implement proper dependency injection mechanism
    /// It should copy any used puppeteer sharp's method definitions for ease of code maintenance
    /// </summary>
    internal sealed class WebPage : IWebPage
    {
        private readonly Page _page;
        public WebPage(Page page)
        {
            _page = page ?? throw new ArgumentNullException(nameof(page));
        }

        public async Task<IWebResponse> GoToAsync(string url, int? timeout = null, WaitUntilNavigation[] waitUntil = null)
        {
            Response response = await _page.GoToAsync(url, timeout, waitUntil);
            IWebResponse webResponse = new WebResponse(response);
            return webResponse;
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

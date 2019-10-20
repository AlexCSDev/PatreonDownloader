using System;
using System.Threading.Tasks;
using PuppeteerSharp;

namespace PatreonDownloader.PuppeteerCookieRetriever.Wrappers.Browser
{
    /// <summary>
    /// This interface is a wrapper around a Puppeteer Sharp's page object used to implement proper dependency injection mechanism
    /// It should copy any used puppeteer sharp's method definitions for ease of code maintenance
    /// </summary>
    internal interface IWebPage
    {
        Task<IWebResponse> GoToAsync(string url, int? timeout = null, WaitUntilNavigation[] waitUntil = null);
        Task<IWebRequest> WaitForRequestAsync(Func<Request, bool> predicate, WaitForOptions options = null);
        Task<CookieParam[]> GetCookiesAsync(params string[] urls);
        Task CloseAsync(PageCloseOptions options = null);
    }
}

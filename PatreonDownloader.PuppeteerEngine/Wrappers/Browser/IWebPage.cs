using System;
using System.Threading.Tasks;
using PuppeteerSharp;

namespace PatreonDownloader.PuppeteerEngine.Wrappers.Browser
{
    /// <summary>
    /// This interface is a wrapper around a Puppeteer Sharp's page object used to implement proper dependency injection mechanism
    /// It should copy any used puppeteer sharp's method definitions for ease of code maintenance
    /// </summary>
    public interface IWebPage
    {
        bool IsClosed { get; }
        Task<IWebResponse> GoToAsync(string url, int? timeout = null, WaitUntilNavigation[] waitUntil = null);
        Task SetUserAgentAsync(string userAgent);
        Task<string> GetContentAsync();
        Task<IWebRequest> WaitForRequestAsync(Func<Request, bool> predicate, WaitForOptions options = null);
        Task<CookieParam[]> GetCookiesAsync(params string[] urls);
        Task CloseAsync(PageCloseOptions options = null);
    }
}

using System.Threading.Tasks;

namespace PatreonDownloader.PuppeteerCookieRetriever.Wrappers.Browser
{
    /// <summary>
    /// This interface is a wrapper around a Puppeteer Sharp's response object used to implement proper dependency injection mechanism
    /// It should copy any used puppeteer sharp's method definitions for ease of code maintenance
    /// </summary>
    internal interface IWebResponse
    {
        Task<string> TextAsync();
    }
}

using System.Threading.Tasks;

namespace PatreonDownloader.PuppeteerEngine.Wrappers.Browser
{
    /// <summary>
    /// This interface is a wrapper around a Puppeteer Sharp's browser object used to implement proper dependency injection mechanism
    /// It should copy any used puppeteer sharp's method definitions for ease of code maintenance
    /// </summary>
    public interface IWebBrowser
    {
        Task<IWebPage> NewPageAsync();
        Task<string> GetUserAgentAsync();
    }
}

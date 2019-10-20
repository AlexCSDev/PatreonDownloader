using System;
using System.Threading.Tasks;
using PuppeteerSharp;

namespace PatreonDownloader.PuppeteerCookieRetriever.Wrappers.Browser
{
    /// <summary>
    /// This class is a wrapper around a Puppeteer Sharp's response object used to implement proper dependency injection mechanism
    /// It should copy any used puppeteer sharp's method definitions for ease of code maintenance
    /// </summary>
    internal sealed class WebResponse : IWebResponse
    {
        private readonly Response _response;
        public WebResponse(Response response)
        {
            _response = response ?? throw new ArgumentNullException(nameof(response));
        }
        public async Task<string> TextAsync()
        {
            return await _response.TextAsync();
        }
    }
}

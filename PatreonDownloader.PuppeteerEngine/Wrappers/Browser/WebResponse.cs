using System;
using System.Net;
using System.Threading.Tasks;
using PuppeteerSharp;

namespace PatreonDownloader.PuppeteerEngine.Wrappers.Browser
{
    /// <summary>
    /// This class is a wrapper around a Puppeteer Sharp's response object used to implement proper dependency injection mechanism
    /// It should copy any used puppeteer sharp's method definitions for ease of code maintenance
    /// </summary>
    public sealed class WebResponse : IWebResponse
    {
        private readonly Response _response;
        public WebResponse(Response response)
        {
            _response = response ?? throw new ArgumentNullException(nameof(response));
        }

        public HttpStatusCode Status
        {
            get { return _response.Status; }
        }
        
        public async Task<string> TextAsync()
        {
            return await _response.TextAsync();
        }
    }
}

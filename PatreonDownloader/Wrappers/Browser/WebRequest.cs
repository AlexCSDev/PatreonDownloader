using System;
using System.Collections.Generic;
using System.Text;
using PuppeteerSharp;

namespace PatreonDownloader.Wrappers.Browser
{
    /// <summary>
    /// This class is a wrapper around a Puppeteer Sharp's request object used to implement proper dependency injection mechanism
    /// It should copy any used puppeteer sharp's method definitions for ease of code maintenance
    /// </summary>
    internal sealed class WebRequest : IWebRequest
    {
        private readonly Request _request;

        public WebRequest(Request request)
        {
            _request = request ?? throw new ArgumentNullException(nameof(request));
        }

        public string Url => _request.Url;
    }
}

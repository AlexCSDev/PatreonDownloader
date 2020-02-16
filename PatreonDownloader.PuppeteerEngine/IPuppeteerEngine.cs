using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using PatreonDownloader.PuppeteerEngine.Wrappers.Browser;

namespace PatreonDownloader.PuppeteerEngine
{
    public interface IPuppeteerEngine
    {
        bool IsHeadless { get; }
        Task<IWebBrowser> GetBrowser();
        Task CloseBrowser();
    }
}

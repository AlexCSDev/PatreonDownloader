using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using PatreonDownloader.Common.Enums;
using PatreonDownloader.Interfaces.Models;

namespace PatreonDownloader.Common.Interfaces.Plugins
{
    public interface IPlugin
    {
        string Name { get; }
        string Author { get; }
        string ContactInformation { get; }

        /// <summary>
        /// Initialization function, called by IPluginManager's BeforeStart() function.
        /// </summary>
        /// <returns></returns>
        /// <param name="overwriteFiles">Specifies if existing files should be overwritten</param>
        /// <returns></returns>
        Task BeforeStart(bool overwriteFiles);
        /// <summary>
        /// Returns true if supplied url is supported by this plugin
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        Task<bool> IsSupportedUrl(string url);
        /// <summary>
        /// Download crawled url
        /// </summary>
        /// <param name="crawledUrl"></param>
        /// <param name="downloadDirectory"></param>
        /// <returns></returns>
        Task Download(CrawledUrl crawledUrl, string downloadDirectory);

        /// <summary>
        /// Extract supported urls from supplied html text
        /// </summary>
        /// <param name="htmlContents"></param>
        /// <returns></returns>
        Task<List<string>> ExtractSupportedUrls(string htmlContents);
    }
}

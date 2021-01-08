using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using PatreonDownloader.Common.Interfaces;
using PatreonDownloader.Common.Interfaces.Plugins;
using PatreonDownloader.Interfaces.Models;

namespace PatreonDownloader.Engine
{
    internal interface IPluginManager
    {
        /// <summary>
        /// Initialization function, called on every PatreonDownloader.Download call
        /// </summary>
        /// <returns></returns>
        Task BeforeStart(PatreonDownloaderSettings settings);

        /// <summary>
        /// Download file using one of the registered plugins (or default if none are found)
        /// </summary>
        /// <param name="crawledUrl"></param>
        /// <param name="downloadDirectory"></param>
        /// <returns></returns>
        Task DownloadCrawledUrl(CrawledUrl crawledUrl, string downloadDirectory);

        /// <summary>
        /// Run entry contents through every plugin to extract supported urls
        /// </summary>
        /// <param name="htmlContents"></param>
        /// <returns></returns>
        Task<List<string>> ExtractSupportedUrls(string htmlContents);
    }
}

using System.Threading.Tasks;
using PatreonDownloader.Interfaces.Models;

namespace PatreonDownloader.Common.Interfaces
{
    public interface IDownloader
    {
        /// <summary>
        /// Initialization function, called by IPluginManager's BeforeStart() function
        /// </summary>
        /// <returns></returns>
        Task BeforeStart();
        Task<bool> IsSupportedUrl(string url);
        Task Download(CrawledUrl crawledUrl, string downloadDirectory);
    }
}

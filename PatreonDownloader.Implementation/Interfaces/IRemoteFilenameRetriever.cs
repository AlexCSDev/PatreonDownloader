using System.Threading.Tasks;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace PatreonDownloader.Implementation.Interfaces
{
    interface IRemoteFilenameRetriever
    {
        /// <summary>
        /// Initialization function, called on every PatreonDownloader.Download call
        /// </summary>
        /// <returns></returns>
        Task BeforeStart(IUniversalDownloaderPlatformSettings settings);
        Task<string> RetrieveRemoteFileName(string url);
    }
}

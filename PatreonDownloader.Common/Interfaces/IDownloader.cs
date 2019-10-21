using System.Threading.Tasks;
using PatreonDownloader.Interfaces.Models;

namespace PatreonDownloader.Common.Interfaces
{
    public interface IDownloader
    {
        Task<bool> IsSupportedUrl(string url);
        Task<bool> Download(CrawledUrl crawledUrl, string downloadDirectory);
    }
}

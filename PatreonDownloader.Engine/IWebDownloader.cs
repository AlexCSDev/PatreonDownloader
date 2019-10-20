using System.Threading.Tasks;
using PatreonDownloader.Engine.Models;

namespace PatreonDownloader.Engine
{
    internal interface IWebDownloader
    {
        Task<DownloadResult> DownloadFile(string url, string path);

        Task<string> DownloadString(string url);
    }
}

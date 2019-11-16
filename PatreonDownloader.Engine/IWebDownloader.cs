using System.Threading.Tasks;
using PatreonDownloader.Engine.Models;

namespace PatreonDownloader.Engine
{
    internal interface IWebDownloader
    {
        Task DownloadFile(string url, string path);

        Task<string> DownloadString(string url);
    }
}

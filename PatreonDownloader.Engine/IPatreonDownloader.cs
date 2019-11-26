using System.Threading.Tasks;

namespace PatreonDownloader.Engine
{
    internal interface IPatreonDownloader
    {
        Task Download(string creatorName, PatreonDownloaderSettings settings);
    }
}

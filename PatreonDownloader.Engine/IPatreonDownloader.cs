using System.Threading.Tasks;

namespace PatreonDownloader.Engine
{
    internal interface IPatreonDownloader
    {
        Task<bool> Download(string creatorName, PatreonDownloaderSettings settings);
    }
}

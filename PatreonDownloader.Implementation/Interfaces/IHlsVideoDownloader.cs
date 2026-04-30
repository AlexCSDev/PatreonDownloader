using System.Threading.Tasks;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace PatreonDownloader.Implementation.Interfaces
{
    internal interface IHlsVideoDownloader
    {
        bool IsFfmpegAvailable { get; }
        Task BeforeStart(IUniversalDownloaderPlatformSettings settings);
        bool IsHlsDownload(string url, string path);
        string GetVideoOutputPath(string path);
        Task<bool> TryDownloadVideo(string url, string path);
    }
}

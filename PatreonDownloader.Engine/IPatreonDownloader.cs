using System;
using System.Threading.Tasks;
using PatreonDownloader.Engine.Events;

namespace PatreonDownloader.Engine
{
    internal interface IPatreonDownloader
    {
        event EventHandler<DownloaderStatusChangedEventArgs> StatusChanged;

        Task Download(string creatorName, PatreonDownloaderSettings settings);
    }
}

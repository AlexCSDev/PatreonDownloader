using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PatreonDownloader.Engine.Events;
using PatreonDownloader.Engine.Models;
using PatreonDownloader.Interfaces.Models;

namespace PatreonDownloader.Engine.Stages.Downloading
{
    internal interface IDownloadManager
    {
        event EventHandler<FileDownloadedEventArgs> FileDownloaded;
        Task Download(List<CrawledUrl> crawledUrls, string downloadDirectory);
    }
}

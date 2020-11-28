using System;
using System.Threading.Tasks;
using PatreonDownloader.Engine.Events;

namespace PatreonDownloader.Engine
{
    internal interface IPatreonDownloader
    {
        event EventHandler<DownloaderStatusChangedEventArgs> StatusChanged;
        event EventHandler<PostCrawlEventArgs> PostCrawlStart;
        event EventHandler<PostCrawlEventArgs> PostCrawlEnd;
        event EventHandler<NewCrawledUrlEventArgs> NewCrawledUrl;
        event EventHandler<CrawlerMessageEventArgs> CrawlerMessage;
        event EventHandler<FileDownloadedEventArgs> FileDownloaded;
        Task Download(string url, PatreonDownloaderSettings settings);
    }
}

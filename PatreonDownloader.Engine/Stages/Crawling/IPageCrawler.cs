using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PatreonDownloader.Engine.Events;
using PatreonDownloader.Engine.Models;
using PatreonDownloader.Interfaces.Models;

namespace PatreonDownloader.Engine.Stages.Crawling
{
    internal interface IPageCrawler
    {
        event EventHandler<PostCrawlEventArgs> PostCrawlStart;
        event EventHandler<PostCrawlEventArgs> PostCrawlEnd;
        event EventHandler<NewCrawledUrlEventArgs> NewCrawledUrl;
        event EventHandler<CrawlerMessageEventArgs> CrawlerMessage;
        Task<List<CrawledUrl>> Crawl(CampaignInfo campaignInfo, PatreonDownloaderSettings settings);
    }
}

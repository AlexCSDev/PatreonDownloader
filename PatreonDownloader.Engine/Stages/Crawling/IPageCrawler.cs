using System.Collections.Generic;
using System.Threading.Tasks;
using PatreonDownloader.Engine.Models;
using PatreonDownloader.Interfaces.Models;

namespace PatreonDownloader.Engine.Stages.Crawling
{
    internal interface IPageCrawler
    {
        Task<List<CrawledUrl>> Crawl(CampaignInfo campaignInfo, PatreonDownloaderSettings settings);
    }
}

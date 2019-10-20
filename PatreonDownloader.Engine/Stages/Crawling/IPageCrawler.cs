using System.Threading.Tasks;
using PatreonDownloader.Engine.Models;

namespace PatreonDownloader.Engine.Stages.Crawling
{
    internal interface IPageCrawler
    {
        Task Crawl(CampaignInfo campaignInfo);
    }
}

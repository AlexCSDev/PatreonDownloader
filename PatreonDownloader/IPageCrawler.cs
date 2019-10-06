using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using PatreonDownloader.Models;

namespace PatreonDownloader
{
    internal interface IPageCrawler
    {
        Task Crawl(CampaignInfo campaignInfo);
    }
}

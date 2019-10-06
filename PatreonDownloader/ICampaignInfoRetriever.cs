using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using PatreonDownloader.Models;

namespace PatreonDownloader
{
    internal interface ICampaignInfoRetriever
    {
        Task<CampaignInfo> RetrieveCampaignInfo(long campaignId);
    }
}

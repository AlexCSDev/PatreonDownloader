using System.Threading.Tasks;
using PatreonDownloader.Engine.Models;

namespace PatreonDownloader.Engine.Stages.Crawling
{
    internal interface ICampaignInfoRetriever
    {
        Task<CampaignInfo> RetrieveCampaignInfo(long campaignId);
    }
}

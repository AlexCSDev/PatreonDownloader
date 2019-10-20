using System.Threading.Tasks;

namespace PatreonDownloader.Engine.Stages.Crawling
{
    internal interface ICampaignIdRetriever
    {
        Task<long> RetrieveCampaignId(string url);
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PatreonDownloader
{
    internal interface ICampaignIdRetriever
    {
        Task<long> RetrieveCampaignId(string url);
    }
}

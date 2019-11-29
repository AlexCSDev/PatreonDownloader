using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PatreonDownloader.Engine.Models;

namespace PatreonDownloader.Engine.Stages.Crawling
{
    /// <summary>
    /// This class is used to retrieve campaign information (avatar, cover, name)
    /// </summary>
    internal sealed class CampaignInfoRetriever : ICampaignInfoRetriever
    {
        private readonly IWebDownloader _webDownloader;

        public CampaignInfoRetriever(IWebDownloader webDownloader)
        {
            _webDownloader = webDownloader ?? throw new ArgumentNullException(nameof(webDownloader));
        }

        /// <summary>
        /// Retrieve campaign information
        /// </summary>
        /// <param name="campaignId">Campaign ID</param>
        /// <returns>CampaignInfo object containing retrieved campaign information</returns>
        public async Task<CampaignInfo> RetrieveCampaignInfo(long campaignId)
        {
            if(campaignId < 1)
                throw new ArgumentOutOfRangeException(nameof(campaignId), "Campaign id cannot be less than 1");

            string json = await _webDownloader.DownloadString(
                $"https://www.patreon.com/api/campaigns/{campaignId}?include=access_rules.tier.null&fields[access_rule]=access_rule_type%2Camount_cents%2Cpost_count&fields[reward]=title%2Cid%2Camount_cents&json-api-version=1.0");

            Models.JSONObjects.Campaign.Root root = JsonConvert.DeserializeObject<Models.JSONObjects.Campaign.Root>(json);

            return new CampaignInfo
            {
                AvatarUrl = root.Data.Attributes.AvatarUrl, CoverUrl = root.Data.Attributes.CoverUrl, Name = root.Data.Attributes.Name, Id = campaignId
            };
        }
    }
}

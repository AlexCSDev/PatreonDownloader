using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PatreonDownloader.Models;
using PatreonDownloader.Wrappers.Browser;
using PuppeteerSharp;

namespace PatreonDownloader
{
    /// <summary>
    /// This class is used to retrieve campaign information (avatar, cover, name)
    /// </summary>
    internal sealed class CampaignInfoRetriever : ICampaignInfoRetriever
    {
        private readonly IWebBrowser _browser;

        public CampaignInfoRetriever(IWebBrowser browser)
        {
            _browser = browser ?? throw new ArgumentNullException(nameof(browser));
        }

        /// <summary>
        /// Retrieve campaign information
        /// </summary>
        /// <param name="campaignId">Campaign ID</param>
        /// <returns>CampaignInfo object containing retrieved campaign information</returns>
        public async Task<CampaignInfo> RetrieveCampaignInfo(long campaignId)
        {
            var page = await _browser.NewPageAsync();
            IWebResponse response = await page.GoToAsync($"https://www.patreon.com/api/campaigns/{campaignId}?include=access_rules.tier.null&fields[access_rule]=access_rule_type%2Camount_cents%2Cpost_count&fields[reward]=title%2Cid%2Camount_cents&json-api-version=1.0");
            string json = await response.TextAsync();

            Models.JSONObjects.Campaign.Root root = JsonConvert.DeserializeObject<Models.JSONObjects.Campaign.Root>(json);

            await page.CloseAsync();

            return new CampaignInfo
            {
                AvatarUrl = root.Data.Attributes.AvatarUrl, CoverUrl = root.Data.Attributes.CoverUrl, Name = root.Data.Attributes.Name, Id = campaignId
            };
        }
    }
}

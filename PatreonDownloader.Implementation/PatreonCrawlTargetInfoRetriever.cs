using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NLog;
using UniversalDownloaderPlatform.Common.Exceptions;
using UniversalDownloaderPlatform.Common.Interfaces;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace PatreonDownloader.Implementation
{
    internal sealed class PatreonCrawlTargetInfoRetriever : ICrawlTargetInfoRetriever
    {
        private readonly IWebDownloader _webDownloader;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public PatreonCrawlTargetInfoRetriever(IWebDownloader webDownloader)
        {
            _webDownloader = webDownloader ?? throw new ArgumentNullException(nameof(webDownloader));
        }

        public async Task<ICrawlTargetInfo> RetrieveCrawlTargetInfo(string url)
        {
            long campaignId = await GetCampaignId(url);

            return await GetCrawlTargetInfo(campaignId);
        }

        private async Task<long> GetCampaignId(string url)
        {
            if (string.IsNullOrEmpty(url))
                throw new ArgumentException("Argument cannot be null or empty", nameof(url));

            try
            {
                string pageHtml = await _webDownloader.DownloadString(url);

                Regex regex = new Regex("\"self\": ?\"https:\\/\\/www\\.patreon\\.com\\/api\\/campaigns\\/(\\d+)\"");
                Match match = regex.Match(pageHtml);
                if (!match.Success)
                    throw new UniversalDownloaderException($"Unable to retrieve campaign id: regex failed. Report this error to developer.");

                return Convert.ToInt64(match.Groups[1].Value);
            }
            catch(DownloadException downloadEx)
            {
                if(downloadEx.StatusCode == System.Net.HttpStatusCode.Forbidden)
                {
                    _logger.Fatal("Cannot retrieve creator compaign id. This usually means your network is being blocked by Patreon. If you are using VPN or proxy try running application without it. If you are NOT using VPN or proxy - try using them. This is usually NOT an issue with PatreonDownloader even if you can access Patreon via your web browser.");
                    throw new UniversalDownloaderException($"Unable to retrieve campaign id: 403 Forbidden");
                }

                throw new UniversalDownloaderException($"Unable to retrieve campaign id: {downloadEx.Message}", downloadEx);
            }
            catch (Exception ex)
            {
                throw new UniversalDownloaderException($"Unable to retrieve campaign id: {ex.Message}", ex);
            }
        }

        private async Task<ICrawlTargetInfo> GetCrawlTargetInfo(long campaignId)
        {
            try
            {
                if (campaignId < 1)
                    throw new ArgumentOutOfRangeException(nameof(campaignId), "Campaign id cannot be less than 1");

                string json = await _webDownloader.DownloadString(
                    $"https://www.patreon.com/api/campaigns/{campaignId}?include=access_rules.tier.null&fields[access_rule]=access_rule_type%2Camount_cents%2Cpost_count&fields[reward]=title%2Cid%2Camount_cents&json-api-version=1.0");

                Models.JSONObjects.Campaign.Root root = JsonConvert.DeserializeObject<Models.JSONObjects.Campaign.Root>(json);

                return new PatreonCrawlTargetInfo
                {
                    AvatarUrl = root.Data.Attributes.AvatarUrl,
                    CoverUrl = root.Data.Attributes.CoverUrl,
                    Name = root.Data.Attributes.Name,
                    Id = campaignId
                };
            }
            catch (Exception ex)
            {
                throw new UniversalDownloaderException($"Unable to retrieve crawl target info: {ex.Message}", ex);
            }
        }
    }
}

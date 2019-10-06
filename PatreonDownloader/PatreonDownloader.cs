using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NLog;
using PatreonDownloader.Models;
using PuppeteerSharp;

namespace PatreonDownloader
{
    internal sealed class PatreonDownloader : IPatreonDownloader
    {
        private readonly ICampaignIdRetriever _campaignIdRetriever;
        private readonly ICampaignInfoRetriever _campaignInfoRetriever;
        private readonly IPageCrawler _pageCrawler;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public PatreonDownloader(ICampaignIdRetriever campaignIdRetriever, ICampaignInfoRetriever campaignInfoRetriever, IPageCrawler pageCrawler)
        {
            _campaignIdRetriever = campaignIdRetriever ?? throw new ArgumentNullException(nameof(campaignIdRetriever));
            _campaignInfoRetriever = campaignInfoRetriever ?? throw new ArgumentNullException(nameof(campaignInfoRetriever));
            _pageCrawler = pageCrawler ?? throw new ArgumentNullException(nameof(pageCrawler));
        }

        public async Task Download(string url)
        {
            _logger.Debug($"Retrieving campaign ID");
            long campaignId = await _campaignIdRetriever.RetrieveCampaignId(url);

            if (campaignId == -1)
            {
                _logger.Fatal($"Unable to retrieve campaign id for {url}");
                return;
            }

            _logger.Info($"Campaign ID: {campaignId}");

            _logger.Debug($"Retrieving campaign info");
            CampaignInfo campaignInfo = await _campaignInfoRetriever.RetrieveCampaignInfo(campaignId);
            _logger.Info($"Campaign name: {campaignInfo.Name}");

            _logger.Debug("Starting crawler");
            await _pageCrawler.Crawl(campaignInfo);

            _logger.Debug("Finished downloading");
        }
    }
}

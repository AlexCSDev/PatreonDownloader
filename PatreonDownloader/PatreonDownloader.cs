using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NLog;
using PatreonDownloader.Models;
using PatreonDownloader.Wrappers.Browser;
using PuppeteerSharp;

namespace PatreonDownloader
{
    internal sealed class PatreonDownloader : IPatreonDownloader
    {
        private readonly long _campaignId;
        private readonly CookieContainer _cookieContainer;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public PatreonDownloader(long campaignId, CookieContainer cookieContainer)
        {
            _campaignId = campaignId;
            _cookieContainer = cookieContainer ?? throw new ArgumentNullException(nameof(cookieContainer));
        }

        public async Task Download(string url)
        {
            _logger.Debug("Initializing file downloader");
            IWebDownloader webDownloader = new WebDownloader(_cookieContainer);

            _logger.Debug("Initializing campaign info retriever");
            ICampaignInfoRetriever campaignInfoRetriever = new CampaignInfoRetriever(webDownloader);

            _logger.Debug($"Retrieving campaign info");
            CampaignInfo campaignInfo = await campaignInfoRetriever.RetrieveCampaignInfo(_campaignId);
            _logger.Info($"Campaign name: {campaignInfo.Name}");

            _logger.Debug("Initializing page crawler");
            IPageCrawler pageCrawler = new PageCrawler(webDownloader);

            _logger.Debug("Starting crawler");
            await pageCrawler.Crawl(campaignInfo);

            _logger.Debug("Finished downloading");
        }
    }
}

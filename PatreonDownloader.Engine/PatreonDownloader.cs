using System;
using System.Net;
using System.Threading.Tasks;
using NLog;
using PatreonDownloader.Engine.Helpers;
using PatreonDownloader.Engine.Models;
using PatreonDownloader.Engine.Stages.Crawling;
using PatreonDownloader.Engine.Stages.Downloading;
using PatreonDownloader.Interfaces;

namespace PatreonDownloader.Engine
{
    public sealed class PatreonDownloader : IPatreonDownloader, IDisposable
    {
        private readonly ICookieRetriever _cookieRetriever;
        private readonly string _url;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Create a new downloader for specified url
        /// </summary>
        /// <param name="cookieRetriever">Cookie retriever object</param>
        /// <param name="url">Posts page url</param>
        public PatreonDownloader(ICookieRetriever cookieRetriever, string url)
        {
            //TODO: Check that url is valid
            _url = url ?? throw new ArgumentNullException(nameof(url));
            _cookieRetriever = cookieRetriever ?? throw new ArgumentNullException(nameof(cookieRetriever));
        }

        public async Task<bool> Download()
        {
            _logger.Debug("Retrieving cookies");
            CookieContainer cookieContainer = await _cookieRetriever.RetrieveCookies(_url);

            if (cookieContainer == null)
            {
                _logger.Fatal($"Unable to retrieve cookies for {_url}");
                return false;
            }

            _logger.Debug("Initializing file downloader");
            IWebDownloader webDownloader = new WebDownloader(cookieContainer);

            _logger.Debug("Initializing id retriever");
            ICampaignIdRetriever campaignIdRetriever = new CampaignIdRetriever(webDownloader);

            _logger.Debug($"Retrieving campaign ID");
            long campaignId = await campaignIdRetriever.RetrieveCampaignId(_url);

            if (campaignId == -1)
            {
                _logger.Fatal($"Unable to retrieve campaign id for {_url}");
                return false;
            }
            _logger.Info($"Campaign ID: {campaignId}");

            _logger.Debug("Initializing campaign info retriever");
            ICampaignInfoRetriever campaignInfoRetriever = new CampaignInfoRetriever(webDownloader);

            _logger.Debug($"Retrieving campaign info");
            CampaignInfo campaignInfo = await campaignInfoRetriever.RetrieveCampaignInfo(campaignId);
            _logger.Info($"Campaign name: {campaignInfo.Name}");

            _logger.Debug("Initializing remote filename retriever");
            IRemoteFilenameRetriever remoteFilenameRetriever = new RemoteFilenameRetriever(cookieContainer);

            _logger.Debug("Initializing download manager");
            IDownloadManager downloadManager = new DownloadManager(webDownloader, remoteFilenameRetriever);

            _logger.Debug("Initializing page crawler");
            IPageCrawler pageCrawler = new PageCrawler(webDownloader, downloadManager);

            _logger.Debug("Starting crawler");
            await pageCrawler.Crawl(campaignInfo);

            _logger.Debug("Finished downloading");
            return true;
        }

        public void Dispose()
        {
            IDisposable cookieRetrieverDisposable = _cookieRetriever as IDisposable;
            cookieRetrieverDisposable?.Dispose();
        }
    }
}

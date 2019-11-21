using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using NLog;
using PatreonDownloader.Common.Interfaces;
using PatreonDownloader.Engine.Exceptions;
using PatreonDownloader.Engine.Helpers;
using PatreonDownloader.Engine.Models;
using PatreonDownloader.Engine.Stages.Crawling;
using PatreonDownloader.Engine.Stages.Downloading;
using PatreonDownloader.Interfaces;
using PatreonDownloader.Interfaces.Models;

namespace PatreonDownloader.Engine
{
    public sealed class PatreonDownloader : IPatreonDownloader, IDisposable
    {
        private readonly ICookieRetriever _cookieRetriever;
        private CookieContainer _cookieContainer;

        private IWebDownloader _webDownloader;
        private ICampaignIdRetriever _campaignIdRetriever;
        private ICampaignInfoRetriever _campaignInfoRetriever;
        private IRemoteFilenameRetriever _remoteFilenameRetriever;
        private IDownloader _directDownloader;
        private IDownloadManager _downloadManager;
        private IPageCrawler _pageCrawler;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Create a new downloader for specified url
        /// </summary>
        /// <param name="cookieRetriever">Cookie retriever object</param>
        public PatreonDownloader(ICookieRetriever cookieRetriever)
        {
            _cookieRetriever = cookieRetriever ?? throw new ArgumentNullException(nameof(cookieRetriever));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="creatorName"></param>
        /// <param name="settings">Downloader settings, will be set to default values if not provided</param>
        /// <returns></returns>
        public async Task<bool> Download(string creatorName, PatreonDownloaderSettings settings = null)
        {
            if(string.IsNullOrEmpty(creatorName))
                throw new ArgumentException("Argument cannot be null or empty", nameof(creatorName));

            settings = settings ?? new PatreonDownloaderSettings();

            if (string.IsNullOrEmpty(settings.DownloadDirectory))
                settings.DownloadDirectory = Path.Combine(Directory.GetCurrentDirectory(), "download", creatorName.ToLower(CultureInfo.InvariantCulture));

            settings.Consumed = true;
            _logger.Debug($"Patreon downloader settings: {settings}");

            try
            {
                if (!Directory.Exists(settings.DownloadDirectory))
                {
                    Directory.CreateDirectory(settings.DownloadDirectory);
                }
            }
            catch (Exception ex)
            {
                throw new PatreonDownloaderException("Unable to create download directory", ex);
            }

            //TODO: Check that url is valid
            //We only ask for creator name because
            //we assume that every creator has a friendly url
            //like https://www.patreon.com/CREATORNAME/posts
            string url = $"https://www.patreon.com/{creatorName}/posts"; //Build valid url from creator name

            if (_cookieContainer == null)
            {
                _logger.Debug("PatreonDownloader needs initialization...");
                _cookieContainer = await _cookieRetriever.RetrieveCookies();
                if (_cookieContainer == null)
                {
                    throw new PatreonDownloaderException("Unable to retrieve cookies");
                }

                _logger.Debug("Initializing file downloader");
                _webDownloader = new WebDownloader(_cookieContainer);

                _logger.Debug("Initializing id retriever");
                _campaignIdRetriever = new CampaignIdRetriever(_webDownloader);

                _logger.Debug("Initializing campaign info retriever");
                _campaignInfoRetriever = new CampaignInfoRetriever(_webDownloader);

                _logger.Debug("Initializing remote filename retriever");
                _remoteFilenameRetriever = new RemoteFilenameRetriever(_cookieContainer);

                _logger.Debug("Initializing default (direct) downloader");
                _directDownloader = new DirectDownloader(_webDownloader, _remoteFilenameRetriever);

                _logger.Debug("Initializing download manager");
                _downloadManager = new DownloadManager(_directDownloader);

                _logger.Debug("Initializing page crawler");
                _pageCrawler = new PageCrawler(_webDownloader);
            }

            _logger.Debug("Retrieving campaign ID");
            long campaignId = await _campaignIdRetriever.RetrieveCampaignId(url);

            if (campaignId == -1)
            {
                throw new PatreonDownloaderException("Unable to retrieve campaign id");
            }
            _logger.Info($"Campaign ID: {campaignId}");

            _logger.Debug($"Retrieving campaign info");
            CampaignInfo campaignInfo = await _campaignInfoRetriever.RetrieveCampaignInfo(campaignId);
            _logger.Info($"Campaign name: {campaignInfo.Name}");

            _logger.Debug("Starting crawler");
            List<CrawledUrl> crawledUrls = await _pageCrawler.Crawl(campaignInfo, settings);

            _logger.Debug("Starting downloader");

            await _downloadManager.Download(crawledUrls, settings.DownloadDirectory);

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

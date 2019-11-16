using System;
using System.Net;
using System.Threading.Tasks;
using NLog;
using PatreonDownloader.Common.Interfaces;
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
        private readonly PatreonDownloaderSettings _settings;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// Create a new downloader for specified url
        /// </summary>
        /// <param name="cookieRetriever">Cookie retriever object</param>
        /// <param name="creatorName">Creator name</param>
        /// <param name="settings">Downloader settings, will be set to default values if not provided</param>
        public PatreonDownloader(ICookieRetriever cookieRetriever, string creatorName, PatreonDownloaderSettings settings = null)
        {
            //TODO: Check that url is valid
            //We only ask for creator name because
            //we assume that every creator has a friendly url
            //like https://www.patreon.com/CREATORNAME/posts
            _url = creatorName ?? throw new ArgumentNullException(nameof(creatorName));

            _url = $"https://www.patreon.com/{_url}/posts"; //Build valid url from creator name

            _cookieRetriever = cookieRetriever ?? throw new ArgumentNullException(nameof(cookieRetriever));

            _settings = settings ?? new PatreonDownloaderSettings();

            _settings.Consumed = true;

            _logger.Debug($"Patreon downloader instance created. Creator: {creatorName}, Settings: {_settings}");
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

            _logger.Debug("Initializing default (direct) downloader");
            IDownloader directDownloader = new DirectDownloader(webDownloader, remoteFilenameRetriever);

            _logger.Debug("Initializing download manager");
            IDownloadManager downloadManager = new DownloadManager(directDownloader);

            _logger.Debug("Initializing page crawler");
            IPageCrawler pageCrawler = new PageCrawler(webDownloader, downloadManager, _settings);

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

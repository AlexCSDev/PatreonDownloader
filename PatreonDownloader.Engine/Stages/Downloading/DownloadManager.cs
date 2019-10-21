using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NLog;
using PatreonDownloader.Common.Interfaces;
using PatreonDownloader.Engine.Helpers;
using PatreonDownloader.Interfaces;
using PatreonDownloader.Interfaces.Models;

namespace PatreonDownloader.Engine.Stages.Downloading
{
    internal sealed class DownloadManager : IDownloadManager
    {
        private IDownloader _defaultDownloader;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public DownloadManager(IDownloader defaultDownloader)
        {
            _defaultDownloader = defaultDownloader ?? throw new ArgumentNullException(nameof(defaultDownloader));
        }

        public async Task Download(List<CrawledUrl> crawledUrls, string downloadDirectory)
        {
            for (int i = 0; i < crawledUrls.Count; i++)
            {
                CrawledUrl entry = crawledUrls[i];

                if (!UrlChecker.IsValidUrl(entry.Url))
                {
                    _logger.Error($"[{entry.PostId}] Invalid or blacklisted external entry of type {entry.UrlType}: {entry.Url}");
                    continue;
                }

                _logger.Info($"Downloading {i + 1}/{crawledUrls.Count}: {entry.Url}");

                _logger.Debug($"{entry.Url} is {entry.UrlType}");

                //TODO: CUSTOM DOWNLOADER SUPPORT
                if (await _defaultDownloader.IsSupportedUrl(entry.Url))
                    await _defaultDownloader.Download(entry, downloadDirectory);
            }
        }
    }
}

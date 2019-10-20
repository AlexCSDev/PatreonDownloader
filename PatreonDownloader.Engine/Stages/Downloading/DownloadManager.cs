using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using NLog;
using PatreonDownloader.Engine.Helpers;
using PatreonDownloader.Engine.Models;

namespace PatreonDownloader.Engine.Stages.Downloading
{
    internal sealed class DownloadManager : IDownloadManager
    {
        private IWebDownloader _webDownloader;
        private IRemoteFilenameRetriever _remoteFilenameRetriever;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public DownloadManager(IWebDownloader webDownloader, IRemoteFilenameRetriever remoteFilenameRetriever)
        {
            _webDownloader = webDownloader ?? throw new ArgumentNullException(nameof(webDownloader));
            _remoteFilenameRetriever = remoteFilenameRetriever ?? throw new ArgumentNullException(nameof(remoteFilenameRetriever));
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

                string filename = $"{entry.PostId}_";

                switch (entry.UrlType)
                {
                    case CrawledUrlType.ExternalImage:
                        filename += "extimg";
                        break;
                    case CrawledUrlType.DropboxUrl:
                        filename += "extdropbox";
                        break;
                    case CrawledUrlType.GoogleDriveUrl:
                        filename += "extgd";
                        break;
                    case CrawledUrlType.MegaUrl:
                        filename += "extmega";
                        break;
                    case CrawledUrlType.YoutubeVideo:
                        filename += "extyt";
                        break;
                    case CrawledUrlType.ImgurUrl:
                        throw new NotImplementedException("Imgur urls not supported");
                        /*filename += "extimgur";
                        break;*/
                    case CrawledUrlType.DirectUrl:
                        filename += "direct";
                        break;
                    case CrawledUrlType.PostFile:
                        filename += "post";
                        break;
                    case CrawledUrlType.PostAttachment:
                        filename += "attachment";
                        break;
                    case CrawledUrlType.PostMedia:
                        filename += "media";
                        break;
                    case CrawledUrlType.AvatarFile:
                        filename += "avatar";
                        break;
                    case CrawledUrlType.CoverFile:
                        filename += "cover";
                        break;
                    default:
                        throw new ArgumentException($"Invalid url type: {entry.UrlType}");
                }

                if (entry.Filename == null)
                {
                    _logger.Debug($"No filename for {entry.Url}, trying to retrieve...");
                    string remoteFilename =
                        await _remoteFilenameRetriever.RetrieveRemoteFileName(entry.Url);

                    if (remoteFilename == null)
                    {
                        _logger.Error($"[{entry.PostId}] Unable to retrieve name for external entry of type {entry.UrlType}: {entry.Url}");
                        continue;
                    }

                    filename += $"_{remoteFilename}";
                }
                else
                {
                    _logger.Debug($"Filename for {entry.Url} is {entry.Filename}");
                    filename += $"_{entry.Filename}";
                }

                _logger.Debug($"Sanitizing filename: {filename}");
                foreach (char c in System.IO.Path.GetInvalidFileNameChars())
                {
                    filename = filename.Replace(c, '_');
                }

                await _webDownloader.DownloadFile(entry.Url, Path.Combine(downloadDirectory, filename));
            }
        }
    }
}

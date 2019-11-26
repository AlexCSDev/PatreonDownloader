using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NLog;
using PatreonDownloader.Common.Interfaces;
using PatreonDownloader.Engine.Exceptions;
using PatreonDownloader.Engine.Helpers;
using PatreonDownloader.Interfaces;
using PatreonDownloader.Interfaces.Models;

namespace PatreonDownloader.Engine.Stages.Downloading
{
    /// <summary>
    /// This is the default downloader for all files
    /// This downloader is called when no other downloader is available for url
    /// </summary>
    internal sealed class DirectDownloader : IDownloader
    {
        private IWebDownloader _webDownloader;
        private IRemoteFilenameRetriever _remoteFilenameRetriever;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public DirectDownloader(IWebDownloader webDownloader, IRemoteFilenameRetriever remoteFilenameRetriever)
        {
            _webDownloader = webDownloader ?? throw new ArgumentNullException(nameof(webDownloader));
            _remoteFilenameRetriever = remoteFilenameRetriever ??
                                       throw new ArgumentNullException(nameof(remoteFilenameRetriever));
        }

        public async Task<bool> IsSupportedUrl(string url)
        {
            return await Task.FromResult(true);
        }

        public async Task Download(CrawledUrl crawledUrl, string downloadDirectory)
        {
            if (crawledUrl.Url.IndexOf("dropbox.com/", StringComparison.Ordinal) != -1)
            {
                if (!crawledUrl.Url.EndsWith("?dl=1"))
                {
                    if (crawledUrl.Url.EndsWith("?dl=0"))
                        crawledUrl.Url = crawledUrl.Url.Replace("?dl=0", "?dl=1");
                    else
                        crawledUrl.Url = $"{crawledUrl.Url}?dl=1";
                }

                _logger.Debug($"[{crawledUrl.PostId}] This is a dropbox entry: {crawledUrl.Url}");
            }
            else if (crawledUrl.Url.IndexOf("drive.google.com/file/d/", StringComparison.Ordinal) != -1)
            {
                //TODO: GOOGLE DRIVE SUPPORT
                _logger.Fatal($"[{crawledUrl.PostId}] [NOT SUPPORTED] Google Drive link found: {crawledUrl.Url}");
            }
            else if (crawledUrl.Url.StartsWith("https://mega.nz/"))
            {
                //TODO: MEGA SUPPORT
                _logger.Fatal($"[{crawledUrl.PostId}] [NOT SUPPORTED] MEGA link found: {crawledUrl.Url}");
            }
            else if (crawledUrl.Url.IndexOf("youtube.com/watch?v=", StringComparison.Ordinal) != -1 ||
                     crawledUrl.Url.IndexOf("youtu.be/", StringComparison.Ordinal) != -1)
            {
                //TODO: YOUTUBE SUPPORT?
                _logger.Fatal($"[{crawledUrl.PostId}] [NOT SUPPORTED] YOUTUBE link found: {crawledUrl.Url}");
            }
            else if (crawledUrl.Url.IndexOf("imgur.com/", StringComparison.Ordinal) != -1)
            {
                //TODO: IMGUR SUPPORT
                _logger.Fatal($"[{crawledUrl.PostId}] [NOT SUPPORTED] IMGUR link found: {crawledUrl.Url}");
            }

            string filename = $"{crawledUrl.PostId}_";

            switch (crawledUrl.UrlType)
            {
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
                case CrawledUrlType.ExternalUrl:
                    filename += "external";
                    break;
                default:
                    throw new ArgumentException($"Invalid url type: {crawledUrl.UrlType}");
            }

            if (crawledUrl.Filename == null)
            {
                _logger.Debug($"No filename for {crawledUrl.Url}, trying to retrieve...");
                string remoteFilename =
                    await _remoteFilenameRetriever.RetrieveRemoteFileName(crawledUrl.Url);

                if (remoteFilename == null)
                {
                    throw new DownloadException(
                        $"[{crawledUrl.PostId}] Unable to retrieve name for external entry of type {crawledUrl.UrlType}: {crawledUrl.Url}");
                }

                filename += $"_{remoteFilename}";
            }
            else
            {
                _logger.Debug($"Filename for {crawledUrl.Url} is {crawledUrl.Filename}");
                filename += $"_{crawledUrl.Filename}";
            }

            _logger.Debug($"Sanitizing filename: {filename}");
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                filename = filename.Replace(c, '_');
            }

            await _webDownloader.DownloadFile(crawledUrl.Url, Path.Combine(downloadDirectory, filename));
        }
    }
}

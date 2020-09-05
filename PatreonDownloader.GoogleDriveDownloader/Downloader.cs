using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using NLog;
using PatreonDownloader.Common.Exceptions;
using PatreonDownloader.Common.Interfaces;
using PatreonDownloader.Interfaces.Models;

namespace PatreonDownloader.GoogleDriveDownloader
{
    public sealed class Downloader : IDownloader
    {
        private static readonly Regex _googleDriveRegex = new Regex("https:\\/\\/drive\\.google\\.com\\/(?:file\\/d\\/|open\\?id\\=|drive\\/folders\\/|folderview\\?id=|drive\\/u\\/[0-9]+\\/folders\\/)([A-Za-z0-9_-]+)");
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private static readonly GoogleDriveEngine _engine = new GoogleDriveEngine();

        public async Task BeforeStart()
        {
            //Not used
        }

        public async Task<bool> IsSupportedUrl(string url)
        {
            Match match = _googleDriveRegex.Match(url);

            return match.Success;
        }

        public async Task Download(CrawledUrl crawledUrl, string downloadDirectory)
        {
            _logger.Debug($"Received new url: {crawledUrl.Url}, download dir: {downloadDirectory}");

            Match match = _googleDriveRegex.Match(crawledUrl.Url);
            if (!match.Success)
            {
                _logger.Error($"Unable to parse google drive url: {crawledUrl.Url}");
                throw new DownloadException($"Unable to parse google drive url: {crawledUrl.Url}");
            }

            string id  = match.Groups[1].Value;

            string downloadPath = Path.Combine(downloadDirectory,
                $"{crawledUrl.PostId}_{id.Substring(id.Length - 6, 5)}_gd_").TrimEnd(new[] { '/', '\\' });

            _logger.Debug($"Retrieved id: {id}, download path: {downloadPath}");

            try
            {
                _engine.Download(id, downloadPath);
            }
            catch (Exception ex)
            {
                _logger.Error("GOOGLE DRIVE ERROR: " + ex);
                throw new DownloadException($"Unable to download {crawledUrl.Url}", ex);
            }
        }
    }
}

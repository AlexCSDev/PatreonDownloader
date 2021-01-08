using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NLog;
using PatreonDownloader.Common.Enums;
using PatreonDownloader.Common.Exceptions;
using PatreonDownloader.Common.Interfaces.Plugins;
using PatreonDownloader.Interfaces.Models;

namespace PatreonDownloader.GoogleDriveDownloader
{
    public sealed class Plugin : IPlugin
    {
        public string Name => "Google Drive Downloader";
        public string Author => "Aleksey Tsutsey";
        public string ContactInformation => "https://github.com/Megalan/PatreonDownloader";

        private static readonly Regex _googleDriveRegex;
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private static readonly GoogleDriveEngine _engine;

        private bool _overwriteFiles;

        static Plugin()
        {
            if (!System.IO.File.Exists("gd_credentials.json"))
            {
                LogManager.GetCurrentClassLogger().Fatal("!!!![GOOGLE DRIVE]: gd_credentials.json not found, google drive files will not be downloaded! Refer to documentation for additional information. !!!!");
            }

            _googleDriveRegex = new Regex("https:\\/\\/drive\\.google\\.com\\/(?:file\\/d\\/|open\\?id\\=|drive\\/folders\\/|folderview\\?id=|drive\\/u\\/[0-9]+\\/folders\\/)([A-Za-z0-9_-]+)");
            _engine = new GoogleDriveEngine();
        }

        public async Task BeforeStart(bool overwriteFiles)
        {
            _overwriteFiles = overwriteFiles;
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

            string id = match.Groups[1].Value;

            string downloadPath = Path.Combine(downloadDirectory,
                $"{crawledUrl.PostId}_{id.Substring(id.Length - 6, 5)}_gd_").TrimEnd(new[] { '/', '\\' });

            _logger.Debug($"Retrieved id: {id}, download path: {downloadPath}");

            try
            {
                _engine.Download(id, downloadPath, _overwriteFiles);
            }
            catch (Exception ex)
            {
                _logger.Error("GOOGLE DRIVE ERROR: " + ex);
                throw new DownloadException($"Unable to download {crawledUrl.Url}", ex);
            }
        }

        public async Task<List<string>> ExtractSupportedUrls(string htmlContents)
        {
            //Let default plugin do this
            return null;
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using NLog;
using PatreonDownloader.Common.Enums;
using PatreonDownloader.Common.Interfaces.Plugins;
using PatreonDownloader.Engine.Events;
using PatreonDownloader.Engine.Exceptions;
using PatreonDownloader.Engine.Helpers;
using PatreonDownloader.Interfaces.Models;

namespace PatreonDownloader.Engine
{
    /// <summary>
    /// This is the default download/parsing plugin for all files
    /// This plugin is used when no other plugins are available for url
    /// </summary>
    internal sealed class DefaultPlugin : IPlugin
    {
        private IWebDownloader _webDownloader;
        private IRemoteFilenameRetriever _remoteFilenameRetriever;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private Dictionary<string, int> _fileCountDict; //file counter for duplicate check
        private bool _overwriteFiles;

        private static readonly Regex _fileIdRegex; //Regex used to retrieve file id from its url

        public string Name => "Default plugin";

        public string Author => "Aleksey Tsutsey";
        public string ContactInformation => "https://github.com/Megalan/PatreonDownloader";

        static DefaultPlugin()
        {
            _fileIdRegex =
                new Regex(
                    "https:\\/\\/(.+)\\.patreonusercontent\\.com\\/(.+)\\/(.+)\\/patreon-media\\/p\\/post\\/([0-9]+)\\/([a-z0-9]+)",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        public DefaultPlugin(IWebDownloader webDownloader, IRemoteFilenameRetriever remoteFilenameRetriever)
        {
            _webDownloader = webDownloader ?? throw new ArgumentNullException(nameof(webDownloader));
            _remoteFilenameRetriever = remoteFilenameRetriever ??
                                       throw new ArgumentNullException(nameof(remoteFilenameRetriever));
        }

        public async Task<bool> IsSupportedUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            return await Task.FromResult(true);
        }

        public async Task Download(CrawledUrl crawledUrl, string downloadDirectory)
        {
            if(crawledUrl == null)
                throw new ArgumentNullException(nameof(crawledUrl));
            if(string.IsNullOrEmpty(downloadDirectory))
                throw new ArgumentException("Argument cannot be null or empty", nameof(downloadDirectory));

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

            string key = $"{crawledUrl.PostId}_{filename.ToLowerInvariant()}";
            if (!_fileCountDict.ContainsKey(key))
                _fileCountDict.Add(key, 0);

            _fileCountDict[key]++;

            if (_fileCountDict[key] > 1)
            {
                _logger.Warn($"Found more than a single file with the name {filename} in post {crawledUrl.PostId}, file id/sequential number will be appended to its name.");

                string appendStr = _fileCountDict[key].ToString();

                if (crawledUrl.UrlType != CrawledUrlType.ExternalUrl)
                {
                    MatchCollection matches = _fileIdRegex.Matches(crawledUrl.Url);

                    if (matches.Count == 0)
                        throw new DownloadException($"[{crawledUrl.PostId}] Unable to retrieve file id for {crawledUrl.Url}, contact developer!");
                    if (matches.Count > 1)
                        throw new DownloadException($"[{crawledUrl.PostId}] More than 1 media found in URL {crawledUrl.Url}");

                    appendStr = matches[0].Groups[5].Value;
                }

                filename = $"{Path.GetFileNameWithoutExtension(filename)}_{appendStr}{Path.GetExtension(filename)}";
            }

            await _webDownloader.DownloadFile(crawledUrl.Url, Path.Combine(downloadDirectory, filename), _overwriteFiles);
        }

        public async Task BeforeStart(bool overwriteFiles)
        {
            _overwriteFiles = overwriteFiles;
            _fileCountDict = new Dictionary<string, int>();
        }

        public async Task<List<string>> ExtractSupportedUrls(string htmlContents)
        {
            List<string> retList = new List<string>(); 
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlContents);
            HtmlNodeCollection imgNodeCollection = doc.DocumentNode.SelectNodes("//img");
            if (imgNodeCollection != null)
            {
                foreach (var imgNode in imgNodeCollection)
                {
                    if (imgNode.Attributes.Count == 0 || !imgNode.Attributes.Contains("src"))
                        continue;

                    string url = imgNode.Attributes["src"].Value;

                    if (IsAllowedUrl(url))
                    {
                        retList.Add(url);

                        _logger.Debug($"Parsed by default plugin (image): {url}");
                    }
                }
            }

            HtmlNodeCollection linkNodeCollection = doc.DocumentNode.SelectNodes("//a");
            if (linkNodeCollection != null)
            {
                foreach (var linkNode in linkNodeCollection)
                {
                    if (linkNode.Attributes.Count == 0 || !linkNode.Attributes.Contains("href"))
                        continue;

                    var url = linkNode.Attributes["href"].Value;

                    if (IsAllowedUrl(url))
                    {
                        retList.Add(url);
                        _logger.Debug($"Parsed by default plugin (direct): {url}");
                    }
                }
            }

            return retList;
        }

        private bool IsAllowedUrl(string url)
        { 
            if (url.StartsWith("https://mega.nz/"))
            {
                //This should never be called if mega plugin is installed
                _logger.Debug($"Mega plugin not installed, file will not be downloaded: {url}");
                return false;
            }

            if (url.IndexOf("youtube.com/watch?v=", StringComparison.Ordinal) != -1 ||
                     url.IndexOf("youtu.be/", StringComparison.Ordinal) != -1)
            {
                //TODO: YOUTUBE SUPPORT?
                _logger.Fatal($"[NOT SUPPORTED] YOUTUBE link found: {url}");
                return false;
            }

            if (url.IndexOf("imgur.com/", StringComparison.Ordinal) != -1)
            {
                //TODO: IMGUR SUPPORT
                _logger.Fatal($"[NOT SUPPORTED] IMGUR link found: {url}");
                return false;
            }

            return true;
        }
    }
}

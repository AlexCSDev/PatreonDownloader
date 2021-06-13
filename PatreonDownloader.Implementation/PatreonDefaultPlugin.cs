using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using NLog;
using PatreonDownloader.Implementation;
using PatreonDownloader.Implementation.Enums;
using PatreonDownloader.Implementation.Interfaces;
using UniversalDownloaderPlatform.Common.Exceptions;
using UniversalDownloaderPlatform.Common.Interfaces;
using UniversalDownloaderPlatform.Common.Interfaces.Models;
using UniversalDownloaderPlatform.Common.Interfaces.Plugins;
using UniversalDownloaderPlatform.DefaultImplementations.Models;

namespace PatreonDownloader.Engine
{
    /// <summary>
    /// This is the default download/parsing plugin for all files
    /// This plugin is used when no other plugins are available for url
    /// </summary>
    internal sealed class PatreonDefaultPlugin : IPlugin
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

        static PatreonDefaultPlugin()
        {
            _fileIdRegex =
                new Regex(
                    "https:\\/\\/(.+)\\.patreonusercontent\\.com\\/(.+)\\/(.+)\\/patreon-media\\/p\\/post\\/([0-9]+)\\/([a-z0-9]+)",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        public PatreonDefaultPlugin(IWebDownloader webDownloader, IRemoteFilenameRetriever remoteFilenameRetriever)
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

        public async Task Download(ICrawledUrl crawledUrl, string downloadDirectory)
        {
            if(crawledUrl == null)
                throw new ArgumentNullException(nameof(crawledUrl));
            if(string.IsNullOrEmpty(downloadDirectory))
                throw new ArgumentException("Argument cannot be null or empty", nameof(downloadDirectory));

            await _webDownloader.DownloadFile(crawledUrl.Url, crawledUrl.DownloadPath, _overwriteFiles);
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

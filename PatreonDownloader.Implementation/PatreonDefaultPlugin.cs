using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime;
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

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public string Name => "Default plugin";

        public string Author => "Aleksey Tsutsey";
        public string ContactInformation => "https://github.com/Megalan/PatreonDownloader";

        private IUniversalDownloaderPlatformSettings _settings;

        public PatreonDefaultPlugin(IWebDownloader webDownloader)
        {
            _webDownloader = webDownloader ?? throw new ArgumentNullException(nameof(webDownloader));
        }

        public void OnLoad(IDependencyResolver dependencyResolver)
        {
            //do nothing
        }

        public async Task<bool> IsSupportedUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
                return false;

            return await Task.FromResult(true);
        }

        public async Task Download(ICrawledUrl crawledUrl)
        {
            if(crawledUrl == null)
                throw new ArgumentNullException(nameof(crawledUrl));

            if (string.IsNullOrWhiteSpace(crawledUrl.DownloadPath))
                throw new DownloadException($"Download path is not filled for {crawledUrl.Url}");

            await _webDownloader.DownloadFile(crawledUrl.Url, Path.Combine(_settings.DownloadDirectory, crawledUrl.DownloadPath), null); //referer is set in PatreonWebDownloader
        }

        public Task BeforeStart(IUniversalDownloaderPlatformSettings settings)
        {
            _settings = settings;
            return Task.CompletedTask;
        }

        public Task<List<string>> ExtractSupportedUrls(string htmlContents)
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

                    url = url.Replace("&amp;", "&"); //sometimes there are broken links with &amp; instead of &

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

                    url = url.Replace("&amp;", "&"); //sometimes there are broken links with &amp; instead of &

                    if (IsAllowedUrl(url))
                    {
                        retList.Add(url);
                        _logger.Debug($"Parsed by default plugin (direct): {url}");
                    }
                }
            }

            return Task.FromResult(retList);
        }

        private bool IsAllowedUrl(string url)
        { 
            if (url.StartsWith("https://mega.nz/"))
            {
                //This should never be called if mega plugin is installed
                _logger.Debug($"Mega plugin not installed, file will not be downloaded: {url}");
                return false;
            }

            return true;
        }

        public Task<bool> ProcessCrawledUrl(ICrawledUrl crawledUrl)
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

                _logger.Trace($"Dropbox url found: {crawledUrl.Url}");
            }

            return Task.FromResult(false); //we still want full processing for all crawled urls passed here 
        }
    }
}

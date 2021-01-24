using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HtmlAgilityPack;
using Microsoft.Extensions.Configuration;
using NLog;
using NLog.Fluent;
using PatreonDownloader.Common.Interfaces.Plugins;
using PatreonDownloader.Interfaces.Models;

namespace PatreonDownloader.MegaDownloader
{
    public class Plugin : IPlugin
    {
        public string Name => "Mega.nz Downloader";
        public string Author => "Aleksey Tsutsey";
        public string ContactInformation => "https://github.com/Megalan/PatreonDownloader";

        private readonly ILogger _logger = LogManager.GetCurrentClassLogger();
        private bool _overwriteFiles;

        private readonly static Regex _newFormatRegex;
        private readonly static Regex _oldFormatRegex;
        private readonly static MegaCredentials _megaCredentials;
        private static MegaDownloader _megaDownloader;

        static Plugin()
        {
            _newFormatRegex = new Regex(@"/(?<type>(file|folder))/(?<id>[^#]+)#(?<key>[a-zA-Z0-9_-]+)");//Regex("(#F|#)![a-zA-Z0-9]{0,8}![a-zA-Z0-9_-]+");
            _oldFormatRegex = new Regex(@"#(?<type>F?)!(?<id>[^!]+)!(?<key>[^$!\?]+)");

            string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "mega_credentials.json");

            IConfiguration configuration = new ConfigurationBuilder()
                .AddJsonFile(configPath, true, false)
                .Build();

            if (!File.Exists(configPath))
            {
                LogManager.GetCurrentClassLogger().Warn("!!!![MEGA]: mega_credentials.json not found, mega downloading will be limited! Refer to documentation for additional information. !!!!");
            }
            else
            {
                _megaCredentials = new MegaCredentials(configuration["email"], configuration["password"]);
            }

            try
            {
                _megaDownloader = new MegaDownloader(_megaCredentials);
            }
            catch (Exception ex)
            {
                LogManager.GetCurrentClassLogger().Fatal("!!!![MEGA]: Unable to initialize mega downloader, check email and password! No mega files will be downloaded in this session. !!!!");
            }
        }

        public async Task BeforeStart(bool overwriteFiles)
        {
            _overwriteFiles = overwriteFiles;
        }

        public async Task Download(CrawledUrl crawledUrl, string downloadDirectory)
        {
            if (_megaDownloader == null)
            {
                _logger.Fatal($"Mega downloader initialization failure (check credentials), {crawledUrl.Url} will not be downloaded!");
                return;
            }

            try
            {
                var result = _megaDownloader.DownloadUrl(crawledUrl, downloadDirectory);

                if (result != MegaDownloadResult.Success)
                {
                    _logger.Error($"Error while downloading {crawledUrl.Url}! {result}");
                }
            }
            catch (Exception ex)
            {
                _logger.Error(ex, $"MEGA DOWNLOAD EXCEPTION: {ex}");

            }
        }

        public async Task<List<string>> ExtractSupportedUrls(string htmlContents)
        {
            List<string> retList = new List<string>();
            HtmlDocument doc = new HtmlDocument();
            doc.LoadHtml(htmlContents);
            string parseText = string.Join(" ", doc.DocumentNode.Descendants()
                .Where(n => !n.HasChildNodes && !string.IsNullOrWhiteSpace(n.InnerText))
                .Select(n => n.InnerText)); //first get a copy of text without all html tags
            parseText += doc.DocumentNode.InnerHtml; //now append a copy of this text with all html tags intact (otherwise we lose all <a href=... links)

            MatchCollection matchesNewFormat = _newFormatRegex.Matches(parseText);

            MatchCollection matchesOldFormat = _oldFormatRegex.Matches(parseText);

            _logger.Debug($"Found NEW:{matchesNewFormat.Count}|OLD:{matchesOldFormat.Count} possible mega links in description");

            List<string> megaUrls = new List<string>();

            foreach (Match match in matchesNewFormat)
            {
                _logger.Debug($"Parsing mega match new format {match.Value}");
                megaUrls.Add($"https://mega.nz/{match.Groups["type"].Value.Trim()}/{match.Groups["id"].Value.Trim()}#{match.Groups["key"].Value.Trim()}");
            }

            foreach (Match match in matchesOldFormat)
            {
                _logger.Debug($"Parsing mega match old format {match.Value}");
                megaUrls.Add($"https://mega.nz/#{match.Groups["type"].Value.Trim()}!{match.Groups["id"].Value.Trim()}!{match.Groups["key"].Value.Trim()}");
            }

            foreach (string url in megaUrls)
            {
                string sanitizedUrl = url.Split(' ')[0].Replace("&lt;wbr&gt;", "").Replace("&lt;/wbr&gt;", "");
                _logger.Debug($"Adding mega match {sanitizedUrl}");
                if (retList.Contains(sanitizedUrl))
                {
                    _logger.Debug($"Already parsed, skipping: {sanitizedUrl}");
                    continue;
                }
                retList.Add(sanitizedUrl);
            }

            return retList;
        }

        public async Task<bool> IsSupportedUrl(string url)
        {
            MatchCollection matchesNewFormat = _newFormatRegex.Matches(url);
            MatchCollection matchesOldFormat = _oldFormatRegex.Matches(url);

            if (matchesOldFormat.Count > 0 || matchesNewFormat.Count > 0)
                return true;

            return false;
        }
    }
}

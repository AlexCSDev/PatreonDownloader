using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NLog;
using PatreonDownloader.Implementation.Enums;
using PatreonDownloader.Implementation.Interfaces;
using UniversalDownloaderPlatform.Common.Exceptions;
using UniversalDownloaderPlatform.Common.Interfaces;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace PatreonDownloader.Implementation
{
    class PatreonCrawledUrlProcessor : ICrawledUrlProcessor
    {
        private static readonly HashSet<char> _invalidFilenameCharacters;

        private readonly IRemoteFilenameRetriever _remoteFilenameRetriever;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private Dictionary<string, int> _fileCountDict; //file counter for duplicate check
        private static readonly Regex _googleDriveRegex;
        private static readonly Regex _fileIdRegex; //Regex used to retrieve file id from its url

        static PatreonCrawledUrlProcessor()
        {
            _invalidFilenameCharacters = new HashSet<char>(Path.GetInvalidFileNameChars());
            _invalidFilenameCharacters.Add(':');

            _fileIdRegex =
                new Regex(
                    "https:\\/\\/(.+)\\.patreonusercontent\\.com\\/(.+)\\/(.+)\\/patreon-media\\/p\\/post\\/([0-9]+)\\/([a-z0-9]+)",
                    RegexOptions.Compiled | RegexOptions.IgnoreCase);

            _googleDriveRegex = new Regex("https:\\/\\/drive\\.google\\.com\\/(?:file\\/d\\/|open\\?id\\=|drive\\/folders\\/|folderview\\?id=|drive\\/u\\/[0-9]+\\/folders\\/)([A-Za-z0-9_-]+)");
        }

        public PatreonCrawledUrlProcessor(IRemoteFilenameRetriever remoteFilenameRetriever)
        {
            _remoteFilenameRetriever = remoteFilenameRetriever ??
                                       throw new ArgumentNullException(nameof(remoteFilenameRetriever));
            _fileCountDict = new Dictionary<string, int>();

            _logger.Debug("KemonoCrawledUrlProcessor initialized");
        }

        public async Task<bool> ProcessCrawledUrl(ICrawledUrl udpCrawledUrl, string downloadDirectory)
        {
            PatreonCrawledUrl crawledUrl = (PatreonCrawledUrl)udpCrawledUrl;

            bool skipChecks = false; //skip sanitization, duplicate and other checks, do not pass filename to download path
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
                _logger.Debug($"[{crawledUrl.PostId}] mega found: {crawledUrl.Url}");
                skipChecks = true; //mega plugin expects to see only path to the folder where everything will be saved
            }
            else if (_googleDriveRegex.Match(crawledUrl.Url).Success)
            {
                _logger.Debug($"[{crawledUrl.PostId}] google drive found: {crawledUrl.Url}");
                skipChecks = true; //no need for checks if we use google drive plugin
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

            string filename = crawledUrl.Filename;

            if (!skipChecks)
            {
                filename = $"{crawledUrl.PostId}_";

                switch (crawledUrl.UrlType)
                {
                    case PatreonCrawledUrlType.PostFile:
                        filename += "post";
                        break;
                    case PatreonCrawledUrlType.PostAttachment:
                        filename += "attachment";
                        break;
                    case PatreonCrawledUrlType.PostMedia:
                        filename += "media";
                        break;
                    case PatreonCrawledUrlType.AvatarFile:
                        filename += "avatar";
                        break;
                    case PatreonCrawledUrlType.CoverFile:
                        filename += "cover";
                        break;
                    case PatreonCrawledUrlType.ExternalUrl:
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
                    filename += $"_{crawledUrl.Filename}";
                }

                _logger.Debug($"Filename for {crawledUrl.Url} is {filename}");

                _logger.Debug($"Sanitizing filename: {filename}");
                foreach (char c in _invalidFilenameCharacters)
                {
                    filename = filename.Replace(c, '_');
                }
                _logger.Debug($"Sanitized filename: {filename}");


                string key = $"{crawledUrl.PostId}_{filename.ToLowerInvariant()}";
                if (!_fileCountDict.ContainsKey(key))
                    _fileCountDict.Add(key, 0);

                _fileCountDict[key]++;

                if (_fileCountDict[key] > 1)
                {
                    _logger.Warn($"Found more than a single file with the name {filename} in the same folder in post {crawledUrl.PostId}, sequential number will be appended to its name.");

                    string appendStr = _fileCountDict[key].ToString();

                    if (crawledUrl.UrlType != PatreonCrawledUrlType.ExternalUrl)
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
            }

            crawledUrl.DownloadPath = !skipChecks ? Path.Combine(downloadDirectory, filename) : downloadDirectory + Path.DirectorySeparatorChar;

            return true;
        }
    }
}

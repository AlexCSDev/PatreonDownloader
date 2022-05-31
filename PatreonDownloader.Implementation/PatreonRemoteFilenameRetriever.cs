using System;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using HeyRed.Mime;
using NLog;
using PatreonDownloader.Implementation.Helpers;
using PatreonDownloader.Implementation.Interfaces;
using PatreonDownloader.Implementation.Models;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace PatreonDownloader.Implementation
{
    internal class PatreonRemoteFilenameRetriever : IRemoteFilenameRetriever
    {
        private Regex _urlRegex;
        private HttpClient _httpClient;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private bool _isUseMediaType;

        public PatreonRemoteFilenameRetriever()
        {
            _urlRegex = new Regex(@"[^\/\&\?]+\.\w{3,4}(?=([\?&].*$|$))");

            _httpClient = new HttpClient();
        }

        public async Task BeforeStart(IUniversalDownloaderPlatformSettings settings)
        {
            PatreonDownloaderSettings patreonDownloaderSettings = (PatreonDownloaderSettings)settings;
            _isUseMediaType = patreonDownloaderSettings.FallbackToContentTypeFilenames;
        }

        /// <summary>
        /// Retrieve remote file name
        /// </summary>
        /// <param name="url">File name url</param>
        /// <returns>File name if url is valid, null if url is invalid</returns>
        public async Task<string> RetrieveRemoteFileName(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            string mediaType = null;
            string filename = null;
            try
            {
                var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

                if (!string.IsNullOrWhiteSpace(response.Content.Headers.ContentDisposition?.FileName))
                {
                    filename = response.Content.Headers.ContentDisposition.FileName.Replace("\"", "");
                    _logger.Debug($"Content-Disposition returned: {filename}");
                }
                else if (!string.IsNullOrWhiteSpace(response.Content.Headers.ContentType?.MediaType) && _isUseMediaType)
                {
                    mediaType = response.Content.Headers.ContentType?.MediaType;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.Error($"HttpRequestException while trying to retrieve remote file name: {ex}");
            }
            catch (TaskCanceledException ex)
            {
                _logger.Error($"TaskCanceledException while trying to retrieve remote file name: {ex}");
            }

            if (string.IsNullOrWhiteSpace(filename))
            {
                Match match = _urlRegex.Match(url);
                if (match.Success)
                {
                    filename = match.Groups[0].Value; //?? throw new ArgumentException("Invalid url", nameof(url));

                    // Patreon truncates extensions so we need to fix this
                    if (url.Contains("patreonusercontent.com/", StringComparison.Ordinal))
                    {
                        if (filename.EndsWith(".jpe"))
                            filename += "g";
                    }
                    _logger.Debug($"Content-Disposition failed, fallback to url extraction, extracted name: {filename}");
                }
            }

            if (!string.IsNullOrWhiteSpace(mediaType) && string.IsNullOrWhiteSpace(filename))
            {
                filename =
                    $"gen_{HashHelper.ComputeSha256Hash(url)}.{MimeTypesMap.GetExtension(mediaType)}";

                _logger.Debug($"Content-Disposition and url extraction failed, fallback to Content-Type + hash based name: {filename}");
            }

            return filename;
        }
    }
}

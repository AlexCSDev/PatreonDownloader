using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NLog;

namespace PatreonDownloader.Helpers
{
    internal class RemoteFilenameRetriever : IRemoteFilenameRetriever
    {
        private Regex _urlRegex;
        private HttpClient _httpClient;

        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public RemoteFilenameRetriever(CookieContainer cookieContainer)
        {
            if(cookieContainer == null)
                throw new ArgumentNullException(nameof(cookieContainer));

            _urlRegex = new Regex(@"[^\/\&\?]+\.\w{3,4}(?=([\?&].*$|$))");

            var handler = new HttpClientHandler();
            handler.UseCookies = true;
            handler.CookieContainer = cookieContainer;

            _httpClient = new HttpClient(handler);
        }

        /// <summary>
        /// Retrieve remote file name
        /// </summary>
        /// <param name="url">File name url</param>
        /// <returns>File name if url is valid, null if url is invalid</returns>
        public async Task<string> RetrieveRemoteFileName(string url)
        {
            string filename = null;
            try
            {
                var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

                if (response.Content.Headers.ContentDisposition?.FileName != null)
                    filename = response.Content.Headers.ContentDisposition.FileName.Replace("\"", "");

                _logger.Debug($"Content-Disposition returned: {filename}");
            }
            catch (HttpRequestException ex)
            {
                _logger.Error($"HttpRequestException while trying to retrieve remote file name: {ex}");
            }
            catch (TaskCanceledException ex)
            {
                _logger.Error($"TaskCanceledException while trying to retrieve remote file name: {ex}");
            }

            if (String.IsNullOrEmpty(filename))
            {
                Match match = _urlRegex.Match(url);
                if (!match.Success)
                {
                    return null;
                }
                filename = match.Groups[0].Value; //?? throw new ArgumentException("Invalid url", nameof(url));

                // Patreon truncates extensions so we need to fix this
                if (url.Contains("patreonusercontent.com/", StringComparison.Ordinal))
                {
                    if (filename.EndsWith(".jpe"))
                        filename += "g";
                }
                _logger.Debug($"Content-Disposition failed, fallback to url extraction, extracted name: {filename}");
            }

            return filename;
        }
    }
}

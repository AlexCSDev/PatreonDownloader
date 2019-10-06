using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using NLog;
using PatreonDownloader.Models;

namespace PatreonDownloader
{
    internal sealed class FileDownloader : IFileDownloader
    {
        private HttpClient _httpClient;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public FileDownloader(CookieContainer cookieContainer)
        {
            var handler = new HttpClientHandler();
            handler.UseCookies = true;
            handler.CookieContainer = cookieContainer ?? throw new ArgumentNullException(nameof(cookieContainer));
            _httpClient = new HttpClient(handler);
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/77.0.3865.90 Safari/537.36");
        }

        public async Task<DownloadResult> DownloadFile(string url, string path)
        {
            if (File.Exists(path))
            {
                _logger.Warn($"File {path} already exists, file will not be downloaded");
                return DownloadResult.FileExists;
            }

            try
            {
                using (var request = new HttpRequestMessage(HttpMethod.Get, url))
                {
                    using (Stream contentStream =
                            await (await _httpClient.SendAsync(request)).Content.ReadAsStreamAsync(),
                        stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                    {
                        _logger.Debug($"Starting download: {url}");
                        await contentStream.CopyToAsync(stream);
                        _logger.Debug($"Finished download: {url}");
                    }
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.Error($"HttpRequestException while downloading file ({url}): {ex}");
                return DownloadResult.HttpError;
            }
            catch (System.IO.IOException ex)
            {
                _logger.Error($"IOerror while downloading file (not enough disk space?) ({url}): {ex}");
                return DownloadResult.IOError;
            }
            catch (Exception ex)
            {
                _logger.Error($"Unknown error while downloading file ({url}): {ex}");
                return DownloadResult.UnknownError;
            }

            return DownloadResult.Success;
        }
    }
}

using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NLog;
using PatreonDownloader.Implementation.Interfaces;
using UniversalDownloaderPlatform.Common.Enums;
using UniversalDownloaderPlatform.Common.Exceptions;
using UniversalDownloaderPlatform.Common.Helpers;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace PatreonDownloader.Implementation
{
    internal sealed class FfmpegHlsVideoDownloader : IHlsVideoDownloader
    {
        private const string FfmpegExecutable = "ffmpeg";
        private const string PatreonReferer = "https://www.patreon.com";

        private readonly IProcessRunner _processRunner;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private IUniversalDownloaderPlatformSettings _settings;
        private bool _isSaveM3u8;
        private bool _hasLoggedUnavailableWarning;

        public bool IsFfmpegAvailable { get; private set; }

        public FfmpegHlsVideoDownloader(IProcessRunner processRunner)
        {
            _processRunner = processRunner ?? throw new ArgumentNullException(nameof(processRunner));
        }

        public async Task BeforeStart(IUniversalDownloaderPlatformSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _isSaveM3u8 = settings is Models.PatreonDownloaderSettings patreonSettings && patreonSettings.IsSaveM3u8;

            if (_isSaveM3u8)
            {
                IsFfmpegAvailable = false;
                _logger.Info("HLS manifests will be saved as .m3u8 files because --save-m3u8 is enabled.");
                return;
            }

            try
            {
                ProcessRunResult result = await _processRunner.RunAsync(FfmpegExecutable, "-version");
                IsFfmpegAvailable = result.ExitCode == 0;

                if (IsFfmpegAvailable)
                    _logger.Info("ffmpeg detected, HLS video manifests will be downloaded as mp4 files.");
                else
                    _logger.Warn($"ffmpeg detection failed with exit code {result.ExitCode}. HLS manifests will be downloaded as manifest files.");
            }
            catch (Exception ex)
            {
                IsFfmpegAvailable = false;
                _logger.Warn($"ffmpeg was not detected. HLS manifests will be downloaded as manifest files. Detection error: {ex.Message}");
            }
        }

        public bool IsHlsDownload(string url, string path)
        {
            return HasM3u8Path(url) || HasM3u8Path(path);
        }

        public string GetVideoOutputPath(string path)
        {
            if (string.Equals(Path.GetExtension(path), ".m3u8", StringComparison.OrdinalIgnoreCase))
                return Path.ChangeExtension(path, ".mp4");

            return path + ".mp4";
        }

        public async Task<bool> TryDownloadVideo(string url, string path)
        {
            if (!IsHlsDownload(url, path))
                return false;

            if (_isSaveM3u8)
                return false;

            if (!IsFfmpegAvailable)
            {
                if (!_hasLoggedUnavailableWarning)
                {
                    _logger.Warn("ffmpeg is not available, HLS manifests will be downloaded as .m3u8 files.");
                    _hasLoggedUnavailableWarning = true;
                }

                return false;
            }

            string outputPath = GetVideoOutputPath(path);
            if (File.Exists(outputPath) &&
                !FileExistsActionHelper.DoFileExistsActionBeforeDownload(outputPath, -1, false, _settings.FileExistsAction, LoggingFunction))
            {
                return true;
            }

            string temporaryPath = $"{outputPath}.dwnldtmp";
            try
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);

                string directoryName = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrWhiteSpace(directoryName) && !Directory.Exists(directoryName))
                    Directory.CreateDirectory(directoryName);
            }
            catch (Exception ex)
            {
                throw new DownloadException($"Unable to prepare temporary video path for {url}", ex);
            }

            string arguments =
                $"-y -hide_banner -loglevel warning -headers {Quote(BuildHeaders(url))} -i {Quote(url)} -c copy -bsf:a aac_adtstoasc -f mp4 {Quote(temporaryPath)}";

            _logger.Info($"Downloading HLS video with ffmpeg: {url}");
            ProcessRunResult result = await _processRunner.RunAsync(FfmpegExecutable, arguments);
            if (result.ExitCode != 0)
            {
                TryDeleteTemporaryFile(temporaryPath);
                throw new DownloadException($"ffmpeg failed with exit code {result.ExitCode} while downloading {url}: {GetErrorSummary(result.StandardError)}");
            }

            if (!File.Exists(temporaryPath))
                throw new DownloadException($"ffmpeg completed successfully but did not create expected output file {temporaryPath}");

            try
            {
                FileExistsActionHelper.DoFileExistsActionAfterDownload(outputPath, temporaryPath, _settings.FileExistsAction, LoggingFunction);
            }
            catch (Exception ex)
            {
                throw new DownloadException(ex.Message, ex);
            }

            return true;
        }

        private string BuildHeaders(string url)
        {
            StringBuilder headers = new StringBuilder();
            headers.Append("Referer: ").Append(PatreonReferer).Append("\r\n");

            if (!string.IsNullOrWhiteSpace(_settings.UserAgent))
                headers.Append("User-Agent: ").Append(_settings.UserAgent).Append("\r\n");

            string cookieHeader = GetCookieHeader(url);
            if (!string.IsNullOrWhiteSpace(cookieHeader))
                headers.Append("Cookie: ").Append(cookieHeader).Append("\r\n");

            return headers.ToString();
        }

        private string GetCookieHeader(string url)
        {
            if (_settings.CookieContainer == null || !Uri.TryCreate(url, UriKind.Absolute, out Uri uri))
                return null;

            return _settings.CookieContainer.GetCookieHeader(uri);
        }

        private static bool HasM3u8Path(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return false;

            string withoutQuery = value.Split('?', '#')[0];
            return withoutQuery.EndsWith(".m3u8", StringComparison.OrdinalIgnoreCase);
        }

        private static string Quote(string value)
        {
            return $"\"{value.Replace("\"", "\\\"")}\"";
        }

        private static string GetErrorSummary(string error)
        {
            if (string.IsNullOrWhiteSpace(error))
                return "no ffmpeg error output";

            error = error.Trim();
            return error.Length <= 500 ? error : error.Substring(0, 500);
        }

        private static void TryDeleteTemporaryFile(string temporaryPath)
        {
            try
            {
                if (File.Exists(temporaryPath))
                    File.Delete(temporaryPath);
            }
            catch
            {
                // Failure to delete the temp file should not hide the ffmpeg failure.
            }
        }

        private void LoggingFunction(LogMessageLevel level, string message, Exception exception)
        {
            switch (level)
            {
                case LogMessageLevel.Trace:
                    _logger.Trace(message, exception);
                    break;
                case LogMessageLevel.Debug:
                    _logger.Debug(message, exception);
                    break;
                case LogMessageLevel.Fatal:
                    _logger.Fatal(message, exception);
                    break;
                case LogMessageLevel.Error:
                    _logger.Error(message, exception);
                    break;
                case LogMessageLevel.Warning:
                    _logger.Warn(message, exception);
                    break;
                case LogMessageLevel.Information:
                    _logger.Info(message, exception);
                    break;
            }
        }
    }
}

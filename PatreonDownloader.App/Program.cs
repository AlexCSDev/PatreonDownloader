using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using CommandLine;
using Microsoft.Extensions.Configuration;
using NLog;
using PatreonDownloader.App.Models;
using PatreonDownloader.Implementation;
using PatreonDownloader.Implementation.Models;
using UniversalDownloaderPlatform.Common.Enums;
using UniversalDownloaderPlatform.Common.Events;
using UniversalDownloaderPlatform.Engine;

namespace PatreonDownloader.App
{
    class Program
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private static UniversalDownloader _universalDownloader;
        private static PuppeteerEngine.PuppeteerCookieRetriever _cookieRetriever;
        private static IConfiguration _configuration;
        private static int _filesDownloaded;

        static async Task Main(string[] args)
        {
            _configuration = new ConfigurationBuilder()
                .AddJsonFile("settings.json", true, false)
                .Build();

            NLogManager.ReconfigureNLog();

            try
            {
                UpdateChecker updateChecker = new UpdateChecker();
                (bool isUpdateAvailable, string updateMessage) = await updateChecker.IsNewVersionAvailable();
                if (isUpdateAvailable)
                {
                    _logger.Warn("New version is available at https://github.com/AlexCSDev/PatreonDownloader/releases");
                    if (updateMessage != null && !updateMessage.StartsWith("!"))
                        _logger.Warn($"Note from developer: {updateMessage}");
                }

                if (updateMessage != null && updateMessage.StartsWith("!"))
                    _logger.Warn($"Note from developer: {updateMessage.Substring(1)}");
            }
            catch (Exception ex)
            {
                _logger.Error($"Error encountered while checking for updates: {ex}", ex);
            }

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            Console.CancelKeyPress += ConsoleOnCancelKeyPress;

            ParserResult<CommandLineOptions> parserResult = Parser.Default.ParseArguments<CommandLineOptions>(args);

            CommandLineOptions commandLineOptions = null;
            parserResult.WithParsed(options =>
            {
                NLogManager.ReconfigureNLog(options.Verbose);
                commandLineOptions = options;
            });

            if (commandLineOptions == null)
                return;

            try
            {
                await RunPatreonDownloader(commandLineOptions);
            }
            catch (Exception ex)
            {
                _logger.Fatal($"Fatal error, application will be closed: {ex}");
                Environment.Exit(0);
            }
        }

        private static void ConsoleOnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            _logger.Info("Cancellation requested");
            Cleanup();
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            _logger.Debug("Entered process exit");
            Cleanup();
        }

        private static void Cleanup()
        {
            _logger.Debug("Cleanup called");
            if (_universalDownloader != null)
            {
                _logger.Debug("Disposing downloader...");
                try
                {
                    _universalDownloader.Dispose();
                    _universalDownloader = null;
                }
                catch (Exception ex)
                {
                    _logger.Fatal($"Error during patreon downloader disposal! Exception: {ex}");
                }
            }

            if (_cookieRetriever != null)
            {
                _logger.Debug("Disposing cookie retriever...");
                try
                {
                    _cookieRetriever.Dispose();
                    _cookieRetriever = null;
                }
                catch (Exception ex)
                {
                    _logger.Fatal($"Error during cookie retriever disposal! Exception: {ex}");
                }
            }
        }

        private static async Task RunPatreonDownloader(CommandLineOptions commandLineOptions)
        {
            if (string.IsNullOrWhiteSpace(commandLineOptions.Url))
            {
                _logger.Fatal("Creator url should be provided");
                Environment.Exit(0);
                return;
            }

            _universalDownloader = new UniversalDownloader(new PatreonDownloaderModule());

            _filesDownloaded = 0;

            _universalDownloader.StatusChanged += UniversalDownloaderOnStatusChanged;
            _universalDownloader.PostCrawlStart += UniversalDownloaderOnPostCrawlStart;
            //_patreonDownloader.PostCrawlEnd += UniversalDownloaderOnPostCrawlEnd;
            _universalDownloader.NewCrawledUrl += UniversalDownloaderOnNewCrawledUrl;
            _universalDownloader.CrawlerMessage += UniversalDownloaderOnCrawlerMessage;
            _universalDownloader.FileDownloaded += UniversalDownloaderOnFileDownloaded;

            PatreonDownloaderSettings settings = await InitializeSettings(commandLineOptions);
            await _universalDownloader.Download(commandLineOptions.Url, commandLineOptions.DownloadDirectory, settings);

            _universalDownloader.StatusChanged -= UniversalDownloaderOnStatusChanged;
            _universalDownloader.PostCrawlStart -= UniversalDownloaderOnPostCrawlStart;
            //_universalDownloader.PostCrawlEnd -= UniversalDownloaderOnPostCrawlEnd;
            _universalDownloader.NewCrawledUrl -= UniversalDownloaderOnNewCrawledUrl;
            _universalDownloader.CrawlerMessage -= UniversalDownloaderOnCrawlerMessage;
            _universalDownloader.FileDownloaded -= UniversalDownloaderOnFileDownloaded;
            _universalDownloader.Dispose();
            _universalDownloader = null;
        }

        private static async Task<PatreonDownloaderSettings> InitializeSettings(CommandLineOptions commandLineOptions)
        {
            _logger.Info("Retrieving cookies...");
            if (!string.IsNullOrWhiteSpace(commandLineOptions.RemoteBrowserAddress))
                _cookieRetriever =
                    new PuppeteerEngine.PuppeteerCookieRetriever(new Uri(commandLineOptions.RemoteBrowserAddress));
            else
                _cookieRetriever = new PuppeteerEngine.PuppeteerCookieRetriever(true);
            CookieContainer cookieContainer = await _cookieRetriever.RetrieveCookies();
            if (cookieContainer == null)
            {
                throw new Exception("Unable to retrieve cookies");
            }

            string userAgent = await _cookieRetriever.GetUserAgent();
            if (string.IsNullOrWhiteSpace(userAgent))
            {
                throw new Exception("Unable to retrieve user agent");
            }

            _cookieRetriever.Dispose();
            _cookieRetriever = null;

            await Task.Delay(1000); //wait for PuppeteerCookieRetriever to close the browser

            PatreonDownloaderSettings settings = new PatreonDownloaderSettings
            {
                OverwriteFiles = commandLineOptions.OverwriteFiles,
                UrlBlackList = (_configuration["UrlBlackList"] ?? "").ToLowerInvariant().Split("|").ToList(),
                UserAgent = userAgent,
                CookieContainer = cookieContainer,
                SaveAvatarAndCover = commandLineOptions.SaveAvatarAndCover,
                SaveDescriptions = commandLineOptions.SaveDescriptions,
                SaveEmbeds = commandLineOptions.SaveEmbeds,
                SaveJson = commandLineOptions.SaveJson,
                DownloadDirectory = commandLineOptions.DownloadDirectory,
                RemoteFileSizeNotAvailableAction = commandLineOptions.NoRemoteSizeAction,
                UseSubDirectories = commandLineOptions.UseSubDirectories,
                SubDirectoryPattern = commandLineOptions.SubDirectoryPattern,
                MaxFilenameLength = commandLineOptions.MaxFilenameLength
            };

            return settings;
        }

        private static void UniversalDownloaderOnCrawlerMessage(object sender, CrawlerMessageEventArgs e)
        {
            switch (e.MessageType)
            {
                case CrawlerMessageType.Info:
                    _logger.Info(e.Message);
                    break;
                case CrawlerMessageType.Warning:
                    _logger.Warn(e.Message);
                    break;
                case CrawlerMessageType.Error:
                    _logger.Error(e.Message);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void UniversalDownloaderOnNewCrawledUrl(object sender, NewCrawledUrlEventArgs e)
        {
            _logger.Info($"  + {((PatreonCrawledUrl) e.CrawledUrl).UrlTypeAsFriendlyString}: {e.CrawledUrl.Url}");
        }

        private static void UniversalDownloaderOnPostCrawlEnd(object sender, PostCrawlEventArgs e)
        {
            /*if(!e.Success)
                _logger.Error($"Post cannot be parsed: {e.ErrorMessage}");*/
            //_logger.Info(e.Success ? "✓" : "✗");
        }

        private static void UniversalDownloaderOnPostCrawlStart(object sender, PostCrawlEventArgs e)
        {
            _logger.Info($"-> {e.PostId}");
        }

        private static void UniversalDownloaderOnFileDownloaded(object sender, FileDownloadedEventArgs e)
        {
            _filesDownloaded++;
            if (e.Success)
                _logger.Info($"Downloaded {_filesDownloaded}/{e.TotalFiles}: {e.Url}");
            else
                _logger.Error($"Failed to download {e.Url}: {e.ErrorMessage}");
        }

        private static void UniversalDownloaderOnStatusChanged(object sender, DownloaderStatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case DownloaderStatus.Ready:
                    break;
                case DownloaderStatus.Initialization:
                    _logger.Info("Preparing to download...");
                    break;
                case DownloaderStatus.RetrievingCampaignInformation:
                    _logger.Info("Retrieving campaign information...");
                    break;
                case DownloaderStatus.Crawling:
                    _logger.Info("Crawling...");
                    break;
                case DownloaderStatus.Downloading:
                    _logger.Info("Downloading...");
                    break;
                case DownloaderStatus.Done:
                    _logger.Info("Finished");
                    break;
                case DownloaderStatus.ExportingCrawlResults:
                    _logger.Info("Exporting crawl results...");
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
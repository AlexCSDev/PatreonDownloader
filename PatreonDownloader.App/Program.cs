using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommandLine;
using NLog;
using PatreonDownloader.App.Models;
using PatreonDownloader.Common.Interfaces;
using PatreonDownloader.Engine;
using PatreonDownloader.Engine.Enums;
using PatreonDownloader.Engine.Events;
using PatreonDownloader.Engine.Exceptions;
using PatreonDownloader.Interfaces;

namespace PatreonDownloader.App
{
    class Program
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private static Engine.PatreonDownloader _patreonDownloader;
        private static int _filesDownloaded;

        //TODO: Trap ctrl+c for a proper shutdown
        //TODO: Configure logging via command line
        static async Task Main(string[] args)
        {
            NLogManager.ReconfigureNLog();

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            ParserResult<CommandLineOptions> parserResult = Parser.Default.ParseArguments<CommandLineOptions>(args);

            string creatorName = null;
            PatreonDownloaderSettings settings = null;
            parserResult.WithParsed(options =>
            {
                creatorName = options.CreatorName;
                settings = new PatreonDownloaderSettings
                {
                    SaveAvatarAndCover = options.SaveAvatarAndCover,
                    SaveDescriptions = options.SaveDescriptions,
                    SaveEmbeds = options.SaveEmbeds,
                    SaveJson = options.SaveJson,
                    DownloadDirectory = options.DownloadDirectory
                };
                NLogManager.ReconfigureNLog(options.Verbose);
            });

            if (string.IsNullOrEmpty(creatorName) || settings == null)
                return;

            try
            {
                await RunPatreonDownloader(creatorName, settings);
            }
            catch (Exception ex)
            {
                _logger.Fatal($"Fatal error, application will be closed: {ex}");
                Environment.Exit(0);
            }
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            _logger.Debug("Entered process exit");
            if (_patreonDownloader != null)
            {
                _logger.Debug("Disposing downloader...");
                try
                {
                    _patreonDownloader.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.Fatal($"Error during patreon downloader disposal! Exception: {ex}");
                }
            }
        }

        private static async Task RunPatreonDownloader(string creatorName, PatreonDownloaderSettings settings)
        {
            //TODO: Pluggable architecture
            ICookieRetriever cookieRetriever = new PuppeteerCookieRetriever.PuppeteerCookieRetriever();

            using (_patreonDownloader = new Engine.PatreonDownloader(cookieRetriever))
            {
                _filesDownloaded = 0;

                _patreonDownloader.StatusChanged += PatreonDownloaderOnStatusChanged;
                _patreonDownloader.PostCrawlStart += PatreonDownloaderOnPostCrawlStart;
                //_patreonDownloader.PostCrawlEnd += PatreonDownloaderOnPostCrawlEnd;
                _patreonDownloader.NewCrawledUrl += PatreonDownloaderOnNewCrawledUrl;
                _patreonDownloader.CrawlerMessage += PatreonDownloaderOnCrawlerMessage;
                _patreonDownloader.FileDownloaded += PatreonDownloaderOnFileDownloaded;
                await _patreonDownloader.Download(creatorName, settings);
            }
        }

        private static void PatreonDownloaderOnCrawlerMessage(object sender, CrawlerMessageEventArgs e)
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

        private static void PatreonDownloaderOnNewCrawledUrl(object sender, NewCrawledUrlEventArgs e)
        {
            _logger.Info($"  + {e.CrawledUrl.UrlTypeAsFriendlyString}: {e.CrawledUrl.Url}");
        }

        private static void PatreonDownloaderOnPostCrawlEnd(object sender, PostCrawlEventArgs e)
        {
            /*if(!e.Success)
                _logger.Error($"Post cannot be parsed: {e.ErrorMessage}");*/
            //_logger.Info(e.Success ? "✓" : "✗");
        }

        private static void PatreonDownloaderOnPostCrawlStart(object sender, PostCrawlEventArgs e)
        {
            _logger.Info($"-> {e.PostId}");
        }

        private static void PatreonDownloaderOnFileDownloaded(object sender, FileDownloadedEventArgs e)
        {
            _filesDownloaded++;
            if(e.Success)
                _logger.Info($"Downloaded {_filesDownloaded}/{e.TotalFiles}: {e.Url}");
            else
                _logger.Error($"Failed to download {e.Url}: {e.ErrorMessage}");
        }

        private static void PatreonDownloaderOnStatusChanged(object sender, DownloaderStatusChangedEventArgs e)
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
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}

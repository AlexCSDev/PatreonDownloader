using System;
using System.Diagnostics;
using System.Net;
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
        private static PuppeteerEngine.PuppeteerCookieRetriever _cookieRetriever;
        private static int _filesDownloaded;

        static async Task Main(string[] args)
        {
            NLogManager.ReconfigureNLog();

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            Console.CancelKeyPress += ConsoleOnCancelKeyPress;

            ParserResult<CommandLineOptions> parserResult = Parser.Default.ParseArguments<CommandLineOptions>(args);

            string url = null;
            bool headlessBrowser = true;

            PatreonDownloaderSettings settings = null;
            parserResult.WithParsed(options =>
            {
                url = options.Url;
                headlessBrowser = !options.NoHeadless;
                settings = new PatreonDownloaderSettings
                {
                    SaveAvatarAndCover = options.SaveAvatarAndCover,
                    SaveDescriptions = options.SaveDescriptions,
                    SaveEmbeds = options.SaveEmbeds,
                    SaveJson = options.SaveJson,
                    DownloadDirectory = options.DownloadDirectory,
                    OverwriteFiles = options.OverwriteFiles
                };
                NLogManager.ReconfigureNLog(options.Verbose);
            });

            if (string.IsNullOrEmpty(url) || settings == null)
                return;

            try
            {
                await RunPatreonDownloader(url, headlessBrowser, settings);
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
            if (_patreonDownloader != null)
            {
                _logger.Debug("Disposing downloader...");
                try
                {
                    _patreonDownloader.Dispose();
                    _patreonDownloader = null;
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

        private static async Task RunPatreonDownloader(string url, bool headlessBrowser, PatreonDownloaderSettings settings)
        {
            CookieContainer cookieContainer = null;
            using (_cookieRetriever = new PuppeteerEngine.PuppeteerCookieRetriever(headlessBrowser))
            {
                _logger.Info("Retrieving cookies...");
                cookieContainer = await _cookieRetriever.RetrieveCookies();
                if (cookieContainer == null)
                {
                    throw new Exception("Unable to retrieve cookies");
                }
            }

            await Task.Delay(1000); //wait for PuppeteerCookieRetriever to close the browser

            using (_patreonDownloader = new Engine.PatreonDownloader(cookieContainer, headlessBrowser))
            {
                _filesDownloaded = 0;

                _patreonDownloader.StatusChanged += PatreonDownloaderOnStatusChanged;
                _patreonDownloader.PostCrawlStart += PatreonDownloaderOnPostCrawlStart;
                //_patreonDownloader.PostCrawlEnd += PatreonDownloaderOnPostCrawlEnd;
                _patreonDownloader.NewCrawledUrl += PatreonDownloaderOnNewCrawledUrl;
                _patreonDownloader.CrawlerMessage += PatreonDownloaderOnCrawlerMessage;
                _patreonDownloader.FileDownloaded += PatreonDownloaderOnFileDownloaded;
                await _patreonDownloader.Download(url, settings);
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

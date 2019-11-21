using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommandLine;
using NLog;
using PatreonDownloader.App.Models;
using PatreonDownloader.Common.Interfaces;
using PatreonDownloader.Engine;
using PatreonDownloader.Engine.Exceptions;
using PatreonDownloader.Interfaces;

namespace PatreonDownloader.App
{
    class Program
    {
        private static readonly Logger _logger = LogManager.GetCurrentClassLogger();
        private static Engine.PatreonDownloader _patreonDownloader;

        //TODO: Trap ctrl+c for a proper shutdown
        //TODO: Configure logging via command line
        static async Task Main(string[] args)
        {
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
                    SaveJson = options.SaveJson
                };
            });

            if (string.IsNullOrEmpty(creatorName) || settings == null)
                return;

            try
            {
                await RunPatreonDownloader(creatorName, settings);
            }
            catch (Exception ex)
            {
                /*DownloadException downloadException = ex as DownloadException;
                if (downloadException != null)
                {
                    string logMessage = $"Download error: {downloadException.Message}";
                    if (downloadException.InnerException != null)
                        logMessage += $". Inner Exception: {downloadException.InnerException}";
                    _logger.Error($"Download error: {downloadException.Message}. {downloadException.InnerException != null ? "Inner exception: "}");
                }*/
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
                bool result = await _patreonDownloader.Download(creatorName, settings);

                _logger.Info($"{(result ? "Successfully" : "UNSUCCESSFULLY" )} finished downloading {creatorName}");
            }
        }
    }
}

using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using CommandLine;
using NLog;
using PatreonDownloader.App.Models;
using PatreonDownloader.Common.Interfaces;
using PatreonDownloader.Engine;
using PatreonDownloader.Interfaces;

//Alow tests to see internal classes
[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]
[assembly: InternalsVisibleTo("PatreonDownloader.Tests")]
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
                    DownloadAvatarAndCover = options.DownloadAvatarAndCover,
                    SaveDescriptions = options.SaveDescriptions,
                    SaveEmbeds = options.SaveEmbeds,
                    SaveJson = options.SaveJson
                };
            });

            if (string.IsNullOrEmpty(creatorName) || settings == null)
                return;

            await RunPatreonDownloader(creatorName, settings);
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

            using (_patreonDownloader = new Engine.PatreonDownloader(cookieRetriever, creatorName, settings))
            {
                bool result = await _patreonDownloader.Download();

                _logger.Info($"{(result ? "Successfully" : "UNSUCCESSFULLY" )} finished downloading {creatorName}");
            }
        }
    }
}

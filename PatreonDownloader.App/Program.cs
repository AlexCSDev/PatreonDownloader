using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using NLog;
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
        static async Task Main(string[] args)
        {
            _logger.Debug("Patreon downloader started");

            //TODO: Proper command system
            //TODO: Login command
            if (args.Length == 0)
            {
                _logger.Fatal("creator posts page url is required");
                return;
            }

            _logger.Info($"Creator page: {args[0]}");

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            await RunPatreonDownloader(args[0]);
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

        private static async Task RunPatreonDownloader(string url)
        {
            //TODO: Pluggable architecture
            ICookieRetriever cookieRetriever = new PuppeteerCookieRetriever.PuppeteerCookieRetriever();

            //TODO: exception handling
            using (_patreonDownloader = new Engine.PatreonDownloader(cookieRetriever, url))
            {
                bool result = await _patreonDownloader.Download();

                _logger.Info($"{(result ? "Successfully" : "UNSUCCESSFULLY" )} finished downloading {url}");
            }
        }
    }
}

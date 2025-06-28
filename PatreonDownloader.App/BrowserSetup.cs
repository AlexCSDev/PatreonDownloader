using PuppeteerSharp;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PatreonDownloader.App
{
    public static class BrowserSetup
    {
        public static async Task EnsureBrowserDownloaded()
        {
            try
            {
                Console.WriteLine("Checking if Chromium browser is downloaded...");
                
                var browserFetcher = new BrowserFetcher();
                
                // Check if browser is already downloaded
                var localRevisions = browserFetcher.GetInstalledBrowsers();
                if (localRevisions.Count() > 0)
                {
                    Console.WriteLine("Chromium browser already downloaded.");
                    return;
                }
                
                Console.WriteLine("Downloading Chromium browser (this may take a few minutes)...");
                await browserFetcher.DownloadAsync();
                Console.WriteLine("Browser download completed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading browser: {ex.Message}");
                throw;
            }
        }
    }
}


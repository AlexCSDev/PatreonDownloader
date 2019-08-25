using PuppeteerSharp;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace PatreonDownloader
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Patreon downloader started");
            var result = RunPatreonDownloader().Result;
            while (true)
            {
                Thread.Sleep(1000);
            }
        }

        private static async Task<bool> RunPatreonDownloader()
        {
            Console.WriteLine("Downloading browser");
            await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
            Console.WriteLine("Launching browser");
            var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Devtools = true,
                UserDataDir = Path.Combine(Environment.CurrentDirectory,"chromedata")
            });
            var page = await browser.NewPageAsync();
            await page.SetRequestInterceptionAsync(true);
            page.Request += (sender, e) =>
            {
                //Console.WriteLine($"Request: {e.Request.Url}");
                //e.Request
               /* if (e.Request.ResourceType == ResourceType.Image)
                    e.Request.AbortAsync();
                else*/
                    e.Request.ContinueAsync();
            };
            page.Response += (sender, args) =>
            {
                //Console.WriteLine($"Response: {args.Response.Url}");
                if (args.Response.Url.Contains("https://www.patreon.com/api/posts"))
                {
                    Console.WriteLine("API RESPONSE: "+args.Response.Url);
                }
            };
            await page.GoToAsync("https://www.patreon.com/bigclive/posts");
            //await page.ScreenshotAsync(outputFile);
            return true;
        }
    }
}

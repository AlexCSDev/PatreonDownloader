using PuppeteerSharp;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;

namespace PatreonDownloader
{
    class Program
    {
        static void Main(string[] args)
        {
            Log.Instance.Debug("Patreon downloader started");
            if (args.Length == 0)
            {
                Log.Instance.Fatal("creator posts page url is required");
                return;
            }

            Log.Instance.Info($"Creator page: {args[0]}");

            var result = RunPatreonDownloader(args[0]).Result;
            while (true)
            {
                Thread.Sleep(1000);
            }
        }

        private static async Task<bool> RunPatreonDownloader(string url)
        {
            Log.Instance.Debug("Downloading browser");
            await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);
            Log.Instance.Debug("Launching browser");
            var browser = await Puppeteer.LaunchAsync(new LaunchOptions
            {
                Devtools = true,
                UserDataDir = Path.Combine(Environment.CurrentDirectory,"chromedata")
            });

            Log.Instance.Debug("Initializing id retriever");
            CampaignIdRetriever campaignIdRetriever = new CampaignIdRetriever(browser);
            long campaignId = await campaignIdRetriever.RetrieveCampaignId(url);

            if (campaignId == -1)
            {
                Log.Instance.Fatal($"Unable to retrieve campaign id for {url}");
                return false;
            }

            Log.Instance.Info($"Campaign ID retrieved: {campaignId}");

            Log.Instance.Debug("Starting crawler");
            PageCrawler crawler = new PageCrawler(browser);

            await crawler.Crawl(campaignId);

            /*tring text = await response.TextAsync();
            var textLength = text.Length;
            int jsonStart = text.IndexOf("Object.assign(window.patreon.bootstrap, {");
            string json = "";
            if (jsonStart != -1)
            {
                jsonStart += "Object.assign(window.patreon.bootstrap, {".Length - 1;
                int jsonEnd = text.IndexOf("});", jsonStart) + 1;
                Console.WriteLine(text.Substring(jsonStart, jsonEnd - jsonStart));
                json = text.Substring(jsonStart, jsonEnd - jsonStart);
                XmlDocument xml = JsonConvert.DeserializeXmlNode(json, "root");
            }*/
            //await page.ScreenshotAsync(outputFile);
            return true;
        }
    }
}

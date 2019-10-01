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
            Log.Instance.Debug("Initializing crawl engine");
            PageCrawler crawler = new PageCrawler(browser);
            var page = await browser.NewPageAsync();
            /*await page.SetRequestInterceptionAsync(true);
            page.Request += (sender, e) =>
            {
                //Console.WriteLine($"Request: {e.Request.Url}");
                //e.Request
               /* if (e.Request.ResourceType == ResourceType.Image)
                    e.Request.AbortAsync();
                else*/
                   /* e.Request.ContinueAsync();
            };*/
            //await page.SetJavaScriptEnabledAsync(false);
            bool foundId = false;
            long id = 0;
            page.Response += async(sender, args) =>
            {
                //Console.WriteLine($"Response: {args.Response.Url}");

                //if(args.re)
                if (args.Response.Url.Contains("https://www.patreon.com/api/stream"))
                {
                    Console.WriteLine("API RESPONSE: "+args.Response.Url);
                }

                if (args.Response.Url.Contains("https://www.patreon.com/api/posts") && !foundId)
                {
                    //try to find campaign id:
                    string urlStr = args.Response.Url;
                    int idPos = urlStr.IndexOf("filter[campaign_id]=");
                    if (idPos != -1)
                    {
                        int startPos = idPos + "filter[campaign_id]=".Length;
                        int endPos = urlStr.IndexOf("&", startPos);
                        id = Convert.ToInt64(urlStr.Substring(startPos, endPos - startPos));
                        foundId = true;
                        Log.Instance.Debug("Found campaign id: "+id);
                        await crawler.Crawl(id);
                    }
                }
            };
            var response = await page.GoToAsync(url);

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

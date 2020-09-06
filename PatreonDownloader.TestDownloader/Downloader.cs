using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using PatreonDownloader.Common.Interfaces;
using PatreonDownloader.Interfaces.Models;

namespace PatreonDownloader.TestDownloader
{
    public sealed class Downloader : IDownloader
    {
        public async Task BeforeStart()
        {
            Console.WriteLine($"Test downloader's BeforeStart called");
        }

        public async Task<bool> IsSupportedUrl(string url)
        {
            Console.WriteLine($"Test downloader's IsSupportedUrl called with url {url}");
            if (url == "http://example.com")
                return true;

            return false;
        }

        public async Task Download(CrawledUrl crawledUrl, string downloadDirectory)
        {
            Console.WriteLine($"Received new url: {crawledUrl.Url}, download dir: {downloadDirectory}");
        }
    }
}

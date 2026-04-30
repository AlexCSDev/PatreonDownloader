using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using PatreonDownloader.Engine;
using PatreonDownloader.Implementation;
using PatreonDownloader.Implementation.Interfaces;
using PatreonDownloader.Implementation.Models;
using UniversalDownloaderPlatform.Common.Interfaces;
using UniversalDownloaderPlatform.Common.Interfaces.Models;
using Xunit;

namespace PatreonDownloader.Tests
{
    public class PatreonDefaultPluginHlsTests
    {
        [Fact]
        public async Task Download_HlsHandledByFfmpeg_DoesNotDownloadManifest()
        {
            var webDownloader = new FakeWebDownloader();
            var hlsDownloader = new FakeHlsVideoDownloader { IsHandled = true, IsFfmpegAvailableValue = true };
            var plugin = new PatreonDefaultPlugin(webDownloader, hlsDownloader);
            await plugin.BeforeStart(new PatreonDownloaderSettings { DownloadDirectory = @"c:\downloads" });

            await plugin.Download(new PatreonCrawledUrl { Url = "https://example.com/video.m3u8", DownloadPath = "video.m3u8" });

            Assert.Empty(webDownloader.Downloads);
            Assert.Single(hlsDownloader.Downloads);
            Assert.Equal(@"c:\downloads\video.m3u8", hlsDownloader.Downloads[0].Path);
        }

        [Fact]
        public async Task Download_HlsNotHandled_FallsBackToManifestDownload()
        {
            var webDownloader = new FakeWebDownloader();
            var hlsDownloader = new FakeHlsVideoDownloader { IsHandled = false, IsFfmpegAvailableValue = false };
            var plugin = new PatreonDefaultPlugin(webDownloader, hlsDownloader);
            await plugin.BeforeStart(new PatreonDownloaderSettings { DownloadDirectory = @"c:\downloads" });

            await plugin.Download(new PatreonCrawledUrl { Url = "https://example.com/video.m3u8", DownloadPath = "video.m3u8" });

            Assert.Single(webDownloader.Downloads);
            Assert.Equal(@"c:\downloads\video.m3u8", webDownloader.Downloads[0].Path);
        }

        private sealed class FakeHlsVideoDownloader : IHlsVideoDownloader
        {
            public bool IsHandled { get; set; }
            public bool IsFfmpegAvailableValue { get; set; }
            public List<(string Url, string Path)> Downloads { get; } = new();
            public bool IsFfmpegAvailable => IsFfmpegAvailableValue;
            public Task BeforeStart(IUniversalDownloaderPlatformSettings settings) => Task.CompletedTask;
            public bool IsHlsDownload(string url, string path) => url.EndsWith(".m3u8") || path.EndsWith(".m3u8");
            public string GetVideoOutputPath(string path) => path;

            public Task<bool> TryDownloadVideo(string url, string path)
            {
                Downloads.Add((url, path));
                return Task.FromResult(IsHandled);
            }
        }

        private sealed class FakeWebDownloader : IWebDownloader
        {
            public List<(string Url, string Path, string Referer)> Downloads { get; } = new();
            public Task BeforeStart(IUniversalDownloaderPlatformSettings settings) => Task.CompletedTask;
            public void UpdateCookies(CookieCollection cookieCollection) { }

            public Task DownloadFile(string url, string path, string refererUrl = null)
            {
                Downloads.Add((url, path, refererUrl));
                return Task.CompletedTask;
            }

            public Task<string> DownloadString(string url, string refererUrl = null) => Task.FromResult("");
        }
    }
}

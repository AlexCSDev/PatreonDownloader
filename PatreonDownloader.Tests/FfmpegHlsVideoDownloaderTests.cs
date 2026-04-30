using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PatreonDownloader.Implementation;
using PatreonDownloader.Implementation.Interfaces;
using PatreonDownloader.Implementation.Models;
using UniversalDownloaderPlatform.Common.Enums;
using Xunit;

namespace PatreonDownloader.Tests
{
    public class FfmpegHlsVideoDownloaderTests
    {
        [Theory]
        [InlineData("https://example.com/video.m3u8", "c:\\downloads\\post_video.bin")]
        [InlineData("https://example.com/video.m3u8?token=abc", "c:\\downloads\\post_video")]
        [InlineData("https://example.com/video", "c:\\downloads\\post_video.m3u8")]
        public void IsHlsDownload_UrlOrPathIndicatesM3u8_ReturnsTrue(string url, string path)
        {
            var downloader = new FfmpegHlsVideoDownloader(new FakeProcessRunner());

            Assert.True(downloader.IsHlsDownload(url, path));
        }

        [Fact]
        public void GetVideoOutputPath_M3u8Path_ReturnsMp4Path()
        {
            var downloader = new FfmpegHlsVideoDownloader(new FakeProcessRunner());

            Assert.Equal(@"c:\downloads\post_video.mp4", downloader.GetVideoOutputPath(@"c:\downloads\post_video.m3u8"));
        }

        [Fact]
        public void GetVideoOutputPath_NonM3u8Path_ReturnsMp4Path()
        {
            var downloader = new FfmpegHlsVideoDownloader(new FakeProcessRunner());

            Assert.Equal(@"c:\downloads\post_video.mp4", downloader.GetVideoOutputPath(@"c:\downloads\post_video"));
        }

        [Fact]
        public async Task BeforeStart_FfmpegVersionSucceeds_MarksFfmpegAvailable()
        {
            var runner = new FakeProcessRunner { Result = new ProcessRunResult(0, "ffmpeg version", "") };
            var downloader = new FfmpegHlsVideoDownloader(runner);

            await downloader.BeforeStart(new PatreonDownloaderSettings());

            Assert.True(downloader.IsFfmpegAvailable);
            Assert.Contains(runner.Calls, x => x.FileName == "ffmpeg" && x.Arguments == "-version");
        }

        [Fact]
        public async Task TryDownloadVideo_FfmpegUnavailable_ReturnsFalse()
        {
            var runner = new FakeProcessRunner { Result = new ProcessRunResult(1, "", "missing") };
            var downloader = new FfmpegHlsVideoDownloader(runner);
            await downloader.BeforeStart(new PatreonDownloaderSettings());

            bool handled = await downloader.TryDownloadVideo("https://example.com/video.m3u8", @"c:\downloads\video.m3u8");

            Assert.False(handled);
        }

        [Fact]
        public async Task TryDownloadVideo_SaveM3u8Enabled_DoesNotRunFfmpeg()
        {
            var runner = new FakeProcessRunner { Result = new ProcessRunResult(0, "ffmpeg version", "") };
            var downloader = new FfmpegHlsVideoDownloader(runner);
            await downloader.BeforeStart(new PatreonDownloaderSettings { IsSaveM3u8 = true });

            bool handled = await downloader.TryDownloadVideo("https://example.com/video.m3u8", @"c:\downloads\video.m3u8");

            Assert.False(handled);
            Assert.Empty(runner.Calls);
        }

        [Fact]
        public async Task TryDownloadVideo_FfmpegAvailable_RunsFfmpegWithHeadersAndMp4Output()
        {
            string tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
            string output = Path.Combine(tempRoot, "video.m3u8");
            var runner = new FakeProcessRunner { Result = new ProcessRunResult(0, "", "") };
            var settings = new PatreonDownloaderSettings { UserAgent = "UnitTestAgent/1.0" };
            var downloader = new FfmpegHlsVideoDownloader(runner);
            await downloader.BeforeStart(settings);

            bool handled = await downloader.TryDownloadVideo("https://example.com/video.m3u8", output);

            Assert.True(handled);
            Assert.Contains(runner.Calls, x => x.FileName == "ffmpeg" &&
                                              x.Arguments.Contains("-headers") &&
                                              x.Arguments.Contains("Referer: https://www.patreon.com") &&
                                              x.Arguments.Contains("User-Agent: UnitTestAgent/1.0") &&
                                              x.Arguments.Contains("-c copy") &&
                                              x.Arguments.Contains("-f mp4") &&
                                              x.Arguments.Contains(Path.Combine(tempRoot, "video.mp4.dwnldtmp")));
            Assert.True(File.Exists(Path.Combine(tempRoot, "video.mp4")));
            Directory.Delete(tempRoot, true);
        }

        [Fact]
        public async Task TryDownloadVideo_KeepExistingAndOutputExists_SkipsFfmpeg()
        {
            string tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(tempRoot);
            string manifestPath = Path.Combine(tempRoot, "video.m3u8");
            string videoPath = Path.Combine(tempRoot, "video.mp4");
            await File.WriteAllTextAsync(videoPath, "existing");
            var runner = new FakeProcessRunner { Result = new ProcessRunResult(0, "", "") };
            var downloader = new FfmpegHlsVideoDownloader(runner);
            await downloader.BeforeStart(new PatreonDownloaderSettings { FileExistsAction = FileExistsAction.KeepExisting });

            bool handled = await downloader.TryDownloadVideo("https://example.com/video.m3u8", manifestPath);

            Assert.True(handled);
            Assert.Single(runner.Calls);
            Directory.Delete(tempRoot, true);
        }

        private sealed class FakeProcessRunner : IProcessRunner
        {
            public List<(string FileName, string Arguments)> Calls { get; } = new();
            public ProcessRunResult Result { get; set; } = new(0, "", "");

            public async Task<ProcessRunResult> RunAsync(string fileName, string arguments)
            {
                Calls.Add((fileName, arguments));
                if (fileName == "ffmpeg" && arguments != "-version" && Result.ExitCode == 0)
                {
                    int lastQuote = arguments.LastIndexOf('"');
                    int previousQuote = arguments.LastIndexOf('"', lastQuote - 1);
                    string output = arguments.Substring(previousQuote + 1, lastQuote - previousQuote - 1);
                    await File.WriteAllTextAsync(output, "video");
                }

                return Result;
            }
        }
    }
}

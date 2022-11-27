using PatreonDownloader.Implementation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using PatreonDownloader.Implementation.Enums;
using PatreonDownloader.Implementation.Models;
using UniversalDownloaderPlatform.Common.Enums;
using Xunit;

namespace PatreonDownloader.Tests
{
    public class PatreonCrawledUrlProcessorTests
    {
        [Fact]
        public async Task ProcessCrawledUrl_MediaFileNameIsUrl_IsTruncatedAndNoExtension()
        {
            PatreonDownloaderSettings settings = new PatreonDownloaderSettings
            {
                CookieContainer = new CookieContainer(),
                DownloadDirectory = "c:\\downloads",
                MaxDownloadRetries = 10,
                OverwriteFiles = false,
                RemoteFileSizeNotAvailableAction = RemoteFileSizeNotAvailableAction.KeepExisting,
                RetryMultiplier = 1,
                SaveAvatarAndCover = true,
                SaveDescriptions = true,
                SaveEmbeds = true,
                SaveJson = true,
                UseSubDirectories = true,
                SubDirectoryPattern = "[%PostId%] %PublishedAt% %PostTitle%",
                MaxFilenameLength = 50
            };

            settings.Consumed = true;

            PatreonCrawledUrl crawledUrl = new PatreonCrawledUrl
            {
                PostId = "123456",
                Title = "Test Post",
                PublishedAt = DateTime.Parse("07.07.2020 20:00:15"),
                Url = "https://www.patreon.com/media-u/Z0FBQUFBQmhXZDd3LXMwN0lJUFdVYTVIMEY1OGxzZTgwaFpQcW5TMk5WQVgxd2JVRFZvRXhjMjQ2V09oTW51eUpLQzIyOW1TdHRzYkY2Uk4yclAwX0VsSXBPMFZsNTBTcmZoaGx4OXJkR1Zham1CYl9fOWNVb3AzZGN1Wl9FMmNzcmIxc3hDek4xcHNuRV92LUVqQ0JESE4tcVBNYzlxYkRnWQ1=",
                Filename = "https://www.patreon.com/media-u/Z0FBQUFBQmhXZDd3a0xfckdEWmFrU0tjZHFUUkZfaDZ1OW92TjFVWFVDNk02c2FvS2FNczZxMS1rSVlaNUotX095dUNhdzJBSmYzMVpDV1luR1BYSXR6OVlZelpFOFFVektEcnpJT1plbElua2kwT1N2ZUMyU1NWaHV0eHQydWhnUXlmVWVLVDFYclBsSDBRaVJ3MDA5d2tzdDRZR3dtb3dBWQ1=",
                UrlType = PatreonCrawledUrlType.PostMedia
            };

            PatreonCrawledUrlProcessor crawledUrlProcessor = new PatreonCrawledUrlProcessor(new PatreonRemoteFilenameRetriever());
            await crawledUrlProcessor.BeforeStart(settings);
            await crawledUrlProcessor.ProcessCrawledUrl(crawledUrl,
                Path.Combine(settings.DownloadDirectory, "UnitTesting"));

            Assert.Equal(@"c:\downloads\UnitTesting\[123456] 2020-07-07 Test Post\media_https___www.patreon.com_media-u_Z0FBQUFBQmhX", crawledUrl.DownloadPath);
        }

        [Fact]
        public async Task ProcessCrawledUrl_MediaFileNameTooLong_IsTruncatedWithExtension()
        {
            PatreonDownloaderSettings settings = new PatreonDownloaderSettings
            {
                CookieContainer = new CookieContainer(),
                DownloadDirectory = "c:\\downloads",
                MaxDownloadRetries = 10,
                OverwriteFiles = false,
                RemoteFileSizeNotAvailableAction = RemoteFileSizeNotAvailableAction.KeepExisting,
                RetryMultiplier = 1,
                SaveAvatarAndCover = true,
                SaveDescriptions = true,
                SaveEmbeds = true,
                SaveJson = true,
                UseSubDirectories = true,
                SubDirectoryPattern = "[%PostId%] %PublishedAt% %PostTitle%",
                MaxFilenameLength = 50
            };

            settings.Consumed = true;

            PatreonCrawledUrl crawledUrl = new PatreonCrawledUrl
            {
                PostId = "123456",
                Title = "Test Post",
                PublishedAt = DateTime.Parse("07.07.2020 20:00:15"),
                Url = "https://www.patreon.com/media-u/Z0FBQUFBQmhXZDd3LXMwN0lJUFdVYTVIMEY1OGxzZTgwaFpQcW5TMk5WQVgxd2JVRFZvRXhjMjQ2V09oTW51eUpLQzIyOW1TdHRzYkY2Uk4yclAwX0VsSXBPMFZsNTBTcmZoaGx4OXJkR1Zham1CYl9fOWNVb3AzZGN1Wl9FMmNzcmIxc3hDek4xcHNuRV92LUVqQ0JESE4tcVBNYzlxYkRnWQ1=",
                Filename = "E0OarAVlc0iipzgUC7JdvBCf9fgSmbwk3xRDjRGByTM24SuMl6HkY1DIdGfcvnZhbTb978AHonnwqWNzMPEWBRQp007ateP9ByhB.png",
                UrlType = PatreonCrawledUrlType.PostFile
            };

            PatreonCrawledUrlProcessor crawledUrlProcessor = new PatreonCrawledUrlProcessor(new PatreonRemoteFilenameRetriever());
            await crawledUrlProcessor.BeforeStart(settings);
            await crawledUrlProcessor.ProcessCrawledUrl(crawledUrl,
                Path.Combine(settings.DownloadDirectory, "UnitTesting"));

            Assert.Equal(@"c:\downloads\UnitTesting\[123456] 2020-07-07 Test Post\post_E0OarAVlc0iipzgUC7JdvBCf9fgSmbwk3xRDjRGByTM24.png", crawledUrl.DownloadPath);
        }

        [Fact]
        public async Task ProcessCrawledUrl_PostMultipleFilesWithTheSameName_IdIsAppendedStartingWithSecondFile()
        {
            PatreonDownloaderSettings settings = new PatreonDownloaderSettings
            {
                CookieContainer = new CookieContainer(),
                DownloadDirectory = "c:\\downloads",
                MaxDownloadRetries = 10,
                OverwriteFiles = false,
                RemoteFileSizeNotAvailableAction = RemoteFileSizeNotAvailableAction.KeepExisting,
                RetryMultiplier = 1,
                SaveAvatarAndCover = true,
                SaveDescriptions = true,
                SaveEmbeds = true,
                SaveJson = true,
                UseSubDirectories = true,
                SubDirectoryPattern = "[%PostId%] %PublishedAt% %PostTitle%",
                MaxFilenameLength = 50
            };

            settings.Consumed = true;

            PatreonCrawledUrlProcessor crawledUrlProcessor = new PatreonCrawledUrlProcessor(new PatreonRemoteFilenameRetriever());
            await crawledUrlProcessor.BeforeStart(settings);

            PatreonCrawledUrl crawledUrl = new PatreonCrawledUrl
            {
                PostId = "123456",
                Title = "Test Post",
                PublishedAt = DateTime.Parse("07.07.2020 20:00:15"),
                Url = "https://c10.patreonusercontent.com/4/patreon-media/p/post/123456/710deacb70e940d999bf2f3022e1e2f0/WAJhIjoxZZJwIjoxfQ%3D%3D/1.png?token-time=1661644800&token-hash=123",
                Filename = "1.png",
                UrlType = PatreonCrawledUrlType.PostMedia
            };

            await crawledUrlProcessor.ProcessCrawledUrl(crawledUrl,
                Path.Combine(settings.DownloadDirectory, "UnitTesting"));

            Assert.Equal(@"c:\downloads\UnitTesting\[123456] 2020-07-07 Test Post\media_1.png", crawledUrl.DownloadPath);

            crawledUrl = new PatreonCrawledUrl
            {
                PostId = "123456",
                Title = "Test Post",
                PublishedAt = DateTime.Parse("07.07.2020 20:00:15"),
                Url = "https://c10.patreonusercontent.com/4/patreon-media/p/post/123456/110deacb70e940d999bf2f3022e1e2f0/WAJhIjoxZZJwIjoxfQ%3D%3D/1.png?token-time=1661644800&token-hash=123",
                Filename = "1.png",
                UrlType = PatreonCrawledUrlType.PostMedia
            };

            await crawledUrlProcessor.ProcessCrawledUrl(crawledUrl,
                Path.Combine(settings.DownloadDirectory, "UnitTesting"));

            Assert.Equal(@"c:\downloads\UnitTesting\[123456] 2020-07-07 Test Post\media_1_110deacb70e940d999bf2f3022e1e2f0.png", crawledUrl.DownloadPath);

            crawledUrl = new PatreonCrawledUrl
            {
                PostId = "123456",
                Title = "Test Post",
                PublishedAt = DateTime.Parse("07.07.2020 20:00:15"),
                Url = "https://c10.patreonusercontent.com/4/2/patreon-media/p/post/123456/210deacb70e940d999bf2f3022e1e2f0/WAJhIjoxZZJwIjoxfQ%3D%3D/1.png?token-time=1661644800&token-hash=123",
                Filename = "1.png",
                UrlType = PatreonCrawledUrlType.PostMedia
            };

            await crawledUrlProcessor.ProcessCrawledUrl(crawledUrl,
                Path.Combine(settings.DownloadDirectory, "UnitTesting"));

            Assert.Equal(@"c:\downloads\UnitTesting\[123456] 2020-07-07 Test Post\media_1_210deacb70e940d999bf2f3022e1e2f0.png", crawledUrl.DownloadPath);
        }
    }
}

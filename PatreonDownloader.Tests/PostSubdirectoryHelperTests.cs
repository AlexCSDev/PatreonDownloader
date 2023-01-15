using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PatreonDownloader.Implementation;
using PatreonDownloader.Implementation.Enums;
using Xunit;

namespace PatreonDownloader.Tests
{
    public class PostSubdirectoryHelperTests
    {
        [Fact]
        public async Task CreateNameFromPattern_CrawledUrlIsProperlyFilled_ProperStringIsReturned()
        {
            PatreonCrawledUrl crawledUrl = new PatreonCrawledUrl
            {
                PostId = "123456",
                Title = "Test Post",
                PublishedAt = DateTime.Parse("07.07.2020 20:00:15"),
                Url = "http://google.com",
                Filename = "test.png",
                UrlType = PatreonCrawledUrlType.PostMedia
            };

            Assert.Equal("[123456] 2020-07-07 Test Post", PostSubdirectoryHelper.CreateNameFromPattern(crawledUrl, "[%PostId%] %PublishedAt% %PostTitle%", 100));
        }

        [Fact]
        public async Task CreateNameFromPattern_PatternIsInWrongCase_ProperStringIsReturned()
        {
            PatreonCrawledUrl crawledUrl = new PatreonCrawledUrl
            {
                PostId = "123456",
                Title = "Test Post",
                PublishedAt = DateTime.Parse("07.07.2020 20:00:15"),
                Url = "http://google.com",
                Filename = "test.png",
                UrlType = PatreonCrawledUrlType.PostMedia
            };

            Assert.Equal("[123456] 2020-07-07 Test Post", PostSubdirectoryHelper.CreateNameFromPattern(crawledUrl, "[%postId%] %PubliSHedAt% %Posttitle%", 100));
        }

        [Fact]
        public async Task CreateNameFromPattern_CrawledUrlTitleIsNull_TitleIsReplacedWithNoTitle()
        {
            PatreonCrawledUrl crawledUrl = new PatreonCrawledUrl
            {
                PostId = "123456",
                Title = null,
                PublishedAt = DateTime.Parse("07.07.2020 20:00:15"),
                Url = "http://google.com",
                Filename = "test.png",
                UrlType = PatreonCrawledUrlType.PostMedia
            };

            Assert.Equal("[123456] 2020-07-07 No Title", PostSubdirectoryHelper.CreateNameFromPattern(crawledUrl, "[%PostId%] %PublishedAt% %PostTitle%", 100));
        }
    }
}
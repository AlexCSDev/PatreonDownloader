using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Moq;
using PatreonDownloader.Engine;
using PatreonDownloader.Engine.Helpers;
using PatreonDownloader.Engine.Stages.Crawling;
using PatreonDownloader.Engine.Stages.Downloading;
using PatreonDownloader.Interfaces.Models;
using PatreonDownloader.Tests.Resources;
using Xunit;

namespace PatreonDownloader.Tests
{
    public class DirectDownloaderTests
    {
        [Fact]
        public async void Download_MultipleFilesWithTheSameName_RenamesFiles()
        {
            string moqPathPassed = null;
            Mock<IWebDownloader> webDownloaderMock = new Mock<IWebDownloader>(MockBehavior.Strict);
            webDownloaderMock.Setup(x => x.DownloadFile(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask)
                .Callback<string, string>((url, path) => { moqPathPassed = path; });

            Mock<IRemoteFilenameRetriever> remoteFilenameRetrieverMock = new Mock<IRemoteFilenameRetriever>(MockBehavior.Strict);
            remoteFilenameRetrieverMock.Setup(x => x.RetrieveRemoteFileName(It.IsAny<string>()))
                .ReturnsAsync("untitled.jpeg");

            DirectDownloader directDownloader = new DirectDownloader(webDownloaderMock.Object, remoteFilenameRetrieverMock.Object);

            CrawledUrl crawledUrl = new CrawledUrl();
            crawledUrl.PostId = 123456;
            crawledUrl.Url = "http://test.com/untitled.jpg";
            crawledUrl.Filename = "untitled.jpeg";
            crawledUrl.UrlType = CrawledUrlType.PostFile;

            await directDownloader.BeforeStart();
            await directDownloader.Download(crawledUrl, "c:\\testpath");

            string passedPath1 = moqPathPassed;
            await directDownloader.Download(crawledUrl, "c:\\testpath");
            string passedPath2 = moqPathPassed;
            await directDownloader.Download(crawledUrl, "c:\\testpath");
            string passedPath3 = moqPathPassed;

            Assert.Equal("c:\\testpath\\123456_post_untitled.jpeg", passedPath1);
            Assert.Equal("c:\\testpath\\123456_post_untitled_2.jpeg", passedPath2);
            Assert.Equal("c:\\testpath\\123456_post_untitled_3.jpeg", passedPath3);
        }
    }
}

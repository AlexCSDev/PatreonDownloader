using System;
using System.Net.Http;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using PatreonDownloader.Engine;
using PatreonDownloader.Engine.Exceptions;
using PatreonDownloader.Engine.Stages.Crawling;
using PatreonDownloader.Tests.Resources;
using PuppeteerSharp;
using Xunit;

namespace PatreonDownloader.Tests
{
    public class CampaignIdRetrieverTests
    {
        [Fact]
        public async void RetrieveCampaignId_ValidResponse_ReturnsCorrectId()
        {
            Mock<IWebDownloader> webDownloaderMock = new Mock<IWebDownloader>(MockBehavior.Strict);
            webDownloaderMock.Setup(x => x.DownloadString(It.IsAny<string>()))
                .ReturnsAsync(EmbeddedFileReader.ReadEmbeddedFile<CampaignIdRetrieverTests>("CampaignIdRetriever.ValidResponse.json"));

            CampaignIdRetriever campaignIdRetriever = new CampaignIdRetriever(webDownloaderMock.Object);

            long campaignId = await campaignIdRetriever.RetrieveCampaignId("testurl");

            Assert.Equal(3216549870, campaignId);
        }

        [Fact]
        public async Task RetrieveCampaignId_PatreonDownloaderException_ThrowsException()
        {
            Mock<IWebDownloader> webDownloaderMock = new Mock<IWebDownloader>(MockBehavior.Strict);

            webDownloaderMock.Setup(x => x.DownloadString(It.IsAny<string>()))
                .Throws<PatreonDownloaderException>();

            CampaignIdRetriever campaignIdRetriever = new CampaignIdRetriever(webDownloaderMock.Object);

            await Assert.ThrowsAsync<PatreonDownloaderException>(async () => await campaignIdRetriever.RetrieveCampaignId("testurl"));
        }

        [Fact]
        public async void RetrieveCampaignId_RequestUrlDoesNotContainId_ReturnsMinusOne()
        {
            Mock<IWebDownloader> webDownloaderMock = new Mock<IWebDownloader>(MockBehavior.Strict);
            webDownloaderMock.Setup(x => x.DownloadString(It.IsAny<string>()))
                .ReturnsAsync(EmbeddedFileReader.ReadEmbeddedFile<CampaignIdRetrieverTests>("CampaignIdRetriever.InvalidResponse.json"));

            CampaignIdRetriever campaignIdRetriever = new CampaignIdRetriever(webDownloaderMock.Object);

            long campaignId = await campaignIdRetriever.RetrieveCampaignId("testurl");

            Assert.Equal(-1, campaignId);
        }

        [Fact]
        public async void RetrieveCampaignId_UrlIsNull_ThrowsArgumentException()
        {
            Mock<IWebDownloader> webDownloaderMock = new Mock<IWebDownloader>(MockBehavior.Strict);

            CampaignIdRetriever campaignIdRetriever = new CampaignIdRetriever(webDownloaderMock.Object);

            await Assert.ThrowsAsync<ArgumentException>(async () => await campaignIdRetriever.RetrieveCampaignId(null));
        }
    }
}

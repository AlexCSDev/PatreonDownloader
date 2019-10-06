using System;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using PatreonDownloader.Wrappers.Browser;
using PuppeteerSharp;
using Xunit;

namespace PatreonDownloader.Tests
{
    //TODO: Move mock setup into separate method/class
    public class CampaignIdRetrieverTests
    {
        [Fact]
        public async void RetrieveCampaignId_ValidResponse_ReturnsCorrectId()
        {
            Mock<IWebRequest> webRequestMock = new Mock<IWebRequest>(MockBehavior.Strict);
            webRequestMock.Setup(x => x.Url)
                .Returns(
                    "https://www.patreon.com/api/posts?sort=-published_at&filter[campaign_id]=3216549870&filter[is_draft]=false&filter[contains_exclusive_posts]=true&include=recent_comments.commenter.campaign.null%2Crecent_comments.commenter.flairs.campaign%2Crecent_comments.parent%2Crecent_comments.post%2Crecent_comments.first_reply.commenter.campaign.null%2Crecent_comments.first_reply.parent%2Crecent_comments.first_reply.post%2Crecent_comments.on_behalf_of_campaign.null%2Crecent_comments.first_reply.on_behalf_of_campaign.null&fields[comment]=body%2Ccreated%2Cdeleted_at%2Cis_by_patron%2Cis_by_creator%2Cvote_sum%2Ccurrent_user_vote%2Creply_count&fields[post]=comment_count&fields[user]=image_url%2Cfull_name%2Curl&fields[flair]=image_tiny_url%2Cname&json-api-use-default-includes=false&json-api-version=1.0");

            Mock<IWebResponse> webResponseMock = new Mock<IWebResponse>(MockBehavior.Strict);
            webResponseMock.Setup(x => x.TextAsync())
                .ReturnsAsync("Test data");
            Mock<IWebPage> pageMock = new Mock<IWebPage>(MockBehavior.Strict);
            pageMock.Setup(x=>x.GoToAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<WaitUntilNavigation[]>()))
                .ReturnsAsync((string url, int? timeout, WaitUntilNavigation waitUntil) => webResponseMock.Object);

            pageMock.Setup(x => x.WaitForRequestAsync(It.IsAny<Func<Request,bool>>(),
                    It.IsAny<WaitForOptions>()))
                .ReturnsAsync(webRequestMock.Object);

            pageMock.Setup(x => x.CloseAsync(It.IsAny<PageCloseOptions>()))
                .Returns(() => Task.Run(() => {}));

            Mock<IWebBrowser> browserMock = new Mock<IWebBrowser>(MockBehavior.Strict);
            browserMock.Setup(x => x.NewPageAsync())
                .ReturnsAsync(pageMock.Object);

            CampaignIdRetriever campaignIdRetriever = new CampaignIdRetriever(browserMock.Object);

            long campaignId = await campaignIdRetriever.RetrieveCampaignId("testurl");

            Assert.Equal(3216549870, campaignId);
        }

        [Fact]
        public async void RetrieveCampaignId_RequestUrlDoesNotContainId_ReturnsMinusOne()
        {
            Mock<IWebRequest> webRequestMock = new Mock<IWebRequest>(MockBehavior.Strict);
            webRequestMock.Setup(x => x.Url)
                .Returns(
                    "https://www.patreon.com/api/posts?sort=-published_at&filter[is_draft]=false&filter[contains_exclusive_posts]=true&include=recent_comments.commenter.campaign.null%2Crecent_comments.commenter.flairs.campaign%2Crecent_comments.parent%2Crecent_comments.post%2Crecent_comments.first_reply.commenter.campaign.null%2Crecent_comments.first_reply.parent%2Crecent_comments.first_reply.post%2Crecent_comments.on_behalf_of_campaign.null%2Crecent_comments.first_reply.on_behalf_of_campaign.null&fields[comment]=body%2Ccreated%2Cdeleted_at%2Cis_by_patron%2Cis_by_creator%2Cvote_sum%2Ccurrent_user_vote%2Creply_count&fields[post]=comment_count&fields[user]=image_url%2Cfull_name%2Curl&fields[flair]=image_tiny_url%2Cname&json-api-use-default-includes=false&json-api-version=1.0");

            Mock<IWebResponse> webResponseMock = new Mock<IWebResponse>(MockBehavior.Strict);
            webResponseMock.Setup(x => x.TextAsync())
                .ReturnsAsync("Test data");
            Mock<IWebPage> pageMock = new Mock<IWebPage>(MockBehavior.Strict);
            pageMock.Setup(x => x.GoToAsync(It.IsAny<string>(), It.IsAny<int?>(), It.IsAny<WaitUntilNavigation[]>()))
                .ReturnsAsync((string url, int? timeout, WaitUntilNavigation waitUntil) => webResponseMock.Object);

            pageMock.Setup(x => x.WaitForRequestAsync(It.IsAny<Func<Request, bool>>(),
                    It.IsAny<WaitForOptions>()))
                .ReturnsAsync(webRequestMock.Object);

            pageMock.Setup(x => x.CloseAsync(It.IsAny<PageCloseOptions>()))
                .Returns(() => Task.Run(() => { }));

            Mock<IWebBrowser> browserMock = new Mock<IWebBrowser>(MockBehavior.Strict);
            browserMock.Setup(x => x.NewPageAsync())
                .ReturnsAsync(pageMock.Object);

            CampaignIdRetriever campaignIdRetriever = new CampaignIdRetriever(browserMock.Object);

            long campaignId = await campaignIdRetriever.RetrieveCampaignId("testurl");

            Assert.Equal(-1, campaignId);
        }
    }
}

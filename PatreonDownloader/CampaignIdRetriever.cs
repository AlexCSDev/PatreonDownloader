using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using PatreonDownloader.Wrappers.Browser;
using PuppeteerSharp;

namespace PatreonDownloader
{
    /// <summary>
    /// This class is used to retrieve Campaign ID from creator's posts page
    /// </summary>
    internal sealed class CampaignIdRetriever : ICampaignIdRetriever
    {
        private readonly IWebBrowser _browser;

        //TODO: Research option of parsing creator's page instead of using a browser
        public CampaignIdRetriever(IWebBrowser browser)
        {
            _browser = browser ?? throw new ArgumentNullException(nameof(browser));
        }

        /// <summary>
        /// Retrieve campaign id from supplied url
        /// </summary>
        /// <param name="url">Creator's post page url</param>
        /// <returns>Returns creator id</returns>
        public async Task<long> RetrieveCampaignId(string url) //TODO: CHECK THAT URL IS VALID
        {
            long id = -1;

            IWebPage page = await _browser.NewPageAsync();
            page.GoToAsync(url); // Missing await is an intended behavior because we await for a specific request on the next line
            IWebRequest request = await page.WaitForRequestAsync(x => x.Url.Contains("https://www.patreon.com/api/posts"));

            string urlStr = request.Url;
            int idPos = urlStr.IndexOf("filter[campaign_id]=", StringComparison.Ordinal);
            if (idPos != -1)
            {
                int startPos = idPos + "filter[campaign_id]=".Length;
                int endPos = urlStr.IndexOf("&", startPos, StringComparison.Ordinal);
                id = Convert.ToInt64(urlStr.Substring(startPos, endPos - startPos));
            }

            await page.CloseAsync();

            return id;
        }
    }
}

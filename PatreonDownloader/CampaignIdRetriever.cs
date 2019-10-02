using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using PuppeteerSharp;

namespace PatreonDownloader
{
    class CampaignIdRetriever
    {
        private Browser _browser;

        public CampaignIdRetriever(Browser browser)
        {
            _browser = browser;
        }

        public async Task<long> RetrieveCampaignId(string url)
        {
            long id = -1;

            var page = await _browser.NewPageAsync();
            page.GoToAsync(url); // Missing await is an intended behavior because we await for a specific request on the next line
            Request request = await page.WaitForRequestAsync(x => x.Url.Contains("https://www.patreon.com/api/posts"));

            string urlStr = request.Url;
            int idPos = urlStr.IndexOf("filter[campaign_id]=");
            if (idPos != -1)
            {
                int startPos = idPos + "filter[campaign_id]=".Length;
                int endPos = urlStr.IndexOf("&", startPos);
                id = Convert.ToInt64(urlStr.Substring(startPos, endPos - startPos));
            }

            await page.CloseAsync();

            return id;
        }
    }
}

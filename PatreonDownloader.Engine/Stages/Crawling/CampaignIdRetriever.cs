using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PatreonDownloader.Engine.Exceptions;

namespace PatreonDownloader.Engine.Stages.Crawling
{
    /// <summary>
    /// This class is used to retrieve Campaign ID from creator's posts page
    /// </summary>
    internal sealed class CampaignIdRetriever : ICampaignIdRetriever
    {
        private readonly IWebDownloader _webDownloader;

        //TODO: Research option of parsing creator's page instead of using a browser
        public CampaignIdRetriever(IWebDownloader webDownloader)
        {
            _webDownloader = webDownloader ?? throw new ArgumentNullException(nameof(webDownloader));
        }

        /// <summary>
        /// Retrieve campaign id from supplied url
        /// </summary>
        /// <param name="url">Creator's post page url</param>
        /// <returns>Returns creator id</returns>
        public async Task<long> RetrieveCampaignId(string url)
        {
            if(string.IsNullOrEmpty(url))
                throw new ArgumentNullException(nameof(url));

            try
            {
                string pageHtml = await _webDownloader.DownloadString(url);

                Regex regex = new Regex("\"self\": \"https:\\/\\/www\\.patreon\\.com\\/api\\/campaigns\\/(\\d+)\"");
                Match match = regex.Match(pageHtml);
                if (!match.Success)
                {
                    return -1;
                }

                return Convert.ToInt64(match.Groups[1].Value);
            }
            catch (Exception ex)
            {
                throw new PatreonDownloaderException($"Unable to retrieve campaign id: {ex.Message}", ex);
            }
        }
    }
}

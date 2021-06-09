using System.Collections.Generic;

namespace PatreonDownloader.Implementation
{
    /// <summary>
    /// Represents one crawled page with all results and link to the next page
    /// </summary>
    internal class ParsingResult
    {
        public List<PatreonCrawledUrl> CrawledUrls { get; set; }
        public string NextPage { get; set; }
    }
}

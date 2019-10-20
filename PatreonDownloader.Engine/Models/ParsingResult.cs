using System.Collections.Generic;

namespace PatreonDownloader.Engine.Models
{
    /// <summary>
    /// Represents one crawled page with all results and link to the next page
    /// </summary>
    struct ParsingResult
    {
        public List<CrawledUrl> Entries;
        public string NextPage;
    }
}

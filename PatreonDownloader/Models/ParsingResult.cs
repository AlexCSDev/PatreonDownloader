using System;
using System.Collections.Generic;
using System.Text;

namespace PatreonDownloader.Models
{
    /// <summary>
    /// Represents one crawled page with all results and link to the next page
    /// </summary>
    struct ParsingResult
    {
        public List<GalleryEntry> Entries;
        public string NextPage;
    }
}

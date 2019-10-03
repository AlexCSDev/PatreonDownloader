using System.Collections.Generic;

namespace PatreonDownloader.Models
{
    public class Gallery
    {
        public string Id { get; set; }
        public string Name { get; set; }

        public List<GalleryEntry> Entries { get; set; }
    }
}

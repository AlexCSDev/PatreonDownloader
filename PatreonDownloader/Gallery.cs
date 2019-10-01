using System;
using System.Collections.Generic;
using System.Text;

namespace PatreonDownloader
{
    public class Gallery
    {
        public string Id { get; set; }
        public string Name { get; set; }

        public ICollection<GalleryEntry> Entries { get; set; }
    }
}

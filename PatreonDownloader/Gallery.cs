using System;
using System.Collections.Generic;
using System.Text;

namespace PatreonDownloader
{
    public class Gallery
    {
        public string Id { get; set; }
        public string Name { get; set; }

        public List<GalleryEntry> Entries { get; set; }
    }
}

using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace PatreonDownloader.Models
{
    /// <summary>
    /// Represents single file and all required metadata
    /// </summary>
    public struct GalleryEntry
    {
        public string Name { get; set; }

        public string Author { get; set; }

        public string Description { get; set; }

        public string PageUrl { get; set; }

        public string DownloadUrl { get; set; }

        public string Path { get; set; }

        /*public object Clone()
        {
            return new GalleryEntry
            {
                Author = Author,
                Description = Description,
                DownloadUrl = DownloadUrl,
                Name = Name,
                PageUrl = PageUrl,
                Path = Path
            };
        }*/
    }
}

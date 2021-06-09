using System.Collections.Generic;
using System.IO;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace PatreonDownloader.Implementation
{
    public class PatreonCrawlTargetInfo : ICrawlTargetInfo
    {
        public long Id { get; set; }
        public string AvatarUrl { get; set; }
        public string CoverUrl { get; set; }
        public string Name { get; set; }

        public string SaveDirectory
        {
            get { return Name; }
        }
    }
}

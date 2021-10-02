using System;
using PatreonDownloader.Implementation.Enums;
using UniversalDownloaderPlatform.DefaultImplementations.Models;

namespace PatreonDownloader.Implementation
{
    public class PatreonCrawledUrl : CrawledUrl
    {
        public string PostId { get; set; }
        public string Title { get; set; }
        public DateTime PublishedAt { get; set; }
        public PatreonCrawledUrlType UrlType { get; set; }

        public string UrlTypeAsFriendlyString
        {
            get
            {
                switch (UrlType)
                {
                    case PatreonCrawledUrlType.Unknown:
                        return "Unknown";
                    case PatreonCrawledUrlType.PostFile:
                        return "File";
                    case PatreonCrawledUrlType.PostAttachment:
                        return "Attachment";
                    case PatreonCrawledUrlType.PostMedia:
                        return "Media";
                    case PatreonCrawledUrlType.ExternalUrl:
                        return "External Url";
                    case PatreonCrawledUrlType.CoverFile:
                        return "Cover";
                    case PatreonCrawledUrlType.AvatarFile:
                        return "Avatar";
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public object Clone()
        {
            return new PatreonCrawledUrl
            {
                PostId = PostId, 
                Url = Url, 
                Filename = Filename, 
                UrlType = UrlType, 
                Title = Title, 
                PublishedAt = PublishedAt
            };
        }
    }
}

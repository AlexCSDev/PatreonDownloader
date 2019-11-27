using System;
using System.ComponentModel;

namespace PatreonDownloader.Interfaces.Models
{
    public enum CrawledUrlType
    {
        Unknown,
        PostFile,
        PostAttachment,
        PostMedia,
        ExternalUrl,
        /*ExternalImage,
        DropboxUrl,
        GoogleDriveUrl,
        MegaUrl,
        YoutubeVideo,
        ImgurUrl,
        DirectUrl,*/
        CoverFile,
        AvatarFile
    }
    /// <summary>
    /// Represents single file and all required metadata
    /// </summary>
    public sealed class CrawledUrl : ICloneable
    {
        public long PostId { get; set; }
        public string Url { get; set; }
        public string Filename { get; set; }
        public CrawledUrlType UrlType { get; set; }

        public string UrlTypeAsFriendlyString
        {
            get
            {
                switch (UrlType)
                {
                    case CrawledUrlType.Unknown:
                        return "Unknown";
                    case CrawledUrlType.PostFile:
                        return "File";
                    case CrawledUrlType.PostAttachment:
                        return "Attachment";
                    case CrawledUrlType.PostMedia:
                        return "Media";
                    case CrawledUrlType.ExternalUrl:
                        return "External Url";
                    case CrawledUrlType.CoverFile:
                        return "Cover";
                    case CrawledUrlType.AvatarFile:
                        return "Avatar";
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        public object Clone()
        {
            return new CrawledUrl {PostId = PostId, Url = Url, Filename = Filename, UrlType = UrlType};
        }
    }
}

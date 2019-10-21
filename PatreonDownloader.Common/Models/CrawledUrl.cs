using System;

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

        public object Clone()
        {
            return new CrawledUrl {PostId = PostId, Url = Url, Filename = Filename};
        }
    }
}

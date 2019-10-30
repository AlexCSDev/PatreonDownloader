using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

// This file contains all classes used for representing deserialized json response of "posts" api endpoint
namespace PatreonDownloader.Engine.Models.JSONObjects.Posts
{
    public class Embed
    {
        [JsonProperty("description")]
        public string Description { get; set; }
        [JsonProperty("html")]
        public object Html { get; set; }
        [JsonProperty("provider")]
        public string Provider { get; set; }
        [JsonProperty("provider_url")]
        public string ProviderUrl { get; set; }
        [JsonProperty("subject")]
        public string Subject { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"Provider: {Provider}, Provider URL: {ProviderUrl}");
            sb.Append(Environment.NewLine);
            sb.Append(Environment.NewLine);
            sb.Append($"Subject: {Subject}");
            sb.Append(Environment.NewLine);
            sb.Append(Environment.NewLine);
            sb.Append($"Url: {Url}");
            sb.Append(Environment.NewLine);
            sb.Append(Environment.NewLine);
            sb.Append($"Description: {Description}");
            sb.Append(Environment.NewLine);
            sb.Append(Environment.NewLine);
            sb.Append($"Html: {Html}");
            sb.Append(Environment.NewLine);
            sb.Append(Environment.NewLine);
            return sb.ToString();
        }
    }
    public class Image
    {
        [JsonProperty("height")]
        public int? Height { get; set; }
        [JsonProperty("large_url")]
        public string LargeUrl { get; set; }
        [JsonProperty("thumb_url")]
        public string ThumbUrl { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }
        [JsonProperty("width")]
        public int? Width { get; set; }
    }

    public class PostFile
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public class RootDataAttributes
    {
        [JsonProperty("change_visibility_at")]
        public object ChangeVisibilityAt { get; set; }
        [JsonProperty("comment_count")]
        public int? CommentCount { get; set; }
        [JsonProperty("content")]
        public string Content { get; set; }
        [JsonProperty("current_user_can_delete")]
        public bool CurrentUserCanDelete { get; set; }
        [JsonProperty("current_user_can_view")]
        public bool CurrentUserCanView { get; set; }
        [JsonProperty("current_user_has_liked")]
        public bool CurrentUserHasLinked { get; set; }
        [JsonProperty("embed")]
        public Embed Embed { get; set; }
        [JsonProperty("image")]
        public Image Image { get; set; }
        [JsonProperty("is_paid")]
        public bool IsPaid { get; set; }
        [JsonProperty("like_count")]
        public int? LikeCount { get; set; }
        [JsonProperty("min_cents_pledged_to_view")]
        public int? MinCentsPledgedToView { get; set; }
        [JsonProperty("patreon_url")]
        public string PatreonUrl { get; set; }
        [JsonProperty("patron_count")]
        public int? PatronCount { get; set; }
        [JsonProperty("pledge_url")]
        public string PledgeUrl { get; set; }
        [JsonProperty("post_file")]
        public PostFile PostFile { get; set; }
        [JsonProperty("post_metadata")]
        public object PostMetadata { get; set; }
        [JsonProperty("post_type")]
        public string PostType { get; set; }
        [JsonProperty("published_at")]
        public DateTime PublishedAt { get; set; }
        [JsonProperty("teaser_text")]
        public object TeaserText { get; set; }
        [JsonProperty("title")]
        public string Title { get; set; }
        [JsonProperty("upgrade_url")]
        public string UpgradeUrl { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }
        [JsonProperty("was_posted_by_campaign_owner")]
        public bool WasPostedByCampaignOwner { get; set; }
    }

    public class AccessRules
    {
        [JsonProperty("data")]
        public List<Data> Data { get; set; }
    }

    public class Attachments
    {
        [JsonProperty("data")]
        public List<Data> Data { get; set; }
    }

    public class Audio
    {
        [JsonProperty("data")]
        public object Data { get; set; }
    }

    public class Data
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public class CampaignLinks
    {
        [JsonProperty("related")]
        public string Related { get; set; }
    }

    public class Campaign
    {
        [JsonProperty("data")]
        public Data Data { get; set; }
        [JsonProperty("links")]
        public CampaignLinks Links { get; set; }
    }

    public class Images
    {
        [JsonProperty("data")]
        public List<Data> Data { get; set; }
    }

    public class Poll
    {
        [JsonProperty("data")]
        public object Data { get; set; }
    }

    public class UserLinks
    {
        [JsonProperty("related")]
        public string Related { get; set; }
    }

    public class User
    {
        [JsonProperty("data")]
        public Data Data { get; set; }
        [JsonProperty("links")]
        public UserLinks Links { get; set; }
    }

    public class UserDefinedTags
    {
        [JsonProperty("data")]
        public List<object> Data { get; set; }
    }

    public class RootDataRelationships
    {
        [JsonProperty("access_rules")]
        public AccessRules AccessRules { get; set; }
        [JsonProperty("attachments")]
        public Attachments Attachments { get; set; }
        [JsonProperty("audio")]
        public Audio Audio { get; set; }
        [JsonProperty("campaign")]
        public Campaign Campaign { get; set; }
        [JsonProperty("images")]
        public Images Images { get; set; }
        [JsonProperty("poll")]
        public Poll Poll { get; set; }
        [JsonProperty("user")]
        public User User { get; set; }
        [JsonProperty("user_defined_tags")]
        public UserDefinedTags UserDefinedTags { get; set; }
    }

    public class RootData
    {
        [JsonProperty("attributes")]
        public RootDataAttributes Attributes { get; set; }
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("relationships")]
        public RootDataRelationships Relationships { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
    }

    public class ImageUrls
    {
        [JsonProperty("default")]
        public string Default { get; set; }
        [JsonProperty("original")]
        public string Original { get; set; }
        [JsonProperty("thumbnail")]
        public string Thumbnail { get; set; }
    }

    public class Dimensions
    {
        [JsonProperty("h")]
        public int? Height { get; set; }
        [JsonProperty("w")]
        public int? Width { get; set; }
    }

    public class Metadata
    {
        [JsonProperty("dimensions")]
        public Dimensions Dimensions { get; set; }
    }

    public class IncludedAttributes
    {
        [JsonProperty("full_name")]
        public string FullName { get; set; }
        [JsonProperty("image_url")]
        public string ImageUrl { get; set; }
        [JsonProperty("url")]
        public string Url { get; set; }
        [JsonProperty("avatar_photo_url")]
        public string AvatarPhotoUrl { get; set; }
        [JsonProperty("earnings_visibility")]
        public string EarningsVisibility { get; set; }
        [JsonProperty("is_monthly")]
        public bool? IsMonthly { get; set; }
        [JsonProperty("is_nsfw")]
        public bool? IsNsfw { get; set; }
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("show_audio_post_download_links")]
        public bool? ShowAudioPostDownloadLinks { get; set; }
        [JsonProperty("download_url")]
        public string DownloadUrl { get; set; }
        [JsonProperty("file_name")]
        public string FileName { get; set; }
        [JsonProperty("image_urls")]
        public ImageUrls ImageUrls { get; set; }
        [JsonProperty("metadata")]
        public Metadata Metadata { get; set; }
        [JsonProperty("access_rule_type")]
        public string AccessRuleType { get; set; }
        [JsonProperty("amount_cents")]
        public object AmountCents { get; set; }
        [JsonProperty("post_count")]
        public int? PostCount { get; set; }
    }

    public class Tier
    {
        [JsonProperty("data")]
        public object Data { get; set; }
    }

    public class IncludedRelationships
    {
        [JsonProperty("tier")]
        public Tier Tier { get; set; }
    }

    public class Included
    {
        [JsonProperty("attributes")]
        public IncludedAttributes Attributes { get; set; }
        [JsonProperty("id")]
        public string Id { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("relationships")]
        public IncludedRelationships Relationships { get; set; }
    }

    public class RootLinks
    {
        [JsonProperty("next")]
        public string Next { get; set; }
    }

    public class Cursors
    {
        [JsonProperty("next")]
        public string Next { get; set; }
    }

    public class Pagination
    {
        [JsonProperty("cursors")]
        public Cursors Cursors { get; set; }
        [JsonProperty("total")]
        public int? Total { get; set; }
    }

    public class Meta
    {
        [JsonProperty("pagination")]
        public Pagination Pagination { get; set; }
    }

    public class Root
    {
        [JsonProperty("data")]
        public List<RootData> Data { get; set; }
        [JsonProperty("included")]
        public List<Included> Included { get; set; }
        [JsonProperty("links")]
        public RootLinks Links { get; set; }

        [JsonProperty("meta")]
        public Meta Meta { get; set; }
    }
}

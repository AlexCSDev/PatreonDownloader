using PatreonDownloader;
using System;
using System.Collections.Generic;
using System.Text;

namespace PatreonDownloader
{
    public class Image
    {
        public int? height { get; set; }
        public string large_url { get; set; }
        public string thumb_url { get; set; }
        public string url { get; set; }
        public int? width { get; set; }
    }

    public class PostFile
    {
        public string name { get; set; }
        public string url { get; set; }
    }

    public class Attributes
    {
        public object change_visibility_at { get; set; }
        public int? comment_count { get; set; }
        public string content { get; set; }
        public bool current_user_can_delete { get; set; }
        public bool current_user_can_view { get; set; }
        public bool current_user_has_liked { get; set; }
        public object embed { get; set; }
        public Image image { get; set; }
        public bool is_paid { get; set; }
        public int? like_count { get; set; }
        public int? min_cents_pledged_to_view { get; set; }
        public string patreon_url { get; set; }
        public int? patron_count { get; set; }
        public string pledge_url { get; set; }
        public PostFile post_file { get; set; }
        public object post_metadata { get; set; }
        public string post_type { get; set; }
        public DateTime published_at { get; set; }
        public object teaser_text { get; set; }
        public string title { get; set; }
        public string upgrade_url { get; set; }
        public string url { get; set; }
        public bool was_posted_by_campaign_owner { get; set; }
    }

    public class Datum2
    {
        public string id { get; set; }
        public string type { get; set; }
    }

    public class AccessRules
    {
        public List<Datum2> data { get; set; }
    }

    public class Attachments
    {
        public List<object> data { get; set; }
    }

    public class Audio
    {
        public object data { get; set; }
    }

    public class Data
    {
        public string id { get; set; }
        public string type { get; set; }
    }

    public class Links
    {
        public string related { get; set; }
    }

    public class Campaign
    {
        public Data data { get; set; }
        public Links links { get; set; }
    }

    public class Images
    {
        public List<object> data { get; set; }
    }

    public class Poll
    {
        public object data { get; set; }
    }

    public class Data2
    {
        public string id { get; set; }
        public string type { get; set; }
    }

    public class Links2
    {
        public string related { get; set; }
    }

    public class User
    {
        public Data2 data { get; set; }
        public Links2 links { get; set; }
    }

    public class UserDefinedTags
    {
        public List<object> data { get; set; }
    }

    public class Relationships
    {
        public AccessRules access_rules { get; set; }
        public Attachments attachments { get; set; }
        public Audio audio { get; set; }
        public Campaign campaign { get; set; }
        public Images images { get; set; }
        public Poll poll { get; set; }
        public User user { get; set; }
        public UserDefinedTags user_defined_tags { get; set; }
    }

    public class Datum
    {
        public Attributes attributes { get; set; }
        public string id { get; set; }
        public Relationships relationships { get; set; }
        public string type { get; set; }
    }

    public class ImageUrls
    {
        public string @default { get; set; }
        public string original { get; set; }
        public string thumbnail { get; set; }
    }

    public class Dimensions
    {
        public int? h { get; set; }
        public int? w { get; set; }
    }

    public class Metadata
    {
        public Dimensions dimensions { get; set; }
    }

    public class Attributes2
    {
        public string full_name { get; set; }
        public string image_url { get; set; }
        public string url { get; set; }
        public string avatar_photo_url { get; set; }
        public string earnings_visibility { get; set; }
        public bool? is_monthly { get; set; }
        public bool? is_nsfw { get; set; }
        public string name { get; set; }
        public bool? show_audio_post_download_links { get; set; }
        public string download_url { get; set; }
        public string file_name { get; set; }
        public ImageUrls image_urls { get; set; }
        public Metadata metadata { get; set; }
        public string access_rule_type { get; set; }
        public object amount_cents { get; set; }
        public int? post_count { get; set; }
    }

    public class Tier
    {
        public object data { get; set; }
    }

    public class Relationships2
    {
        public Tier tier { get; set; }
    }

    public class Included
    {
        public Attributes2 attributes { get; set; }
        public string id { get; set; }
        public string type { get; set; }
        public Relationships2 relationships { get; set; }
    }

    public class Links3
    {
        public string next { get; set; }
    }

    public class Cursors
    {
        public string next { get; set; }
    }

    public class Pagination
    {
        public Cursors cursors { get; set; }
        public int? total { get; set; }
    }

    public class Meta
    {
        public Pagination pagination { get; set; }
    }

    public class RootObject
    {
        public List<Datum> data { get; set; }
        public List<Included> included { get; set; }
        public Links3 links { get; set; }
        public Meta meta { get; set; }
    }
}

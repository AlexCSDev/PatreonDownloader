using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace PatreonDownloader
{
    public class CampaignAPIAttributes
    {
        [JsonProperty("avatar_photo_url")]
        public string AvatarUrl;

        [JsonProperty("cover_photo_url")]
        public string CoverUrl;

        [JsonProperty("name")]
        public string Name;
    }

    public class CampaignAPIData
    {
        [JsonProperty("attributes")]
        public CampaignAPIAttributes Attributes;
    }

    public class CampaignAPIRoot
    {
        [JsonProperty("data")]
        public CampaignAPIData Data;
    }
}

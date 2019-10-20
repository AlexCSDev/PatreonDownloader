using Newtonsoft.Json;

// This file contains all classes used for representing deserialized json response of "campaign" api endpoint
namespace PatreonDownloader.Engine.Models.JSONObjects.Campaign
{
    public class Attributes
    {
        [JsonProperty("avatar_photo_url")]
        public string AvatarUrl;

        [JsonProperty("cover_photo_url")]
        public string CoverUrl;

        [JsonProperty("name")]
        public string Name;
    }

    public class Data
    {
        [JsonProperty("attributes")]
        public Attributes Attributes;
    }

    public class Root
    {
        [JsonProperty("data")]
        public Data Data;
    }
}

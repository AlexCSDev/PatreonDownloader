using Newtonsoft.Json;

namespace PatreonDownloader.Implementation.Models.JSONObjects.IgnorePosts
{
    public class IgnorePost
    {
        [JsonProperty("id")]
        public string Id { get; set; }
    }
}

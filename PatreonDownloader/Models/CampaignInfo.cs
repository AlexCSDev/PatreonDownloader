using System;
using System.Collections.Generic;
using System.Text;

namespace PatreonDownloader.Models
{
    /// <summary>
    /// Contains campaign information such as avatar and cover url's and campaign's name and id
    /// </summary>
    struct CampaignInfo
    {
        public long Id { get; set; }
        public string AvatarUrl { get; set; }
        public string CoverUrl { get; set; }
        public string Name { get; set; }
    }
}

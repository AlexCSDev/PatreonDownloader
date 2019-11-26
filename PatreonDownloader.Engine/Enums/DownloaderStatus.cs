using System;
using System.Collections.Generic;
using System.Text;

namespace PatreonDownloader.Engine.Enums
{
    public enum DownloaderStatus
    {
        Ready,
        Initialization,
        RetrievingCampaignInformation,
        Crawling,
        Downloading,
        Done
    }
}

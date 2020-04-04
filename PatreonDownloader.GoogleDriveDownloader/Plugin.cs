using System;
using System.Collections.Generic;
using System.Text;
using NLog;
using PatreonDownloader.Common.Enums;
using PatreonDownloader.Common.Interfaces.Plugins;

namespace PatreonDownloader.GoogleDriveDownloader
{
    public sealed class Plugin : IPlugin
    {
        public string Name => "Google Drive Downloader";
        public string Author => "Aleksey Tsutsey";
        public string ContactInformation => "https://github.com/Megalan/PatreonDownloader";
        public PluginType PluginType => PluginType.Downloader;

        static Plugin()
        {
            if (!System.IO.File.Exists("gd_credentials.json"))
            {
                LogManager.GetCurrentClassLogger().Fatal("!!!![GOOGLE DRIVE]: gd_credentials.json not found, google drive files will not be downloaded! Refer to documentation for additional information. !!!!");
            }
        }
    }
}

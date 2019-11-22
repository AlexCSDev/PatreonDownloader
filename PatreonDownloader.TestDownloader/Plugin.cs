using System;
using PatreonDownloader.Common.Enums;
using PatreonDownloader.Common.Interfaces.Plugins;

namespace PatreonDownloader.TestDownloader
{
    public sealed class Plugin : IPlugin
    {
        public string Name => "Test Downloader";
        public string Author => "Aleksey Tsutsey";
        public string ContactInformation => "https://github.com/Megalan/PatreonDownloader";
        public PluginType PluginType => PluginType.Downloader;
    }
}

using CommandLine;

namespace PatreonDownloader.App.Models
{
    class CommandLineOptions
    {
        [Option("creator", Required = true, HelpText = "Name of the creator to download from")]
        public string CreatorName { get; set; }
        [Option("descriptions", Required = false, HelpText = "Save post descriptions", Default = false)]
        public bool SaveDescriptions { get; set; }
        [Option("embeds", Required = false, HelpText = "Save embedded content metadata", Default = false)]
        public bool SaveEmbeds { get; set; }
        [Option("json", Required = false, HelpText = "Save json data", Default = false)]
        public bool SaveJson { get; set; }

        [Option("campaign-images", Required = false, HelpText = "Download campaign's avatar and cover images", Default = false)]
        public bool SaveAvatarAndCover { get; set; }

        [Option("download-directory", Required = false, HelpText = "Directory to save all downloaded files in, default: #AppDirectory#/downloads/#CreatorName#.")]
        public string DownloadDirectory { get; set; }

        [Option("verbose", Required = false, HelpText = "Enable verbose (debug) logging", Default = false)]
        public bool Verbose { get; set; }

        [Option("no-headless", Required = false, HelpText = "Show internal browser window (disable headless mode)", Default = false)]
        public bool NoHeadless { get; set; }

        /*[Option("cookie-retriever", Required = false, HelpText = "Cookie retriever plugin to use", Default = "PuppeteerCookieRetriever")]
        public string CookieRetriever { get; set; }*/
    }
}

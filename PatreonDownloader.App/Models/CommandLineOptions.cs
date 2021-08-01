using CommandLine;
using UniversalDownloaderPlatform.Common.Enums;

namespace PatreonDownloader.App.Models
{
    class CommandLineOptions
    {
        [Option("url", Required = true, HelpText = "Url of the creator's page")]
        public string Url { get; set; }
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

        [Option("overwrite-files", Required = false, HelpText = "Overwrite already existing files (recommended if creator might have files multiple files with the same filename or makes changes to already existing posts)", Default = false)]
        public bool OverwriteFiles { get; set; }

        [Option("no-remote-size-action", Required = false, HelpText = "What to do with existing files when it is not possible to retrieve file size from the server. Possible options: ReplaceExisting, KeepExisting. --overwrite-files has priority over KeepExisting.", Default = RemoteFileSizeNotAvailableAction.KeepExisting)]
        public RemoteFileSizeNotAvailableAction NoRemoteSizeAction { get; set; }

        [Option("remote-browser-address", Required = false, HelpText = "Advanced users only. Address of the browser with remote debugging enabled. Refer to documentation for more details.")]
        public string RemoteBrowserAddress { get; set; }
    }
}

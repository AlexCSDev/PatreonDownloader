using CommandLine;
using PatreonDownloader.App.Enums;
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

        [Option("log-level", Required = false, HelpText = "Logging level. Possible options: Default, Debug, Trace. Affects both console and file logging.", Default = LogLevel.Default)]
        public LogLevel LogLevel { get; set; }

        [Option("log-save", Required = false, HelpText = "Create log files in the \"logs\" directory.", Default = false)]
        public bool SaveLogs { get; set; }

        [Option("overwrite-files", Required = false, HelpText = "Overwrite already existing files (recommended if creator might have files multiple files with the same filename or makes changes to already existing posts)", Default = false)]
        public bool OverwriteFiles { get; set; }

        [Option("no-remote-size-action", Required = false, HelpText = "What to do with existing files when it is not possible to retrieve file size from the server. Possible options: ReplaceExisting, KeepExisting. --overwrite-files has priority over KeepExisting.", Default = RemoteFileSizeNotAvailableAction.KeepExisting)]
        public RemoteFileSizeNotAvailableAction NoRemoteSizeAction { get; set; }

        [Option("remote-browser-address", Required = false, HelpText = "Advanced users only. Address of the browser with remote debugging enabled. Refer to documentation for more details.")]
        public string RemoteBrowserAddress { get; set; }

        [Option("use-sub-directories", Required = false, HelpText = "Create a new directory inside of the download directory for every post instead of placing all files into a single directory.")]
        public bool UseSubDirectories { get; set; }

        [Option("sub-directory-pattern", Required = false, HelpText = "Pattern which will be used to create a name for the sub directories if --use-sub-directories is used. Supported parameters: %PostId%, %PublishedAt%, %PostTitle%.", Default = "[%PostId%] %PublishedAt% %PostTitle%")]
        public string SubDirectoryPattern { get; set; }
        [Option("max-filename-length", Required = false, HelpText = "All names of downloaded files will be truncated so their length won't be more than specified value (excluding file extension)", Default = 100)]
        public int MaxFilenameLength { get; set; }
        [Option("filenames-fallback-to-content-type", Required = false, HelpText = "Fallback to using filename generated from url hash if the server returns file content type (extension) and all other methods have failed. Use with caution, this might result in unwanted files being created or the same files being downloaded on every run under different names.", Default = false)]
        public bool FilenamesFallbackToContentType { get; set; }
        [Option("proxy-server-address", Required = false, HelpText = "The address of proxy server to use in the following format: [<proxy-scheme>://]<proxy-host>[:<proxy-port>]. Supported protocols: http(s), socks4, socks4a, socks5.")]
        public string ProxyServerAddress { get; set; }
    }
}

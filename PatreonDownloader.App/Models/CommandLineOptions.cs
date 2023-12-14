using CommandLine;
using PatreonDownloader.App.Enums;
using System;
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

        [Option("file-exists-action", Required = false, HelpText = 
            "What to do with files already existing on the disk.\r\nPossible options:\r\n" +
            "BackupIfDifferent: Check remote file size if enabled and available. If it's different, disabled or not available then download remote file and compare it with existing file, create a backup copy of old file if they are different.\r\n" +
            "ReplaceIfDifferent: Same as BackupIfDifferent, but the backup copy of the file will not be created.\r\n" +
            "AlwaysReplace: Always replace existing file. Warning: will result in increased bandwidth usage.\r\n" +
            "KeepExisting: Always keep existing file. The most bandwidth-friendly option.",
            Default = FileExistsAction.BackupIfDifferent)]
        public FileExistsAction FileExistsAction { get; set; }

        [Option("use-legacy-file-naming", Required = false, HelpText = "Use legacy filenaming pattern (used before version 21). Not compatible with --file-exists-action BackupIfDifferent, ReplaceIfDifferent. Warning: this is compatibility option and might be removed in the future, you should not use it unless you absolutely need it.", Default = false)]
        public bool IsUseLegacyFilenaming { get; set; }

        [Option("disable-remote-file-size-check", Required = false, 
            HelpText = "Do not ask the server for the file size (if it's available) and do not use it in various pre-download checks if the file already exists on the disk. Warning: will result in increased bandwidth usage if used with --file-exists-action BackupIfDifferent, ReplaceIfDifferent, AlwaysReplace.", 
            Default = false)]
        public bool IsDisableRemoteFileSizeCheck { get; set; }

        [Option("remote-browser-address", Required = false, HelpText = "Advanced users only. Address of the browser with remote debugging enabled. Refer to documentation for more details.")]
        public string RemoteBrowserAddress { get; set; }

        [Option("use-sub-directories", Required = false, HelpText = "Create a new directory inside of the download directory for every post instead of placing all files into a single directory.")]
        public bool UseSubDirectories { get; set; }

        [Option("sub-directory-pattern", Required = false, HelpText = "Pattern which will be used to create a name for the sub directories if --use-sub-directories is used. Supported parameters: %PostId%, %PublishedAt%, %PostTitle%.", Default = "[%PostId%] %PublishedAt% %PostTitle%")]
        public string SubDirectoryPattern { get; set; }

        [Option("max-sub-directory-name-length", Required = false, HelpText = "Limits the length of the name for the subdirectories created when --use-sub-directories is used.", Default = 100)]
        public int MaxSubdirectoryNameLength { get; set; }

        [Option("max-filename-length", Required = false, HelpText = "All names of downloaded files will be truncated so their length won't be more than specified value (excluding file extension)", Default = 100)]
        public int MaxFilenameLength { get; set; }

        [Option("filenames-fallback-to-content-type", Required = false, HelpText = "Fallback to using filename generated from url hash if the server returns file content type (extension) and all other methods have failed. Use with caution, this might result in unwanted files being created or the same files being downloaded on every run under different names.", Default = false)]
        public bool FilenamesFallbackToContentType { get; set; }

        [Option("proxy-server-address", Required = false, HelpText = "The address of proxy server to use in the following format: [<proxy-scheme>://]<proxy-host>[:<proxy-port>]. Supported protocols: http(s), socks4, socks4a, socks5.")]
        public string ProxyServerAddress { get; set; }

        [Option("published-after", Required = false, HelpText = "Ignore post published before this date.")]
        public DateTime? PublishedAfter { get; set; }

        [Option("published-before", Required = false, HelpText = "Ignore post published after this date.")]
        public DateTime? PublishedBefore { get; set; }

    }
}

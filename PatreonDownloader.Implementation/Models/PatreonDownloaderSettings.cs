using System;
using System.Collections.Generic;
using System.Text;
using UniversalDownloaderPlatform.Common.Enums;
using UniversalDownloaderPlatform.Common.Helpers;
using UniversalDownloaderPlatform.Common.Interfaces.Models;
using UniversalDownloaderPlatform.DefaultImplementations.Models;
using UniversalDownloaderPlatform.PuppeteerEngine.Interfaces;

namespace PatreonDownloader.Implementation.Models
{
    public record PatreonDownloaderSettings : UniversalDownloaderPlatformSettings, IPuppeteerSettings
    {
        public bool SaveDescriptions { get; init; }

        public bool SaveEmbeds { get; init; }

        public bool SaveJson { get; init; }

        public bool SaveAvatarAndCover { get; init; }

        /// <summary>
        /// Create a new directory for every post and store files of said post in that directory
        /// </summary>
        public bool IsUseSubDirectories { get; init; }

        /// <summary>
        /// Pattern used to generate directory name if UseSubDirectories is enabled
        /// </summary>
        public string SubDirectoryPattern { get; init; }

        /// <summary>
        /// Subdirectory names will be truncated to this length
        /// </summary>
        public int MaxSubdirectoryNameLength { get; init; }

        /// <summary>
        /// Filenames will be truncated to this length
        /// </summary>
        public int MaxFilenameLength { get; init; }

        /// <summary>
        /// Fallback to using sha256 hash and Content-Type for filenames if Content-Disposition fails
        /// </summary>
        public bool FallbackToContentTypeFilenames { get; init; }

        /// <summary>
        /// Use legacy file naming pattern (without addition of media/attachment ids to filenames). NOT COMPATIBLE WITH FileExistsAction BackupIfDifferent/ReplaceIfDifferent
        /// </summary>
        public bool IsUseLegacyFilenaming { get; init; }

        public string LoginPageAddress { get { return "https://www.patreon.com/login"; } }
        public string LoginCheckAddress { get { return "https://www.patreon.com/api/badges?json-api-version=1.0&json-api-use-default-includes=false&include=[]"; } }
        public string CaptchaCookieRetrievalAddress { get { return "https://www.patreon.com/home"; } }
        public Uri RemoteBrowserAddress { get; init; }
        public bool IsHeadlessBrowser { get; init; }

        public PatreonDownloaderSettings()
        {
            SaveDescriptions = true;
            SaveEmbeds = true;
            SaveJson = true;
            SaveAvatarAndCover = true;
            IsUseSubDirectories = false;
            SubDirectoryPattern = "[%PostId%] %PublishedAt% %PostTitle%";
            FallbackToContentTypeFilenames = false;
            MaxFilenameLength = 100;
            MaxSubdirectoryNameLength = 100;
            IsUseLegacyFilenaming = false;
            IsHeadlessBrowser = true;
        }
    }
}

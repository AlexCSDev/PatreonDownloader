using System;
using System.Collections.Generic;
using System.Text;
using UniversalDownloaderPlatform.Common.Enums;
using UniversalDownloaderPlatform.Common.Helpers;
using UniversalDownloaderPlatform.Common.Interfaces.Models;
using UniversalDownloaderPlatform.DefaultImplementations.Models;

namespace PatreonDownloader.Implementation.Models
{
    public class PatreonDownloaderSettings : UniversalDownloaderPlatformSettings
    {
        private bool _saveDescriptions;
        private bool _saveEmbeds;
        private bool _saveJson;
        private bool _saveAvatarAndCover;
        private string _downloadDirectory;
        private bool _useSubDirectories;
        private string _subDirectoryPattern;
        private int _maxFilenameLength;

        public bool SaveDescriptions
        {
            get => _saveDescriptions;
            set => ConsumableSetter.Set(Consumed, ref _saveDescriptions, value);
        }

        public bool SaveEmbeds
        {
            get => _saveEmbeds;
            set => ConsumableSetter.Set(Consumed, ref _saveEmbeds, value);
        }

        public bool SaveJson
        {
            get => _saveJson;
            set => ConsumableSetter.Set(Consumed, ref _saveJson, value);
        }

        public bool SaveAvatarAndCover
        {
            get => _saveAvatarAndCover;
            set => ConsumableSetter.Set(Consumed, ref _saveAvatarAndCover, value);
        }

        /// <summary>
        /// Target directory for downloaded files. If set to null files will be downloaded into #AppDirectory#/downloads/#CreatorName#.
        /// </summary>
        public string DownloadDirectory
        {
            get => _downloadDirectory;
            set => ConsumableSetter.Set(Consumed, ref _downloadDirectory, value);
        }

        /// <summary>
        /// Create a new directory for every post and store files of said post in that directory
        /// </summary>
        public bool UseSubDirectories
        {
            get => _useSubDirectories;
            set => ConsumableSetter.Set(Consumed, ref _useSubDirectories, value);
        }

        /// <summary>
        /// Pattern used to generate directory name if UseSubDirectories is enabled
        /// </summary>
        public string SubDirectoryPattern
        {
            get => _subDirectoryPattern;
            set => ConsumableSetter.Set(Consumed, ref _subDirectoryPattern, value);
        }

        /// <summary>
        /// Filenames will be truncated to this length
        /// </summary>
        public int MaxFilenameLength
        {
            get => _maxFilenameLength;
            set => ConsumableSetter.Set(Consumed, ref _maxFilenameLength, value);
        }

        public PatreonDownloaderSettings()
        {
            _saveDescriptions = true;
            _saveEmbeds = true;
            _saveJson = true;
            _saveAvatarAndCover = true;
            _downloadDirectory = null;
            _useSubDirectories = false;
            _subDirectoryPattern = "[%PostId%] %PublishedAt% %PostTitle%";
            _maxFilenameLength = 50;
        }

        public override string ToString()
        {
            return $"SaveDescriptions={_saveDescriptions},SaveEmbeds={_saveEmbeds},SaveJson={_saveJson},SaveAvatarAndCover={_saveAvatarAndCover},DownloadDirectory={_downloadDirectory},OverwriteFiles={base.OverwriteFiles},UseSubDirectories={_useSubDirectories},MaxFilenameLength={_maxFilenameLength}";
        }
    }
}

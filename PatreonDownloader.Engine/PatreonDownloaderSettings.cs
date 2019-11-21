using System;
using System.Collections.Generic;
using System.Text;
using PatreonDownloader.Engine.Helpers;

namespace PatreonDownloader.Engine
{
    public sealed class PatreonDownloaderSettings
    {
        private bool _saveDescriptions;
        private bool _saveEmbeds;
        private bool _saveJson;
        private bool _saveAvatarAndCover;
        private string _downloadDirectory;

        /// <summary>
        /// Any attempt to set properties will result in exception if this set to true
        /// </summary>
        internal bool Consumed { get; set; }

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

        public PatreonDownloaderSettings()
        {
            _saveDescriptions = true;
            _saveEmbeds = true;
            _saveJson = true;
            _saveAvatarAndCover = true;
            _downloadDirectory = null;
        }

        public override string ToString()
        {
            return $"SaveDescriptions={_saveDescriptions},SaveEmbeds={_saveEmbeds},SaveJson={_saveJson},SaveAvatarAndCover={_saveAvatarAndCover},DownloadDirectory={_downloadDirectory}";
        }
    }
}

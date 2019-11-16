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
        private bool _downloadAvatarAndCover;

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

        public bool DownloadAvatarAndCover
        {
            get => _downloadAvatarAndCover;
            set => ConsumableSetter.Set(Consumed, ref _downloadAvatarAndCover, value);
        }

        public PatreonDownloaderSettings()
        {
            _saveDescriptions = true;
            _saveEmbeds = true;
            _saveJson = true;
            _downloadAvatarAndCover = true;
        }

        public override string ToString()
        {
            return $"SaveDescriptions={_saveDescriptions},SaveEmbeds={_saveEmbeds},SaveJson={_saveJson},DownloadAvatarAndCover={_downloadAvatarAndCover}";
        }
    }
}

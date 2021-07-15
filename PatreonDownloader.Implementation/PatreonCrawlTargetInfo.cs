using System.Collections.Generic;
using System.IO;
using UniversalDownloaderPlatform.Common.Interfaces.Models;

namespace PatreonDownloader.Implementation
{
    public class PatreonCrawlTargetInfo : ICrawlTargetInfo
    {
        private static readonly HashSet<char> InvalidFilenameCharacters;

        static PatreonCrawlTargetInfo()
        {
            InvalidFilenameCharacters = new HashSet<char>(Path.GetInvalidFileNameChars());
        }

        public long Id { get; set; }
        public string AvatarUrl { get; set; }
        public string CoverUrl { get; set; }

        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                _saveDirectory = _name;
                foreach (char c in InvalidFilenameCharacters)
                {
                    _saveDirectory = _saveDirectory.Replace(c, '_');
                }
            }
        }

        private string _saveDirectory;
        public string SaveDirectory => _saveDirectory;
    }
}

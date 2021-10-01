using System;
using System.IO;
using System.Linq;
using System.Text;
using UniversalDownloaderPlatform.Common.Enums;
using UniversalDownloaderPlatform.Common.Helpers;

namespace PatreonDownloader.Implementation
{
    internal class PatreonDirectoryPatternFormat
    {
        public static string Format(string basePath, DirectoryPatternType directoryPattern, DateTime publishAt, string title)
        {
            if (directoryPattern != DirectoryPatternType.Default)
            {
                string pusblishAt = publishAt.ToString("yyyy-MM-dd");

                if (!String.IsNullOrEmpty(title))
                {
                    var tempDir = new StringBuilder(title);

                    foreach (char invalidChar in UniversalDirectoryPatternFormat.InvalidPathChars)
                        tempDir.Replace(invalidChar.ToString(), String.Empty);

                    title = tempDir.ToString().Trim();

                    while (title.Length > 1 && title[^1] == '.')
                        title = title.Remove(title.Length - 1).Trim();
                }

                if (directoryPattern == DirectoryPatternType.PostTitle)
                    return Path.Combine(basePath, title);

                if (directoryPattern == DirectoryPatternType.PublishedAt)
                    return Path.Combine(basePath, pusblishAt);
                else if (directoryPattern == DirectoryPatternType.PublishedAtAndPostTitle)
                    return Path.Combine(basePath, (pusblishAt + " " + title).Trim());

                throw new NotSupportedException(directoryPattern.ToString());
            }

            return String.Empty;
        }
    }
}

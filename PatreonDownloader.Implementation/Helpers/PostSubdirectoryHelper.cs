using System;
using System.IO;
using System.Linq;
using System.Text;
using UniversalDownloaderPlatform.Common.Enums;
using UniversalDownloaderPlatform.Common.Helpers;

namespace PatreonDownloader.Implementation
{
    /// <summary>
    /// Helper used to generate name for post subdirectories
    /// </summary>
    internal class PostSubdirectoryHelper
    {
        /// <summary>
        /// Create a sanitized directory name based on supplied name pattern
        /// </summary>
        /// <param name="crawledUrl">Crawled url with published date, post title and post id</param>
        /// <param name="pattern">Pattern for directory name</param>
        /// <returns></returns>
        public static string CreateNameFromPattern(PatreonCrawledUrl crawledUrl, string pattern)
        {
            string postTitle = crawledUrl.Title.Trim();
            while (postTitle.Length > 1 && postTitle[^1] == '.')
                postTitle = postTitle.Remove(postTitle.Length - 1).Trim();

            string retString = pattern.ToLowerInvariant()
                .Replace("%publishedat%", crawledUrl.PublishedAt.ToString("yyyy-MM-dd"))
                .Replace("%posttitle%", postTitle)
                .Replace("%postid%", crawledUrl.PostId);

            return PathSanitizer.SanitizePath(retString);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PatreonDownloader.Helpers
{
    internal static class UrlChecker
    {
        /// <summary>
        /// Checks that url is a valid url and is not blacklisted
        /// </summary>
        /// <param name="url">Url to check</param>
        /// <param name="useBlackList">Will validation fail if url contains blacklisted string?</param>
        /// <returns></returns>
        public static bool IsValidUrl(string url, bool useBlackList = true)
        {
            Uri uriResult;
            bool validationResult = Uri.TryCreate(url, UriKind.Absolute, out uriResult) &&
                                    (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

            string[] blackList = (ConfigurationManager.Configuration["UrlBlackList"] ?? "").Split("|");
            string lowerUrl = url.ToLower();
            bool blackListResult = useBlackList && blackList.Any(x => lowerUrl.Contains(x));
            return validationResult && !blackListResult;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using NLog;
using UniversalDownloaderPlatform.Common.Exceptions;
using UniversalDownloaderPlatform.Common.Interfaces;

namespace PatreonDownloader.Implementation
{
    internal class PatreonCookieValidator : ICookieValidator
    {
        private readonly IWebDownloader _webDownloader;
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        public PatreonCookieValidator(IWebDownloader webDownloader)
        {
            _webDownloader = webDownloader ?? throw new ArgumentNullException(nameof(webDownloader));
        }

        public async Task ValidateCookies(CookieContainer cookieContainer)
        {
            if (cookieContainer == null)
                throw new ArgumentNullException(nameof(cookieContainer));

            CookieCollection cookies = cookieContainer.GetCookies(new Uri("https://patreon.com"));

            if (cookies["__cf_bm"] == null)
                throw new CookieValidationException("__cf_bm cookie not found");
            if (cookies["session_id"] == null)
                throw new CookieValidationException("session_id cookie not found");
            if (cookies["patreon_device_id"] == null)
                throw new CookieValidationException("patreon_device_id cookie not found");
            if (cookies["datadome"] == null)
            {
                //Some users reported that they don't have datadome cookie and crawling still works fine, so let's ignore that for now.
                _logger.Warn("Datadome cookie was not found. Usually this is not an issue, but if you are experiencing any issues please make sure to report it via GitHub issues page.");
                //throw new CookieValidationException("datadome cookie not found");
            }

            string apiResponse = await _webDownloader.DownloadString("https://www.patreon.com/api/current_user");

            if (apiResponse.ToLower(CultureInfo.InvariantCulture).Contains("\"status\":\"401\""))
                throw new CookieValidationException("current_user api endpoint returned 401 Unauthorized");
        }
    }
}

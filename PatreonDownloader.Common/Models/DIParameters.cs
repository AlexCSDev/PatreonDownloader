﻿using System;
using System.Net;

namespace PatreonDownloader.Common.Models
{
    /// <summary>
    /// Container for parameters/variables used by dependency injected classes
    /// </summary>
    public class DIParameters
    {
        /// <summary>
        /// Patreon/Cloudlfare cookies
        /// </summary>
        public CookieContainer CookieContainer { get; private set; }

        /// <summary>
        /// Should browser be started in headless mode
        /// </summary>
        public bool IsHeadless { get; private set; }

        /// <summary>
        /// Address of the remote browser
        /// </summary>
        public Uri RemoteBrowserAddress { get; private set; }

        public DIParameters(CookieContainer cookieContainer, bool isHeadless, Uri remoteBrowserAddress)
        {
            CookieContainer = cookieContainer;
            IsHeadless = isHeadless;
            RemoteBrowserAddress = remoteBrowserAddress;
        }
    }
}

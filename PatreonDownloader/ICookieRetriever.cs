using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using PatreonDownloader.Wrappers.Browser;

namespace PatreonDownloader
{
    internal interface ICookieRetriever
    {
        Task<CookieContainer> RetrieveCookies(string url);
    }
}

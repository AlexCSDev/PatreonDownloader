using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PatreonDownloader.Interfaces
{
    /// <summary>
    /// Interface for additional implementations of cookie retrievers
    /// </summary>
    public interface ICookieRetriever
    {
        Task Login();
        Task<CookieContainer> RetrieveCookies(string url);
    }
}

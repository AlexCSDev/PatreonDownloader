using System;
using System.Net;
using System.Threading.Tasks;

namespace PatreonDownloader.App.CookieRetriever
{
    /// <summary>
    /// Interface for additional implementations of cookie retrievers
    /// </summary>
    public interface ICookieRetriever
    {
        Task<string> GetUserAgent();
        Task<CookieContainer> RetrieveCookies(string username, string password);
    }
}

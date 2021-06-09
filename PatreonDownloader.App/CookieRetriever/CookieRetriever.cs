using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace PatreonDownloader.App.CookieRetriever
{
    internal class CookieRetriever : ICookieRetriever
    {
        private const string UserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:89.0) Gecko/20100101 Firefox/89.0";

        public async Task<string> GetUserAgent()
        {
            return UserAgent;
        }

        public async Task<CookieContainer> RetrieveCookies(string email, string password)
        {
            try
            {
                using (HttpClientHandler handler = new HttpClientHandler())
                {
                    using (HttpClient httpClient = new HttpClient(handler))
                    {
                        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
                        StringContent content = new StringContent(
                            "{\"data\": {\"attributes\": {\"email\": \"" 
                            + email +
                            "\",\"password\": \"" 
                            + password +
                            "\"},\"relationships\": {},\"type\": \"user\"}}",
                            Encoding.UTF8, "application/json");
                        HttpResponseMessage response = await httpClient.PostAsync(
                            "https://www.patreon.com/api/login?include=campaign%2Cuser_location&json-api-version=1.0",
                            content);

                        if (!response.IsSuccessStatusCode)
                            return null;

                        return handler.CookieContainer;
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}

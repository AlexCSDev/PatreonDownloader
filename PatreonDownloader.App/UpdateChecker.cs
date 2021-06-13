using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace PatreonDownloader.App
{
    internal class UpdateChecker
    {
        private readonly HttpClient _httpClient;
        private const string UpdateUrl = "https://alexcsdev.github.io/pd_version.txt";
        public UpdateChecker()
        {
            _httpClient = new HttpClient();
        }

        public async Task<(bool, string)> IsNewVersionAvailable()
        {
            string[] remoteVersionData = (await _httpClient.GetStringAsync(UpdateUrl)).Split("|");
            string remoteVersion = remoteVersionData[0];
            string message = remoteVersionData.Length > 1 ? remoteVersionData[1] : null;
            Version currentVersion = Assembly.GetEntryAssembly().GetName().Version;
            string currentVersionString = $"{currentVersion.Major}.{currentVersion.Minor}.{currentVersion.Revision}";

            return (remoteVersion != currentVersionString, !string.IsNullOrWhiteSpace(message) ? message : null);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using PatreonDownloader.Models;

namespace PatreonDownloader
{
    internal interface IWebDownloader
    {
        Task<DownloadResult> DownloadFile(string url, string path);

        Task<string> DownloadString(string url);
    }
}

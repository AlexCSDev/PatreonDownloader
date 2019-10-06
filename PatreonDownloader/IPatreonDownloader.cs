using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PatreonDownloader
{
    internal interface IPatreonDownloader
    {
        Task Download(string url);
    }
}

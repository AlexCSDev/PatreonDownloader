using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using PatreonDownloader.Common.Interfaces;

namespace PatreonDownloader.Engine
{
    internal interface IPluginManager
    {
        Task<IDownloader> GetDownloader(string url);
    }
}

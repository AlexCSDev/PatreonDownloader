using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using PatreonDownloader.Common.Interfaces;

namespace PatreonDownloader.Engine
{
    internal interface IPluginManager
    {
        /// <summary>
        /// Initialization function, called on every PatreonDownloader.Download call
        /// </summary>
        /// <returns></returns>
        Task BeforeStart();
        Task<IDownloader> GetDownloader(string url);
    }
}

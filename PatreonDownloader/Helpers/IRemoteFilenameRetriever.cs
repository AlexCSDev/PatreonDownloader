using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace PatreonDownloader.Helpers
{
    interface IRemoteFilenameRetriever
    {
        Task<string> RetrieveRemoteFileName(string url);
    }
}

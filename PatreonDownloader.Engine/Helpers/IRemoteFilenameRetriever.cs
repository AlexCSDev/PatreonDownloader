using System.Threading.Tasks;

namespace PatreonDownloader.Engine.Helpers
{
    interface IRemoteFilenameRetriever
    {
        Task<string> RetrieveRemoteFileName(string url);
    }
}

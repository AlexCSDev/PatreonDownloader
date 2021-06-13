using System.Threading.Tasks;

namespace PatreonDownloader.Implementation.Interfaces
{
    interface IRemoteFilenameRetriever
    {
        Task<string> RetrieveRemoteFileName(string url);
    }
}

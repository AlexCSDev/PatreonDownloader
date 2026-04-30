using System.Threading.Tasks;

namespace PatreonDownloader.Implementation.Interfaces
{
    internal interface IProcessRunner
    {
        Task<ProcessRunResult> RunAsync(string fileName, string arguments);
    }
}

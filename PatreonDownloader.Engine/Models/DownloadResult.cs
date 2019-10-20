namespace PatreonDownloader.Engine.Models
{
    enum DownloadResult
    {
        Unknown,
        Success,
        FileExists,
        HttpError,
        IOError,
        UnknownError
    }
}

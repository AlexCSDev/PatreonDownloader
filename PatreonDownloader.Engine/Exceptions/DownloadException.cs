using System;
using System.Collections.Generic;
using System.Text;

namespace PatreonDownloader.Engine.Exceptions
{
    /// <summary>
    /// Thrown when unrecoverable error is encountered during download process
    /// </summary>
    public sealed class DownloadException : PatreonDownloaderException
    {
        public DownloadException() { }
        public DownloadException(string message) : base(message) { }
        public DownloadException(string message, Exception innerException) : base(message, innerException) { }
    }
}

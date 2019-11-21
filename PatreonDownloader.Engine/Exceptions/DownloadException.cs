using System;
using System.Collections.Generic;
using System.Text;

namespace PatreonDownloader.Engine.Exceptions
{
    public class DownloadException : PatreonDownloaderException
    {
        public DownloadException() { }
        public DownloadException(string message) : base(message) { }
        public DownloadException(string message, Exception innerException) : base(message, innerException) { }
    }
}

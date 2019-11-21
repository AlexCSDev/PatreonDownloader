using System;
using System.Collections.Generic;
using System.Text;

namespace PatreonDownloader.Engine.Exceptions
{
    public class PatreonDownloaderException : Exception
    {
        public PatreonDownloaderException() { }
        public PatreonDownloaderException(string message) : base(message) { }
        public PatreonDownloaderException(string message, Exception innerException) : base(message, innerException) { }
    }
}

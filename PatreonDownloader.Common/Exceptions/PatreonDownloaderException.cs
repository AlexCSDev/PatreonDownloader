using System;

namespace PatreonDownloader.Common.Exceptions
{
    /// <summary>
    /// Base class for all PatreonDownloader exceptions
    /// Thrown when there are no more specific exception is available
    /// </summary>
    public class PatreonDownloaderException : Exception
    {
        public PatreonDownloaderException() { }
        public PatreonDownloaderException(string message) : base(message) { }
        public PatreonDownloaderException(string message, Exception innerException) : base(message, innerException) { }
    }
}

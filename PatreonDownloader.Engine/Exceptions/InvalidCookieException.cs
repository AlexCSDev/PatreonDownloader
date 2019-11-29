using System;
using System.Collections.Generic;
using System.Text;

namespace PatreonDownloader.Engine.Exceptions
{
    /// <summary>
    /// Thrown when supplied cookies are invalid or incomplete
    /// </summary>
    public sealed class CookieValidationException : PatreonDownloaderException
    {
        public CookieValidationException() { }
        public CookieValidationException(string message) : base(message) { }
        public CookieValidationException(string message, Exception innerException) : base(message, innerException) { }
    }
}

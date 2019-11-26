using System;
using System.Collections.Generic;
using System.Text;

namespace PatreonDownloader.Engine.Events
{
    public class FileDownloadedEventArgs : EventArgs
    {
        private readonly string _url;
        private readonly int _totalFiles;
        private readonly bool _success;
        private readonly string _errorMessage;

        public string Url => _url;

        public int TotalFiles => _totalFiles;

        public bool Success => _success;

        public string ErrorMessage => _errorMessage;

        public FileDownloadedEventArgs(string url, int totalFiles, bool success = true, string errorMessage = null)
        {
            _success = success;
            _url = url ?? throw new ArgumentNullException(nameof(url), "Value could not be null");
            _totalFiles = totalFiles > 0
                ? totalFiles
                : throw new ArgumentOutOfRangeException(nameof(totalFiles), "Value cannot be lower than 1");

            if (!success)
                _errorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage), "Value could not be null if success is false");
        }
    }
}

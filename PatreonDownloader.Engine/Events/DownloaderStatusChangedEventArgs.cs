using System;
using System.Collections.Generic;
using System.Text;
using PatreonDownloader.Engine.Enums;

namespace PatreonDownloader.Engine.Events
{
    public sealed class DownloaderStatusChangedEventArgs : EventArgs
    {
        private readonly DownloaderStatus _status;

        public DownloaderStatus Status => _status;

        public DownloaderStatusChangedEventArgs(DownloaderStatus status)
        {
            _status = status;
        }
    }
}

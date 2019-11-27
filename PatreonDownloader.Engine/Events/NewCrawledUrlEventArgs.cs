using System;
using System.Collections.Generic;
using System.Text;
using PatreonDownloader.Interfaces.Models;

namespace PatreonDownloader.Engine.Events
{
    public sealed class NewCrawledUrlEventArgs : EventArgs
    {
        private readonly CrawledUrl _crawledUrl;

        public CrawledUrl CrawledUrl => _crawledUrl;

        public NewCrawledUrlEventArgs(CrawledUrl crawledUrl)
        {
            _crawledUrl = crawledUrl ?? throw new ArgumentNullException(nameof(crawledUrl));
        }
    }
}

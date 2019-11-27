using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Text;

namespace PatreonDownloader.Engine.Events
{
    public enum CrawlerMessageType
    {
        Info,
        Warning,
        Error
    }
    public sealed class CrawlerMessageEventArgs : EventArgs
    {
        private readonly CrawlerMessageType _messageType;
        private readonly string _message;
        private readonly long _postId;

        public CrawlerMessageType MessageType => _messageType;
        public string Message => _message;

        public long PostId => _postId;

        public CrawlerMessageEventArgs(CrawlerMessageType messageType, string message, long postId = -1)
        {
            _messageType = messageType;
            _message = message ?? throw new ArgumentNullException(nameof(message));
            _postId = postId;
        }
    }
}

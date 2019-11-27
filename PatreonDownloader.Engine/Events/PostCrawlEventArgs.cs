using System;
using System.Collections.Generic;
using System.Text;

namespace PatreonDownloader.Engine.Events
{
    public sealed class PostCrawlEventArgs : EventArgs
    {
        private readonly long _postId;
        private readonly bool _success;
        private readonly string _errorMessage;

        public long PostId => _postId;
        public bool Success => _success;
        public string ErrorMessage => _errorMessage;

        public PostCrawlEventArgs(long postId, bool success, string errorMessage = null)
        {
            _postId = postId > 0 ? postId : throw new ArgumentOutOfRangeException(nameof(postId), "Value cannot be lower than 1");
            _success = success;
            if (!success)
                _errorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage), "Value could not be null if success is false");
        }
    }
}

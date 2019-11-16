using System;
using System.Collections.Generic;
using System.Text;

namespace PatreonDownloader.Engine.Exceptions.WebDownloaderExceptions
{
    class FileAlreadyExistsException : Exception
    {

        private readonly string _path;
        public string Path
        {
            get { return _path; }
        }

        public FileAlreadyExistsException(string path)
        {
            _path = path;
        }
    }
}

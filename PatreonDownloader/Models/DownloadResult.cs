using System;
using System.Collections.Generic;
using System.Text;

namespace PatreonDownloader.Models
{
    enum DownloadResult
    {
        Unknown,
        Success,
        FileExists,
        HttpError,
        IOError,
        UnknownError
    }
}

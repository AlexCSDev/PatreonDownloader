using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PatreonDownloader.Common.Interfaces
{
    public interface ICaptchaSolver
    {
        Task<CookieCollection> SolveCaptcha(string url);
    }
}

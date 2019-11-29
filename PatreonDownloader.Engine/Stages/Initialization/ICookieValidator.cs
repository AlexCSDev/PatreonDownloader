using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PatreonDownloader.Engine.Stages.Initialization
{
    internal interface ICookieValidator
    {
        Task ValidateCookies(CookieContainer cookieContainer);
    }
}

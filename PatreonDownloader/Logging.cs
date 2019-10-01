using System;
using System.Collections.Generic;
using System.Text;
using NLog;

namespace PatreonDownloader
{
    internal static class Log
    {
        public static Logger Instance { get; private set; }
        static Log()
        {
            LogManager.ReconfigExistingLoggers();

            Instance = LogManager.GetLogger("PatreonDownloader");
        }
    }
}

using System;
using System.Collections.Generic;
using System.Text;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace PatreonDownloader.App
{
    internal static class NLogManager
    {
        public static void ReconfigureNLog(Enums.LogLevel logLevel = Enums.LogLevel.Default, bool saveLogs = false)
        {
            LoggingConfiguration configuration = new LoggingConfiguration();
            ColoredConsoleTarget consoleTarget = new ColoredConsoleTarget("console")
            {
                Layout = "${longdate} ${uppercase:${level}} ${message}"
            };
            configuration.AddTarget(consoleTarget);

            FileTarget fileTarget = new FileTarget("file")
            {
                FileName = "${basedir}/logs/${shortdate}.log",
                Layout = "${longdate} ${uppercase:${level}} [${logger}] ${message}"
            };
            configuration.AddTarget(fileTarget);

            LogLevel nlogLogLevel = LogLevel.Info;
            switch (logLevel)
            {
                case Enums.LogLevel.Debug:
                    nlogLogLevel = LogLevel.Debug;
                    break;
                case Enums.LogLevel.Trace:
                    nlogLogLevel = LogLevel.Trace;
                    break;
            }

            configuration.AddRule(nlogLogLevel, LogLevel.Fatal, consoleTarget, nlogLogLevel != LogLevel.Info ? "*" : "PatreonDownloader.App.*");
            if(saveLogs)
                configuration.AddRule(nlogLogLevel, LogLevel.Fatal, fileTarget, nlogLogLevel != LogLevel.Info ? "*" : "PatreonDownloader.App.*");
            //configuration.AddRule(debug ? LogLevel.Debug : LogLevel.Info, LogLevel.Fatal, consoleTarget, debug ? "*" : "PatreonDownloader.PuppeteerEngine.*");
            //configuration.AddRuleForAllLevels(fileTarget);

            LogManager.Configuration = configuration;
        }
    }
}

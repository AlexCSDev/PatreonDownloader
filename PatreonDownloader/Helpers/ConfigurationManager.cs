using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace PatreonDownloader.Helpers
{
    internal static class ConfigurationManager
    {
        public static IConfiguration Configuration;
        static ConfigurationManager()
        {
            //Init config
            Configuration = new ConfigurationBuilder()
                .AddJsonFile("settings.json", true, true)
                .Build();
        }
    }
}

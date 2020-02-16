using Microsoft.Extensions.Configuration;

namespace PatreonDownloader.Engine.Helpers
{
    //TODO: Move all configuration-related code to app
    internal static class ConfigurationManager
    {
        public static IConfiguration Configuration;
        static ConfigurationManager()
        {
            //Init config
            Configuration = new ConfigurationBuilder()
                .AddJsonFile("settings.json", true, false)
                .Build();
        }
    }
}

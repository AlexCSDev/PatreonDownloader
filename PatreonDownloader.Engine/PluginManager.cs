using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using NLog;
using PatreonDownloader.Common.Enums;
using PatreonDownloader.Common.Interfaces;
using PatreonDownloader.Common.Interfaces.Plugins;
using PatreonDownloader.Engine.Helpers;

namespace PatreonDownloader.Engine
{
    internal sealed class PluginManager : IPluginManager
    {
        private static string _pluginsDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "plugins");
        private readonly Logger _logger = LogManager.GetCurrentClassLogger();

        private readonly List<IDownloader> _downloaders;
        public PluginManager()
        {
            if (!Directory.Exists(_pluginsDirectory))
                Directory.CreateDirectory(_pluginsDirectory);

            _downloaders = new List<IDownloader>();
            IEnumerable<string> files = Directory.EnumerateFiles(_pluginsDirectory);
            foreach (string file in files)
            {
                try
                {
                    if (!file.EndsWith(".dll"))
                        continue;

                    string filename = Path.GetFileName(file);

                    Assembly assembly = Assembly.LoadFrom(file);

                    Type[] types = assembly.GetTypes();

                    Type pluginType = types.SingleOrDefault(x => x.GetInterfaces().Contains(typeof(IPlugin)));
                    if (pluginType == null)
                        continue;

                    _logger.Debug($"New plugin found: {filename}");

                    IPlugin plugin = Activator.CreateInstance(pluginType) as IPlugin;
                    if (plugin == null)
                    {
                        _logger.Error($"Invalid plugin {filename}: IPlugin interface could not be created");
                        continue;
                    }

                    if (plugin.PluginType == PluginType.Downloader)
                    {
                        Type downloaderType =
                            types.SingleOrDefault(x => x.GetInterfaces().Contains(typeof(IDownloader)));
                        if (downloaderType == null)
                        {
                            _logger.Error(
                                $"Invalid plugin {filename}: Plugin type is downloader but no class implementing IDownloader found");
                            continue;
                        }

                        IDownloader downloader = Activator.CreateInstance(downloaderType) as IDownloader;
                        _downloaders.Add(downloader);
                    }

                    _logger.Info(
                        $"Loaded plugin: {plugin.Name}"); // {assembly.GetName().Version} by {plugin.Author} ({plugin.ContactInformation})
                }
                catch (Exception ex)
                {
                    _logger.Error($"Unable to load plugin {file}: {ex}");
                }
            }
        }

        /// <summary>
        /// Get downloader for url
        /// </summary>
        /// <param name="url">File url</param>
        /// <returns>IDownloader if downloader is found, null if not.</returns>
        public async Task<IDownloader> GetDownloader(string url)
        {
            if (string.IsNullOrEmpty(url))
                return null;

            foreach (IDownloader downloader in _downloaders)
            {
                if (await downloader.IsSupportedUrl(url))
                    return downloader;
            }

            return null;
        }
    }
}

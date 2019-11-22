using System;
using System.Collections.Generic;
using System.Text;
using PatreonDownloader.Common.Enums;

namespace PatreonDownloader.Common.Interfaces.Plugins
{
    public interface IPlugin
    {
        string Name { get; }
        string Author { get; }
        string ContactInformation { get; }
        PluginType PluginType { get; }
    }
}

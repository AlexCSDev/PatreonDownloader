using System;
using System.Collections.Generic;
using System.Text;
using Ninject.Modules;
using PatreonDownloader.Engine;
using PatreonDownloader.Implementation.Interfaces;
using PatreonDownloader.Implementation.Models;
using UniversalDownloaderPlatform.Common.Interfaces;
using UniversalDownloaderPlatform.Common.Interfaces.Models;
using UniversalDownloaderPlatform.Common.Interfaces.Plugins;
using UniversalDownloaderPlatform.DefaultImplementations;
using UniversalDownloaderPlatform.DefaultImplementations.Interfaces;
using UniversalDownloaderPlatform.DefaultImplementations.Models;

namespace PatreonDownloader.Implementation
{
    public class PatreonDownloaderModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IRemoteFileSizeChecker>().To<RemoteFileSizeChecker>().InSingletonScope();
            Bind<IWebDownloader>().To<WebDownloader>().InSingletonScope();
            Bind<IRemoteFilenameRetriever>().To<RemoteFilenameRetriever>().InSingletonScope();
            Bind<ICrawlTargetInfoRetriever>().To<PatreonCrawlTargetInfoRetriever>().InSingletonScope();
            Bind<ICrawledUrlProcessor>().To<PatreonCrawledUrlProcessor>();
            Bind<IPageCrawler>().To<PatreonPageCrawler>();
            Bind<IPlugin>().To<PatreonDefaultPlugin>().WhenInjectedInto<IPluginManager>();
            Bind<IUniversalDownloaderPlatformSettings>().To<PatreonDownloaderSettings>();
        }
    }
}

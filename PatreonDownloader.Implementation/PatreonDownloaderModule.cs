using System;
using System.Collections.Generic;
using System.Text;
using Ninject;
using Ninject.Modules;
using PatreonDownloader.Engine;
using PatreonDownloader.Implementation.Interfaces;
using PatreonDownloader.Implementation.Models;
using PatreonDownloader.PuppeteerEngine;
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
            Bind<IWebDownloader>().To<PatreonWebDownloader>().InSingletonScope();
            Bind<IRemoteFilenameRetriever>().To<PatreonRemoteFilenameRetriever>().InSingletonScope();
            Bind<ICrawlTargetInfoRetriever>().To<PatreonCrawlTargetInfoRetriever>().InSingletonScope();
            Bind<ICrawledUrlProcessor>().To<PatreonCrawledUrlProcessor>().InSingletonScope();
            Bind<IPageCrawler>().To<PatreonPageCrawler>().InSingletonScope();
            Bind<IPlugin>().To<PatreonDefaultPlugin>().WhenInjectedInto<IPluginManager>();
            Bind<IUniversalDownloaderPlatformSettings>().To<PatreonDownloaderSettings>();
            Bind<ICookieValidator>().To<PatreonCookieValidator>().InSingletonScope();
        }
    }
}

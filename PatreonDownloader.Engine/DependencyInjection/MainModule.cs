using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Ninject;
using Ninject.Modules;
using Ninject.Extensions.Conventions;
using PatreonDownloader.Common.Interfaces.Plugins;
using PatreonDownloader.Engine.Helpers;
using PatreonDownloader.Engine.Stages.Crawling;
using PatreonDownloader.Engine.Stages.Downloading;
using PatreonDownloader.Engine.Stages.Initialization;

namespace PatreonDownloader.Engine.DependencyInjection
{
    public class MainModule : NinjectModule
    {
        public override void Load()
        {
            Bind<IPluginManager>().To<PluginManager>().InSingletonScope();
            Bind<IWebDownloader>().To<WebDownloader>().InSingletonScope();
            Bind<ICampaignIdRetriever>().To<CampaignIdRetriever>().InSingletonScope();
            Bind<ICampaignInfoRetriever>().To<CampaignInfoRetriever>().InSingletonScope();
            Bind<ICookieValidator>().To<CookieValidator>().InSingletonScope();
            Bind<IPageCrawler>().To<PageCrawler>();
            Bind<IDownloadManager>().To<DownloadManager>();
            Bind<IRemoteFilenameRetriever>().To<RemoteFilenameRetriever>();
            Bind<IPlugin>().To<DefaultPlugin>().WhenInjectedInto<IPluginManager>(); //inject default plugin into plugin manager

            Kernel.Load("PatreonDownloader.PuppeteerEngine.dll");
        }
    }
}


// -----------------------------------------------------------------------
// <copyright file="App.cs" company="private project">
// <legal>Copyright (c) MB, February 2025</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent;

using System.Configuration;
using System.Windows;

using FtpDiligent.Views;
using Prism.Ioc;
using Prism.Unity;

public partial class App : PrismApplication
{
    protected override Window CreateShell()
    {
        FtpDispatcherGlobals.IoC = Container;
        return Container.Resolve<MainWindow>();
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        string connStr = ConfigurationManager.ConnectionStrings[eDbLocation.Local].ConnectionString;

        containerRegistry.RegisterSingleton<FtpDiligentSqlClient>(() => new FtpDiligentSqlClient(connStr));
        containerRegistry.RegisterSingleton<IFtpRepository, FtpDiligentSqlClient>();
        containerRegistry.RegisterSingleton<IFtpDispatcher, FtpDispatcher>();
        containerRegistry.RegisterSingleton<MainWindow>();
    }
}

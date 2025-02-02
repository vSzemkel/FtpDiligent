
// -----------------------------------------------------------------------
// <copyright file="App.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent;

using System.Configuration;
using System.Windows;

using FtpDiligent.Views;
using Prism.Ioc;
using Prism.Unity;
using Unity.Injection;

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
        containerRegistry.RegisterSingleton<IFtpDiligentDatabaseClient, FtpDiligentSqlClient>();
        containerRegistry.RegisterSingleton<IFtpDispatcher, FtpDispatcher>();
        containerRegistry.RegisterSingleton<MainWindow>();

    }
}

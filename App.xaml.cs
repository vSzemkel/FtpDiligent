
// -----------------------------------------------------------------------
// <copyright file="App.cs" company="private project">
// <legal>Copyright (c) MB, February 2025</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent;

using System.Configuration;
using System.Windows;

using DryIoc;
using Prism.Ioc;

using FtpDiligent.Views;

public partial class App : Prism.DryIoc.PrismApplication
{
    protected override Window CreateShell()
    {
        return Container.Resolve<MainWindow>();
    }

    protected override void RegisterTypes(IContainerRegistry containerRegistry)
    {
        string connStr = ConfigurationManager.ConnectionStrings[eDbLocation.Local].ConnectionString;
        containerRegistry.RegisterInstance<IFtpRepository>(new FtpDiligentSqlClient(connStr));
        containerRegistry.RegisterSingleton<IFtpDispatcher, FtpDispatcher>();
        containerRegistry.RegisterSingleton<MainWindow>();
    }
}

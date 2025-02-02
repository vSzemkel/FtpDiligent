
// -----------------------------------------------------------------------
// <copyright file="App.cs" company="Agora SA">
// <legal>Copyright (c) Development IT, kwiecien 2020</legal>
// <author>Marcin Buchwald</author>
// </copyright>
// -----------------------------------------------------------------------

namespace FtpDiligent;

using System.Configuration;
using System.Windows;

using Autofac;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static IContainer container;

    protected override void OnStartup(StartupEventArgs e)
    {
        string connStr = ConfigurationManager.ConnectionStrings[eDbLocation.Local].ConnectionString;

        var builder = new ContainerBuilder();
        builder.RegisterType<FtpDiligentSqlClient>()
            .SingleInstance()
            .WithParameter(new NamedParameter("connStr", connStr))
            .As<IFtpDiligentDatabaseClient>();
        builder.RegisterType<FtpDispatcher>()
            .SingleInstance()
            .As<IFtpDispatcher>();
        container = builder.Build();

        var scope = container.BeginLifetimeScope();
        FtpDispatcherGlobals.AutofacScope = scope;
        new Views.MainWindow(scope.Resolve<IFtpDiligentDatabaseClient>(), scope.Resolve<IFtpDispatcher>()).Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        FtpDispatcherGlobals.AutofacScope.Dispose();
    }
}

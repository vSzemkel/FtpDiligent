
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
        var builder = new ContainerBuilder();
        string connStr = ConfigurationManager.ConnectionStrings[eDbLocation.Local].ConnectionString;
        builder.RegisterType<MainWindow>().SingleInstance();
        builder.RegisterType<FtpDiligentSqlClient>()
            .WithParameter(new NamedParameter("connStr", connStr))
            .As<IFtpDiligentDatabaseClient>();
        container = builder.Build();

        using (var scope = container.BeginLifetimeScope())
            scope.Resolve<MainWindow>().Show();
    }
}

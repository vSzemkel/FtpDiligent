# FtpDiligent
FTP transfer manager/scheduler

- Protocols: FTP, SFTP ([SSH.NET](https://github.com/sshnet/SSH.NET)), FTPS ([FluentFTP](https://github.com/robinrodricks/FluentFTP))
- Databases: (LocalDB)\MSSQLLocalDB, Oracle, Azure SQL DB
- Transfers: scheduled or hotfolder upload
- Schedules: Weekly (start, stop, stride)
- SyncModes: NewerThenRefreshDate, UniqueDateAndSizeOnDisk, UniqueDateAndSizeInDatabase, AllFiles
- Client GUI: WPF, Microsoft.NET.Sdk.WindowsDesktop
- Diagnostics: GUI, EventLog, Email ([SendGrid](https://sendgrid.com/))
- DI container: ([Autofac](https://docs.autofac.org/))


![Main Window](https://github.com/vSzemkel/FtpDiligent/blob/master/Images/screen.gif?raw=true)

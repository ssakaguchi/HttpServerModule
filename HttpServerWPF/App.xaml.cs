using System.Windows;
using HttpServerService;
using LoggerService;
using ConfigService;

namespace HttpServerWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override Window CreateShell() => Container.Resolve<MainWindow>();

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterSingleton<IServer, Server>();
            containerRegistry.RegisterSingleton<ILogFileWatcher>
                (() => new LogFileWatcher(logDirectoryName: @"logs", logFileName: @"Communication.log"));
            containerRegistry.RegisterSingleton<ILog4netAdapter>
                (() => new Log4netAdapter(logDirectoryName: @"logs", logFileName: @"Communication.log"));
            containerRegistry.RegisterSingleton<IConfigService>(() => new ConfigManager(filePath: @"external_setting_file.json"));
        }
    }
}

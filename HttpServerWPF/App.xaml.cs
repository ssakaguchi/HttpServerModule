using System.Windows;
using HttpServerService;
using LoggerService;

namespace HttpServerWPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        private static class CommunicationLog
        {
            public const string Directory = @"logs";
            public const string FilePath = @"Communication.log";
        }

        protected override Window CreateShell() => Container.Resolve<MainWindow>();

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterInstance(
                new LoggerOptions { LogDirectoryName = CommunicationLog.Directory, LogFileName = CommunicationLog.FilePath });

            containerRegistry.RegisterSingleton<IServer, Server>();
            containerRegistry.RegisterSingleton<ILogFileWatcher, LogFileWatcher>();

        }
    }
}

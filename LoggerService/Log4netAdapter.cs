using log4net;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;

namespace LoggerService
{
    public sealed class Log4netAdapter : ILog4netAdapter
    {
        private ILog Logger { get; } = LogManager.GetLogger(typeof(Log4netAdapter));

        public Log4netAdapter(LoggerOptions options)
        {
            Initialize(options.LogDirectoryName, options.LogFileName);
        }

        public Log4netAdapter()
        {
        }

        internal static void Initialize(string logDirectoryName, string logFileName)
        {
            var hierarchy = (Hierarchy)LogManager.GetRepository();

            // 既に追加済みなら何もしない
            if (hierarchy.Root.Appenders.Cast<IAppender>()
                .Any(a => a.Name == "RollingFileAppender"))
            {
                return;
            }

            // ログディレクトリ作成
            var logDir = Path.Combine(AppContext.BaseDirectory, logDirectoryName);
            Directory.CreateDirectory(logDir);

            // レイアウト
            var layout = new PatternLayout
            {
                ConversionPattern = "%date - %message%newline"
            };
            layout.ActivateOptions();

            // RollingFileAppender
            var appender = new RollingFileAppender
            {
                Name = "RollingFileAppender",
                File = Path.Combine(logDir, logFileName),
                AppendToFile = true,
                RollingStyle = RollingFileAppender.RollingMode.Size,
                MaximumFileSize = "10MB",
                MaxSizeRollBackups = 3,
                StaticLogFileName = true,
                LockingModel = new FileAppender.MinimalLock(),
                Layout = layout
            };
            appender.ActivateOptions();

            // Root logger 設定
            hierarchy.Root.Level = Level.All;
            hierarchy.Root.AddAppender(appender);
            hierarchy.Configured = true;
        }

        public void Info(string message) => Logger.Info(message);

        public void Error(string message) => Logger.Error(message);

        public void Error(string message, Exception ex) => Logger.Error(message, ex);
    }

    public interface ILog4netAdapter
    {
        public void Info(string message);

        public void Error(string message);

        public void Error(string message, Exception ex);
    }
}

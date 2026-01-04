using System.Text;

namespace LoggerService
{
    public sealed class LogFileWatcher : IDisposable, ILogFileWatcher
    {
        public event EventHandler<string>? FileChanged;

        private readonly FileSystemWatcher _fileWatcher;
        private const int ReadFileRetryCount = 5;
        private string _logFilePath = string.Empty;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public LogFileWatcher(string logDirectoryName, string logFileName)
        {
            _logFilePath = Path.Combine(AppContext.BaseDirectory, logDirectoryName, logFileName);

            _fileWatcher = new FileSystemWatcher
            {
                Path = Path.Combine(AppContext.BaseDirectory, logDirectoryName),    // 監視対象のディレクトリ
                Filter = logFileName,   // 監視対象のファイル名
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size, // 監視対象の変更内容
                IncludeSubdirectories = false,      // サブディレクトリは監視しない
                EnableRaisingEvents = true          // 監視を開始
            };


            // イベントハンドラの登録
            _fileWatcher.Created += OnFileChanged;
            _fileWatcher.Changed += OnFileChanged;
        }

        public LogFileWatcher(LoggerOptions options) : this(options.LogDirectoryName, options.LogFileName)
        {
        }

        public LogFileWatcher() { }

        public void Start() => _fileWatcher.EnableRaisingEvents = true;          // 監視を開始
        public void Stop() => _fileWatcher.EnableRaisingEvents = false;          // 監視を停止

        /// <summary>ログファイルの内容を非同期で取得する</summary>
        public async Task<string> ReadLogFileContentAsync() => await ReadFileWithRetryAsync(_logFilePath);

        /// <summary>ファイル変更時処理</summary>
        private async void OnFileChanged(object sender, FileSystemEventArgs e)
        {
            /* 【連続発火・ロック対策】
             * 一回のファイル保存で複数回呼ばれることがあるため、少し待つ。
             * あと、少し待ってから書き込みが終わってからファイルを読み込むようにする。
             */
            await Task.Delay(100);

            try
            {
                string content = await ReadFileWithRetryAsync(_logFilePath);
                FileChanged?.Invoke(this, content);
            }
            catch
            {
            }
        }


        /// <summary>ファイル読込み時処理</summary>
        /// <remarks>一回でファイルを読み込めない場合があるので複数回リトライする</remarks>
        private static async Task<string> ReadFileWithRetryAsync(string path)
        {
            for (int i = 0; i < ReadFileRetryCount; i++)
            {
                try
                {
                    using var stream = new FileStream(
                        path,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.ReadWrite);

                    using var reader = new StreamReader(stream, Encoding.UTF8);
                    return await reader.ReadToEndAsync();
                }
                catch (IOException)
                {
                    await Task.Delay(100);
                }
            }

            throw new IOException("ファイルを読み取れませんでした。");
        }

        public void Dispose()
        {
            this.Stop();
            _fileWatcher?.Dispose();
        }
    }


    public static class LogFileWatcherFactory
    {
        public static ILogFileWatcher Create(string logDirectoryName, string logFileName) => new LogFileWatcher(logDirectoryName, logFileName);
    }


    public interface ILogFileWatcher
    {
        public event EventHandler<string>? FileChanged;

        public void Start();

        public void Stop();

        Task<string> ReadLogFileContentAsync();

        void Dispose();
    }
}

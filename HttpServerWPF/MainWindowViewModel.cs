using HttpServerService;
using LoggerService;
using Reactive.Bindings;
using Reactive.Bindings.Disposables;
using Reactive.Bindings.Extensions;

namespace HttpServerWPF
{
    public class MainWindowViewModel : BindableBase, IDisposable
    {
        public ReactiveProperty<string> HostName { get; } = new ReactiveProperty<string>(string.Empty);
        public ReactiveProperty<int> PortNo { get; } = new ReactiveProperty<int>(0);
        public ReactiveProperty<string> Path { get; } = new ReactiveProperty<string>(string.Empty);
        public ReactiveProperty<bool> UseBasicAuth { get; } = new ReactiveProperty<bool>(false);
        public ReactiveProperty<string> User { get; } = new ReactiveProperty<string>(string.Empty);
        public ReactiveProperty<string> Password { get; } = new ReactiveProperty<string>(string.Empty);
        public ReactiveProperty<string> LogText { get; } = new ReactiveProperty<string>(string.Empty);
        public ReactiveProperty<string> StatusMessage { get; } = new ReactiveProperty<string>(string.Empty);

        public ReactiveCommand LoadedCommand { get; } = new();
        public ReactiveCommand SaveCommand { get; } = new ReactiveCommand();
        public ReactiveCommand StartCommand { get; } = new ReactiveCommand();
        public ReactiveCommand StopCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ClearMessageCommand { get; } = new ReactiveCommand();

        private static class CommunicationLog
        {
            public const string Directory = @"logs";
            public const string FilePath = @"Communication.log";
        }

        private readonly CompositeDisposable _disposables = new();

        private readonly ILog4netAdapter _logger =
            Log4netAdapterFactory.Create(logDirectoryName: CommunicationLog.Directory, logFileName: CommunicationLog.FilePath);

        private readonly ILogFileWatcher _logFileWatcher =
            LogFileWatcherFactory.Create(logDirectoryName: CommunicationLog.Directory, logFileName: CommunicationLog.FilePath);

        public MainWindowViewModel()
        {
            SaveCommand.Subscribe(this.OnSaveButtonClicked).AddTo(_disposables);
            StartCommand.Subscribe(this.OnStartButtonClicked).AddTo(_disposables);
            StopCommand.Subscribe(this.OnStopButtonClicked).AddTo(_disposables);
            LoadedCommand.Subscribe(this.OnLoaded).AddTo(_disposables);
            ClearMessageCommand.Subscribe(this.ClearMessage).AddTo(_disposables);

            // 通信履歴ファイルの監視を開始
            _logFileWatcher.FileChanged += OnLogFileChanged;
        }

        private async void OnLoaded()
        {
            try
            {
                ConfigData configData = ConfigManager.GetConfigData();
                this.HostName.Value = configData.Host;
                this.PortNo.Value = int.Parse(configData.Port);
                this.Path.Value = configData.Path;
                this.User.Value = configData.User;
                this.UseBasicAuth.Value = configData.UseBasicAuth;
                this.Password.Value = configData.Password;

                this.LogText.Value = await _logFileWatcher.ReadLogFileContentAsync();

            }
            catch (Exception e)
            {
                _logger.Error("Loadに失敗しました。", e);
                StatusMessage.Value = "Loadに失敗しました。";
            }
        }

        private void OnSaveButtonClicked()
        {
            try
            {
                var configData = new ConfigData
                {
                    Host = this.HostName.Value,
                    Port = this.PortNo.Value.ToString(),
                    Path = this.Path.Value,
                    UseBasicAuth = this.UseBasicAuth.Value,
                    User = this.User.Value,
                    Password = this.Password.Value
                };
                ConfigManager.SaveConfigData(configData);

                StatusMessage.Value = "設定を保存しました。";
            }
            catch (Exception e)
            {
                _logger.Error("設定の保存に失敗しました。", e);
                StatusMessage.Value = "設定の保存に失敗しました。";
            }
        }

        private void OnStartButtonClicked() => Server.Instance.Start();

        private void OnStopButtonClicked() => Server.Instance.Stop();

        private void OnLogFileChanged(object? sender, string content) => LogText.Value = content;

        private void ClearMessage() => StatusMessage.Value = string.Empty;

        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}

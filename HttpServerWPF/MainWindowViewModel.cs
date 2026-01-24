using System.Reactive.Linq;
using ConfigService;
using HttpServerService;
using HttpServerWPF.ConfigMapper;
using HttpServerWPF.FileDialogService;
using LoggerService;
using Reactive.Bindings;
using Reactive.Bindings.Disposables;
using Reactive.Bindings.Extensions;

namespace HttpServerWPF
{
    public class MainWindowViewModel : BindableBase, IDisposable
    {
        public enum AuthenticationMethodType
        {
            Basic,
            Anonymous,
        }

        public ReactiveProperty<bool> UseHttps { get; } = new ReactiveProperty<bool>(false);
        public ReactiveProperty<string> HostName { get; } = new ReactiveProperty<string>(string.Empty);
        public ReactiveProperty<int> PortNo { get; } = new ReactiveProperty<int>(0);
        public ReactiveProperty<string> Path { get; } = new ReactiveProperty<string>(string.Empty);
        public ReactiveProperty<AuthenticationMethodType> AuthenticationMethod { get; } = new(AuthenticationMethodType.Basic);

        public ReactiveProperty<string> User { get; } = new ReactiveProperty<string>(string.Empty);
        public ReactiveProperty<string> Password { get; } = new ReactiveProperty<string>(string.Empty);
        public ReactiveProperty<string> UploadDirectoryPath { get; } = new ReactiveProperty<string>(string.Empty);
        public ReactiveProperty<string> LogText { get; } = new ReactiveProperty<string>(string.Empty);
        public ReactiveProperty<string> StatusMessage { get; } = new ReactiveProperty<string>(string.Empty);

        public ReactiveCommand LoadedCommand { get; } = new();
        public ReactiveCommand SaveCommand { get; } = new ReactiveCommand();
        public ReactiveCommand StartCommand { get; } = new ReactiveCommand();
        public ReactiveCommand StopCommand { get; } = new ReactiveCommand();
        public ReactiveCommand UploadDirectorySelectCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ClearMessageCommand { get; } = new ReactiveCommand();

        public ReactiveProperty<bool> IsUserEnabled { get; } = new ReactiveProperty<bool>(true);
        public ReactiveProperty<bool> IsPasswordEnabled { get; } = new ReactiveProperty<bool>(true);

        public ReactiveProperty<bool> SaveCommandEnabled { get; } = new ReactiveProperty<bool>(true);
        public ReactiveProperty<bool> StartCommandEnabled { get; } = new ReactiveProperty<bool>(true);
        public ReactiveProperty<bool> StopCommandEnabled { get; } = new ReactiveProperty<bool>(true);

        private readonly CompositeDisposable _disposables = new();
        private readonly IServer _server;
        private readonly ILoggerService _logger;
        private readonly ILogFileWatcher _logFileWatcher;
        private readonly IConfigService _configService;
        private readonly IOpenFolderDialogService _openFileDialogService;
        private readonly IConfigMapper _configMapper;

        public MainWindowViewModel(IServer server,
                                   ILoggerService log4NetAdapter,
                                   ILogFileWatcher logFileWatcher,
                                   IConfigService configService,
                                   IOpenFolderDialogService openFileDialogService,
                                   IConfigMapper configMapper)
        {
            UploadDirectorySelectCommand.Subscribe(this.OnUploadDirectorySelectButtonClicked).AddTo(_disposables);
            SaveCommand.Subscribe(this.OnSaveButtonClicked).AddTo(_disposables);
            StartCommand.Subscribe(this.OnStartButtonClicked).AddTo(_disposables);
            StopCommand.Subscribe(this.OnStopButtonClicked).AddTo(_disposables);
            LoadedCommand.Subscribe(this.OnLoaded).AddTo(_disposables);
            ClearMessageCommand.Subscribe(this.ClearMessage).AddTo(_disposables);


            UseHttps.Skip(1).Subscribe(x => { this.UpdateEnabled(); }).AddTo(_disposables);
            HostName.Skip(1).Subscribe(x => { this.UpdateEnabled(); }).AddTo(_disposables);
            PortNo.Skip(1).Subscribe(x => { this.UpdateEnabled(); }).AddTo(_disposables);
            Path.Skip(1).Subscribe(x => { this.UpdateEnabled(); }).AddTo(_disposables);
            AuthenticationMethod.Skip(1).Subscribe(x => { this.UpdateEnabled(); }).AddTo(_disposables);
            User.Skip(1).Subscribe(x => { this.UpdateEnabled(); }).AddTo(_disposables);
            Password.Skip(1).Subscribe(x => { this.UpdateEnabled(); }).AddTo(_disposables);
            UploadDirectoryPath.Skip(1).Subscribe(x => { this.UpdateEnabled(); }).AddTo(_disposables);

            _server = server;
            _logger = log4NetAdapter;
            _logFileWatcher = logFileWatcher;
            _configService = configService;
            _openFileDialogService = openFileDialogService;
            _configMapper = configMapper;

            // 通信履歴ファイルの監視を開始
            _logFileWatcher.FileChanged += OnLogFileChanged;
        }

        private async void OnLoaded()
        {
            try
            {
                var config = _configService.Load();
                _configMapper.ApplyTo(this, config);

                this.LogText.Value = await _logFileWatcher.ReadLogFileContentAsync();

                this.UpdateEnabled();
            }
            catch (Exception e)
            {
                _logger.Error("Loadに失敗しました。", e);
                StatusMessage.Value = "Loadに失敗しました。";
            }
        }

        private void OnUploadDirectorySelectButtonClicked()
        {
            try
            {
                _openFileDialogService.Title = "アップロードファイル保存先の選択";

                bool? result = _openFileDialogService.OpenFolderDialog();
                if (result == true)
                {
                    this.UploadDirectoryPath.Value = _openFileDialogService.FolderName;
                }

            }
            catch (Exception e)
            {
                _logger.Error("アップロードファイル保存先の選択に失敗しました。", e);
                StatusMessage.Value = "アップロードファイル保存先の選択に失敗しました。";
            }
        }

        private void OnSaveButtonClicked()
        {
            try
            {
                var config = _configMapper.CreateFrom(this);
                _configService.Save(config);

                this.UpdateEnabled();

                StatusMessage.Value = "設定を保存しました。";
            }
            catch (Exception e)
            {
                _logger.Error("設定の保存に失敗しました。", e);
                StatusMessage.Value = "設定の保存に失敗しました。";
            }
        }

        private void OnStartButtonClicked()
        {
            _logger.Info("サーバーを開始します");
            _server.Start();
        }

        private void OnStopButtonClicked()
        {
            _logger.Info("サーバーを停止します");
            _server.Stop();
        }

        private void OnLogFileChanged(object? sender, string content) => LogText.Value = content;

        private void UpdateEnabled()
        {
            var config = _configMapper.CreateFrom(this);
            bool existsDifference = _configService.ExistsConfigDifference(config);
            SaveCommandEnabled.Value = existsDifference;
            StartCommandEnabled.Value = !existsDifference;
            StopCommandEnabled.Value = !existsDifference;

            IsUserEnabled.Value = AuthenticationMethod.Value == AuthenticationMethodType.Basic;
            IsPasswordEnabled.Value = AuthenticationMethod.Value == AuthenticationMethodType.Basic;
        }


        private void ClearMessage() => StatusMessage.Value = string.Empty;

        public void Dispose() => _disposables.Dispose();
    }
}

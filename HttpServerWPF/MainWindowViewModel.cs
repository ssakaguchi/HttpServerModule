using HttpServerService;
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
        public ReactiveProperty<string> UserId { get; } = new ReactiveProperty<string>(string.Empty);
        public ReactiveProperty<string> Password { get; } = new ReactiveProperty<string>(string.Empty);
        public ReactiveProperty<string> LogText { get; } = new ReactiveProperty<string>(string.Empty);
        public ReactiveProperty<string> StatusMessage { get; } = new ReactiveProperty<string>(string.Empty);

        public ReactiveCommand LoadedCommand { get; } = new();
        public ReactiveCommand SaveCommand { get; } = new ReactiveCommand();
        public ReactiveCommand StartCommand { get; } = new ReactiveCommand();
        public ReactiveCommand StopCommand { get; } = new ReactiveCommand();
        public ReactiveCommand ClearMessageCommand { get; } = new ReactiveCommand();


        public MainWindowViewModel()
        {
            SaveCommand.Subscribe(this.OnSaveButtonClicked).AddTo(_disposables);
            StartCommand.Subscribe(this.OnStartButtonClicked).AddTo(_disposables);
            StopCommand.Subscribe(this.OnStopButtonClicked).AddTo(_disposables);
            LoadedCommand.Subscribe(this.OnLoaded).AddTo(_disposables);
            //ClearMessageCommand.Subscribe(this.ClearMessage).AddTo(_disposables);

            //// 通信履歴ファイルの監視を開始
            //_logFileWatcher.FileChanged += OnLogFileChanged;
        }

        private void OnLoaded()
        {
            //throw new NotImplementedException();
        }
        private void OnSaveButtonClicked()
        {
            throw new NotImplementedException();
        }

        private void OnStartButtonClicked()
        {
            Server.Instance.Start();
        }

        private void OnStopButtonClicked()
        {
            Server.Instance.Stop();
        }


        private readonly CompositeDisposable _disposables = new();
        public void Dispose()
        {
            _disposables.Dispose();
        }
    }
}

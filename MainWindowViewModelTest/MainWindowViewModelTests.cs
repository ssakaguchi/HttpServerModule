using ConfigService;
using HttpServerService;
using HttpServerWPF;
using HttpServerWPF.ConfigMapper;
using HttpServerWPF.FileDialogService;
using LoggerService;
using Moq;

namespace MainWindowViewModelTest
{
    public class MainWindowViewModelTests
    {
        private readonly Mock<IServer> _mockClient;
        private readonly Mock<ILoggerService> _mockLogger;
        private readonly Mock<ILogFileWatcher> _mockLogFileWatcher;
        private readonly Mock<IConfigService> _mockConfigService;
        private readonly Mock<IConfigMapper> _mockConfigMapper;
        private readonly Mock<IOpenFolderDialogService> _mockOpenFileDialogService;
        private readonly MainWindowViewModel _viewModel;

        public MainWindowViewModelTests()
        {
            _mockClient = new Mock<IServer>();
            _mockLogger = new Mock<ILoggerService>();
            _mockLogFileWatcher = new Mock<ILogFileWatcher>();
            _mockConfigService = new Mock<IConfigService>();
            _mockConfigMapper = new Mock<IConfigMapper>();
            _mockOpenFileDialogService = new Mock<IOpenFolderDialogService>();

            _viewModel = new MainWindowViewModel(
                _mockClient.Object,
                _mockLogger.Object,
                _mockLogFileWatcher.Object,
                _mockConfigService.Object,
                _mockOpenFileDialogService.Object,
                _mockConfigMapper.Object
            );
        }


        [Fact]
        public void 画面起動に成功_通信ログのデータを画面上のログエリアに表示する()
        {
            // arrange
            var expectedConfig = new ConfigData();
            var expectedLogContent = "Log content";

            _mockConfigService.Setup(x => x.Load()).Returns(expectedConfig);
            _mockLogFileWatcher.Setup(x => x.ReadLogFileContentAsync()).ReturnsAsync(expectedLogContent);

            // act
            _viewModel.LoadedCommand.Execute();

            _mockConfigService.Verify(x => x.Load(), Times.Once);
            _mockConfigMapper.Verify(x => x.ApplyTo(_viewModel, expectedConfig), Times.Once);

            // assert
            // ログ内容が画面に反映されていることを確認
            Assert.Equal(expectedLogContent, _viewModel.LogText.Value);
        }

        [Fact]
        public void 画面起動時に設定情報の読み込みに失敗した場合_エラーログが出力される_画面に指定のエラーメッセージを表示する()
        {
            // arrange
            _mockConfigService.Setup(x => x.Load()).Throws(new Exception("Load error"));

            // act
            _viewModel.LoadedCommand.Execute();

            // assert
            // エラーログが記録されていることを確認
            _mockLogger.Verify(x => x.Error("Loadに失敗しました。", It.IsAny<Exception>()), Times.Once);

            // 画面にエラーメッセージが表示されていることを確認
            Assert.Equal("Loadに失敗しました。", _viewModel.StatusMessage.Value);
        }


        [Fact]
        public void フォルダー選択ダイアログでフォルダーを選択_アップロードファイル保存先プロパティに選択したアップロードファイル保存先のパスがセットされる()
        {
            // arrange
            var expectedFilePath = "C:\\test";
            _mockOpenFileDialogService.Setup(x => x.OpenFolderDialog()).Returns(true);
            _mockOpenFileDialogService.Setup(x => x.FolderName).Returns(expectedFilePath);

            // act
            _viewModel.UploadDirectorySelectCommand.Execute();

            // assert
            // アップロードファイル保存先のパスが画面に反映されていることを確認
            Assert.Equal(expectedFilePath, _viewModel.UploadDirectoryPath.Value);
        }


        [Fact]
        public void Saveボタン押下時に設定の保存に失敗_画面に指定のエラーメッセージを表示する()
        {
            // arrange
            _mockConfigMapper.Setup(x => x.CreateFrom(_viewModel)).Throws(new Exception("Save error"));

            // act
            _viewModel.SaveCommand.Execute();

            // assert
            // エラーログが記録されていることを確認
            _mockLogger.Verify(x => x.Error("設定の保存に失敗しました。", It.IsAny<Exception>()), Times.Once);

            // 画面にエラーメッセージが表示されていることを確認
            Assert.Equal("設定の保存に失敗しました。", _viewModel.StatusMessage.Value);
        }


        [Fact]
        public void サーバー開始ボタン押下時_サーバー開始処理が実行される_ログにサーバーが開始したことが出力される()
        {
            // arrange
            // act
            _viewModel.StartCommand.Execute();

            // assert
            _mockClient.Verify(x => x.Start(), Times.Once);
            _mockLogger.Verify(x => x.Info("サーバーを開始します"), Times.Once);
        }


        [Fact]
        public void サーバー停止ボタン押下時_サーバー停止処理が実行される_ログにサーバーが停止したことが出力される()
        {
            // arrange
            // act
            _viewModel.StopCommand.Execute();

            // assert
            _mockClient.Verify(x => x.Stop(), Times.Once);
            _mockLogger.Verify(x => x.Info("サーバーを停止します"), Times.Once);
        }

        [Fact]
        public void 認証方法を切り替える_ユーザーとパスワードの活性状態も切り替わる()
        {
            // arrange & act
            // 認証方法をAnonymousに設定
            _viewModel.AuthenticationMethod.Value = MainWindowViewModel.AuthenticationMethodType.Anonymous;

            // ユーザー名とパスワードの入力欄が無効化されていることを確認
            Assert.False(_viewModel.IsUserEnabled.Value);
            Assert.False(_viewModel.IsPasswordEnabled.Value);

            // arrange & act
            // 認証方法をBasicに切り替え
            _viewModel.AuthenticationMethod.Value = MainWindowViewModel.AuthenticationMethodType.Basic;

            // ユーザー名とパスワードの入力欄が有効化されていることを確認s
            Assert.True(_viewModel.IsUserEnabled.Value);
            Assert.True(_viewModel.IsPasswordEnabled.Value);
        }

        [Fact]
        public void 画面上の設定情報と保存中の設定情報に差がある_ボタンのうちSaveボタンのみ活性状態にする()
        {
            // arrange
            var config = new ConfigData();
            _mockConfigMapper.Setup(x => x.CreateFrom(_viewModel)).Returns(config);
            _mockConfigService.Setup(x => x.ExistsConfigDifference(config)).Returns(true);

            // act
            _viewModel.HostName.Value = "test"; // 変更してUpdateEnabledを実行させる

            // assert
            Assert.True(_viewModel.SaveCommandEnabled.Value);
            Assert.False(_viewModel.StartCommandEnabled.Value);
            Assert.False(_viewModel.StopCommandEnabled.Value);
        }


        [Fact]
        public void ログファイルが更新される_画面上のログエリアも同じ内容で更新される()
        {
            // arrange
            var newContent = "New log content";

            // act
            _mockLogFileWatcher.Raise(x => x.FileChanged += null, null, newContent);

            // assert
            Assert.Equal(newContent, _viewModel.LogText.Value);
        }
    }
}

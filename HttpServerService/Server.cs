using System.Net;
using System.Text;
using LoggerService;

namespace HttpServerService
{
    public class Server
    {
        private static readonly Server _instance = new();
        public static Server Instance => _instance;

        private HttpListener _listener = new HttpListener();
        private readonly object _sync = new();
        private bool _isStopping;

        private static class CommunicationLog
        {
            public const string Directory = @"logs";
            public const string FilePath = @"Communication.log";
        }

        private readonly ILog4netAdapter _logger =
            Log4netAdapterFactory.Create(logDirectoryName: CommunicationLog.Directory, logFileName: CommunicationLog.FilePath);

        /// <summary> コンストラクタ </summary>
        private Server()
        {
        }

        /// <summary> APIサービスを起動する </summary>
        public void Start()
        {
            try
            {
                lock (_sync)
                {
                    if (_listener.IsListening) return;

                    _isStopping = false;

                    ConfigData config = ConfigManager.GetConfigData();
                    string prefix = $"http://{config.Host}:{config.Port}/{config.Path}/";

                    _listener = new HttpListener();
                    _listener.Prefixes.Add(prefix);

                    // HTTPサーバーを起動する
                    _listener.Start();
                    _listener.BeginGetContext(OnRequestReceived, _listener);
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary> APIサービスを停止する </summary>
        public void Stop()
        {
            lock (_sync)
            {
                if (_listener == null)
                {
                    return;
                }

                _isStopping = true;

                try
                {
                    if (_listener.IsListening)
                    {
                        _listener.Stop();
                    }
                    _listener.Close();
                }
                finally
                {
                    _listener = new HttpListener();
                }
            }
        }

        /// <summary> リクエスト受信時の処理 </summary>
        private void OnRequestReceived(IAsyncResult result)
        {
            _ = this.OnRequestReceivedAsync(result);
        }

        private async Task<bool> OnRequestReceivedAsync(IAsyncResult result)
        {
            // リスナーの状態確認
            if (result.AsyncState is not HttpListener listener) return false;

            // 停止中でなければ次の受信待ちを予約
            try
            {
                if (listener.IsListening && !_isStopping)
                {
                    listener.BeginGetContext(OnRequestReceived, listener);
                }
            }
            catch (ObjectDisposedException) { return false; }
            catch (HttpListenerException) { return false; }

            try
            {
                if (!listener.IsListening) return false;

                // リクエストコンテキストの取得
                HttpListenerContext context = listener.EndGetContext(result);
                HttpListenerRequest httpListenerRequest = context.Request;

                _logger.Info($"要求URI:{httpListenerRequest.Url}");
                _logger.Info($"HTTPメソッド:{httpListenerRequest.HttpMethod}");
                _logger.Info($"受信データ:");
                _logger.Info($"  ヘッダー部:");

                foreach (var key in httpListenerRequest.Headers.AllKeys)
                {
                    if (key == null) continue;

                    string value = httpListenerRequest.Headers[key] ?? string.Empty;
                    _logger.Info($"    {key}:{value}");
                }

                _logger.Info($"  ボディ部:");
                var encoding = context.Request.ContentEncoding ?? Encoding.UTF8;
                using (var reader = new StreamReader(context.Request.InputStream, encoding))
                {
                    var body = await reader.ReadToEndAsync();
                    if (body != string.Empty)
                    {
                        _logger.Info($"    {body}");
                    }
                    else
                    {
                        _logger.Info($"    なし");
                    }
                }

                HttpListenerResponse response = context.Response;

                // レスポンスの設定
                response.StatusCode = (int)HttpStatusCode.OK;
                response.ContentType = "application/json";
                response.ContentEncoding = Encoding.UTF8;

                // レスポンスデータの取得
                byte[] buffer = GetResponseData();

                // レスポンスの書き込み
                response.ContentLength64 = buffer.Length;

                using (response.OutputStream)
                {
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                }

                return true;
            }
            catch (ObjectDisposedException)
            {
                // Stop/Closeによる終了
                return false;
            }
            catch (HttpListenerException)
            {
                // Stopによる中断
                return false;
            }
            catch (Exception)
            {
                // todo:ログ出力実装する
                return false;
            }

            return true;
        }

        /// <summary> レスポンスデータを取得する </summary>
        private static byte[] GetResponseData()
        {
            if (File.Exists("response.json"))
            {
                string jsonResponse = File.ReadAllText("response.json");
                return Encoding.UTF8.GetBytes(jsonResponse);
            }
            else
            {
                return Encoding.UTF8.GetBytes("{\"error\":\"response.json file not found.\"}");
            }
        }
    }
}

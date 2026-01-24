using System.Net;
using System.Text;
using ConfigService;
using LoggerService;

namespace HttpServerService
{
    public class Server : IServer
    {
        private HttpListener _listener = new HttpListener();
        private readonly object _sync = new();
        private bool _isStopping;

        private readonly ILog4netAdapter _logger;
        private readonly IConfigService _configService;

        /// <summary> コンストラクタ </summary>
        public Server(ILog4netAdapter log4NetAdapter, IConfigService configService)
        {
            _logger = log4NetAdapter;
            _configService = configService;
        }

        /// <summary> APIサービスを起動する </summary>
        public void Start()
        {
            try
            {
                lock (_sync)
                {
                    if (_listener.IsListening)
                    {
                        this.Stop();
                    }

                    _isStopping = false;

                    var config = _configService.Load();

                    _listener = new HttpListener();

                    // 認証設定
                    if (config.AuthenticationMethod.Equals("Basic"))
                    {
                        _listener.AuthenticationSchemes = AuthenticationSchemes.Basic;

                        _logger.Info($"  認証方法：Basic認証");
                        _logger.Info($"  アカウント認証 ");
                        _logger.Info($"    ユーザー名：{config.User}");
                        _logger.Info($"    パスワード：{config.Password}");
                    }
                    else
                    {
                        _listener.AuthenticationSchemes = AuthenticationSchemes.Anonymous;
                        _logger.Info("  認証方法：匿名認証");
                    }

                    // uriPrefixの設定
                    UriBuilder uriBuilder = new()
                    {
                        Scheme = config.Scheme,
                        Host = config.Host,
                        Port = int.Parse(config.Port),
                        Path = config.Path.TrimStart('/').TrimEnd('/') + "/"
                    };


                    _logger.Info($"  URI：{uriBuilder.Uri}");

                    _listener.Prefixes.Add(uriBuilder.Uri.ToString());

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

                var config = _configService.Load();
                if (config.AuthenticationMethod.Equals("Basic"))
                {
                    bool ok = TryValidateBasicAuth(httpListenerRequest, config.User, config.Password);
                    if (!ok)
                    {
                        _logger.Error("Basic認証に失敗しました。");
                        await WriteUnauthorizedAsync(context.Response, realm: "HttpServerService");
                        return true;
                    }
                    _logger.Error("Basic認証に成功しました。");
                }

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
                    body = string.IsNullOrEmpty(body) ? "なし" : body;
                    _logger.Info($"    {body}");

                    if (httpListenerRequest.HttpMethod == "POST")
                    {
                        await SaveUploadFile(context, config, body);
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
            catch (Exception ex)
            {
                _logger.Error($"未知の例外エラーが発生しました。", ex);
                return false;
            }
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

        /// <summary> Basic認証の検証を行う </summary>
        private bool TryValidateBasicAuth(HttpListenerRequest request, string expectedUser, string expectedPassword)
        {
            // Authorization: Basic base64(user:pass)
            string? auth = request.Headers["Authorization"];

            if (string.IsNullOrWhiteSpace(auth)) return false;

            if (!auth.StartsWith("Basic ", StringComparison.OrdinalIgnoreCase)) return false;

            string encoded = auth.Substring("Basic ".Length).Trim();
            byte[] decodedBytes;

            try
            {
                decodedBytes = Convert.FromBase64String(encoded);

                string decoded = Encoding.UTF8.GetString(decodedBytes);

                int idx = decoded.IndexOf(':');
                if (idx <= 0) return false;

                string user = decoded.Substring(0, idx);
                string pass = decoded.Substring(idx + 1);


                return user == expectedUser && pass == expectedPassword;
            }
            catch
            {
                return false;
            }
        }

        /// <summary> Unauthorizedレスポンスを書き込む </summary>
        private async Task WriteUnauthorizedAsync(HttpListenerResponse response, string realm)
        {
            response.StatusCode = (int)HttpStatusCode.Unauthorized;
            response.ContentType = "application/json";
            response.ContentEncoding = Encoding.UTF8;

            // WWW-Authenticateヘッダーを追加
            response.AddHeader("WWW-Authenticate", $"Basic realm=\"{realm}\", charset=\"UTF-8\"");

            byte[] body = Encoding.UTF8.GetBytes("{\"error\":\"Unauthorized\"}");
            response.ContentLength64 = body.Length;
            await response.OutputStream.WriteAsync(body, 0, body.Length);
            response.OutputStream.Close();
        }

        /// <summary> アップロードファイルを保存する </summary>
        private static async Task SaveUploadFile(HttpListenerContext context, ConfigData config, string body)
        {
            // 受信したJsonファイルを保存する
            if (context.Request.Url == null || context.Request.Url.Segments == null || context.Request.Url.Segments.Length == 0) { return; }

            string command = context.Request.Url.Segments[^1].TrimEnd('/');
            string filename = $"{command}_{DateTime.Now:yyyyMMddHHmmss.fff}.json";

            // 保存フォルダのパスを取得(デフォルトは実行フォルダ)
            string directory = string.IsNullOrWhiteSpace(config.UploadDirectoryPath)
                ? Environment.CurrentDirectory
                : config.UploadDirectoryPath.Trim('/');

            string saveFolder = $"{directory}\\Json";

            // 未作成の場合、保存フォルダを作成する
            if (!Directory.Exists(saveFolder)) { Directory.CreateDirectory(saveFolder); }

            string filePath = Path.Combine(saveFolder, filename);

            // 保存
            await File.WriteAllTextAsync(filePath, body, Encoding.UTF8);
        }
    }
}

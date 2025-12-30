using System.Net;
using System.Text;

namespace HttpServerService
{
    public class Server
    {
        private static readonly Server _instance = new();
        public static Server Instance => _instance;

        private HttpListener _listener = new HttpListener();
        private readonly object _sync = new();
        private bool _isStopping;

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
            // リスナーの状態確認
            if (result.AsyncState is not HttpListener listener) return;

            // 停止中でなければ次の受信待ちを予約
            try
            {
                if (listener.IsListening && !_isStopping)
                {
                    listener.BeginGetContext(OnRequestReceived, listener);
                }
            }
            catch (ObjectDisposedException) { return; }
            catch (HttpListenerException) { return; }

            try
            {
                if (!listener.IsListening) return;

                // リクエストコンテキストの取得
                HttpListenerContext context = listener.EndGetContext(result);
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
            }
            catch (ObjectDisposedException)
            {
                // Stop/Closeによる終了
                return;
            }
            catch (HttpListenerException)
            {
                // Stopによる中断
                return;
            }
            catch (Exception)
            {
                // todo:ログ出力実装する
                return;
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
    }
}

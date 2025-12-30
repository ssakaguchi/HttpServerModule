using System.Net;
using System.Text;

namespace HttpServerService
{
    public class Server
    {
        private static readonly Server _instance = new();
        public static Server Instance => _instance;

        private HttpListener _listener = new HttpListener();

        /// <summary> コンストラクタ </summary>
        private Server()
        {
        }

        /// <summary> APIサービスを起動する </summary>
        public void Start()
        {
            try
            {
                ConfigData config = SettingManager.GetConfigData();
                string prefix = $"http://{config.Host}:{config.Port}/{config.Path}/";

                _listener.Prefixes.Add(prefix);

                // HTTPサーバーを起動する
                _listener.Start();

                while (_listener.IsListening)
                {
                    var result = _listener.BeginGetContext(OnRequestReceived, _listener);
                    result.AsyncWaitHandle.WaitOne();
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
            if (_listener != null && _listener.IsListening)
            {
                _listener.Stop();
                _listener.Close();
            }
        }

        /// <summary> リクエスト受信時の処理 </summary>
        private void OnRequestReceived(IAsyncResult result)
        {
            // リスナーの状態確認
            if (result.AsyncState is not HttpListener listener) return;
            if (!listener.IsListening) { return; }

            try
            {
                // リクエストコンテキストの取得
                HttpListenerContext context = listener.EndGetContext(result);
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;

                // レスポンスの設定
                response.StatusCode = (int)HttpStatusCode.OK;
                response.ContentType = "application/json";
                response.ContentEncoding = Encoding.UTF8;

                // レスポンスデータの取得
                byte[] buffer = GetResponseData();

                // レスポンスの書き込み
                response.ContentLength64 = buffer.Length;
                response.OutputStream.Write(buffer, 0, buffer.Length);
                using var writer = new StreamWriter(response.OutputStream, Encoding.UTF8);
            }
            catch (Exception)
            {
                throw;
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

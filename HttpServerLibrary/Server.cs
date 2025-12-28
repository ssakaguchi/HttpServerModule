using System.Net;
using System.Text;

namespace HttpServerLibrary
{
    public class Server
    {
        private static readonly Server _instance = new();
        public static Server Instance => _instance;

        private HttpListener _listener = new HttpListener();

        private Server()
        {
        }

        /// <summary> APIサービスを起動する </summary>
        public void Start()
        {
            try
            {
                _listener.Prefixes.Add("http://localhost:8080/api/v1/resource/");
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

        public void Stop()
        {
            if (_listener != null && _listener.IsListening)
            {
                _listener.Stop();
                _listener.Close();
            }
        }


        private void OnRequestReceived(IAsyncResult result)
        {
            if (result.AsyncState is not HttpListener listener) return;
            if (!listener.IsListening) { return; }

            HttpListenerContext context = listener.EndGetContext(result);
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            try
            {
                response.StatusCode = (int)HttpStatusCode.OK;
                response.ContentType = "application/json";
                response.ContentEncoding = Encoding.UTF8;

                using var writer = new StreamWriter(response.OutputStream, Encoding.UTF8);
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}

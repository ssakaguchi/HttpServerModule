using HttpServerService;

class Program
{
    static void Main(string[] args)
    {
        var server = Server.Instance;
        server.Start();
    }
}
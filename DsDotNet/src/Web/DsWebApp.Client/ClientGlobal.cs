//using IoHubClient = IO.Core.Client;

namespace DsWebApp.Client
{
    public class ClientGlobal
    {
        public HmiTagPackage HmiTagPackage { get; set; }
        public static int Counter { get; private set; } = 0;

        /* 직접 client 연결 불가.  browser 에서는 socket 지원 안됨 */
        //public ResettableLazy<IoHubClient> Client { get; private set; }
        public ClientGlobal()
        {
            Console.WriteLine("ClientGlobal ctor");
            if (Counter != 0)
                throw new InvalidOperationException("ClientGlobal must be singleton");
            Counter++;

            //Client = new (() =>
            //{
            //    var client = new IoHubClient($"tcp://localhost:{5555}");
            //    var meta = client.GetMeta();

            //    return client;
            //});
        }
    }
}

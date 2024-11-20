using System;
using System.Threading.Tasks;
using Opc.Ua;
using Opc.Ua.Configuration;
using Engine.Runtime;
using Opc.Ua.Server;
using static Engine.Core.Interface;

namespace OPC.UA.DSServer
{
    public static class DsOpcUaServerManager
    {
        private static ApplicationInstance? _application;
        private static DsOPCServer? _server;

        /// <summary>
        /// OPC UA 서버 시작
        /// </summary>
        public static async Task StartAsync(Storages storages)
        {
            Console.WriteLine("OPC UA 서버 초기화 중...");

            // 1. 애플리케이션 인스턴스 생성
            _application = new ApplicationInstance
            {
                ApplicationName = "MyOpcUaServer",
                ApplicationType = ApplicationType.Server,
                ConfigSectionName = "OpcUaServer"
            };

            // 2. 설정 파일 로드
            await _application.LoadApplicationConfiguration(false);

            // 3. 인증서 생성 및 확인
            bool isCertValid = await _application.CheckApplicationInstanceCertificate(false, 2048);
            if (!isCertValid)
            {
                throw new Exception("애플리케이션 인증서가 유효하지 않습니다.");
            }

            // 4. 서버 시작
            _server = new DsOPCServer(storages);
            await _application.Start(_server);

            Console.WriteLine("OPC UA 서버가 시작되었습니다.");
            Console.WriteLine("엔드포인트:");
            foreach (var endpoint in _server.GetEndpoints())
            {
                Console.WriteLine(endpoint.EndpointUrl);
            }
        }

        /// <summary>
        /// OPC UA 서버 종료
        /// </summary>
        public static void Stop()
        {
            if (_server != null)
            {
                _server.Stop();
                Console.WriteLine("OPC UA 서버가 종료되었습니다.");
            }
        }
    }
    /// <summary>
    /// test 코드
    /// </summary>
    class Program
    {
        static async Task Main(string[] args)
        {

            try
            {
                // 4. RuntimeModel 초기화
                RuntimeModel runModel = new RuntimeModel(@"z://HelloDS.dsz", Engine.Core.RuntimeGeneratorModule.PlatformTarget.WINDOWS);

                // OPC UA 서버 시작
                await DsOpcUaServerManager.StartAsync(runModel.System.TagManager.Storages);

                Console.WriteLine("종료하려면 아무 키나 누르세요...");
                Console.ReadKey();

                // OPC UA 서버 종료
                DsOpcUaServerManager.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"오류: {ex.Message}");
            }
        }
    }
}

using MelsecProtocol;
using Dual.PLC.Common.FS;
using System;
using System.Collections.Generic;
using System.Threading;

class Program
{
    static void Main()
    {
        const int delay = 100;         // 스캔 주기 (ms)
        const int timeout = 2000;       // 통신 타임아웃 (ms)
        const bool isMonitorOnly = true;
        const string plcIp = "192.168.9.109";  // 실제 MELSEC PLC IP 주소

        // 1. F# 스캔 매니저 생성 및 Scanner 확보
        var scanMgr = new MxScanManager(delay, timeout, isMonitorOnly);
        var scanner = scanMgr.CreateScanner(plcIp);

        // 2. 테스트 태그 목록 구성
        const int count = 10;
        string[] deviceTypes = new[] { "M" }; // B, M, X, Y, 등 사용 가능

        var tags = new List<TagInfo>();
        foreach (var dev in deviceTypes)
        {
            for (int i = 0; i < count; i++)
            {
                string address = $"{dev}{i:X}"; // 예: M0, M1, ..., M9
                tags.Add(new TagInfo(
                    name: $"{dev}_{i}",
                    address: address,
                    comment: $"Test {address}",
                    isOutput: false
                ));
            }
        }

        // 3. 이벤트 핸들러 등록
        scanner.TagValueChangedNotify += (s, e) =>
        {
            Console.WriteLine($"📡 [PLC {e.Ip}] {e.Tag.Name} ({e.Tag.Address}) = {e.Tag.Value}");
        };

        scanner.ConnectChangedNotify += (s, e) =>
        {
            Console.WriteLine($"🔌 [PLC {e.Ip}] 연결 상태 변경: {e.State}");
        };

        // 4. 연결 및 태그 등록 후 스캔 시작
        Console.WriteLine("⏳ MELSEC PLC 스캔 시작 중...");
        scanner.Connect();
        scanner.Scan(tags);

        Console.WriteLine("▶ 스캔 중입니다. 종료하려면 아무 키나 누르세요...");
        Console.ReadKey();

        // 5. 종료 처리
        Console.WriteLine("⛔ 스캔 종료 중...");
        scanner.StopScan();
        scanner.Disconnect();
    }
}

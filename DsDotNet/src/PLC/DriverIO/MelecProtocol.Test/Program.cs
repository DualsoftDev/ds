using MelsecProtocol;
using Dual.PLC.Common.FS;
using System;
using System.Collections.Generic;
using System.Threading;

class Program
{
    static void Main()
    {
        const int delay = 20;         // 스캔 주기 (ms)
        const int timeout = 2000;      // 통신 타임아웃 (ms)
        const bool isMonitorOnly = true;
        const string plcIp = "192.168.9.109";  // 실제 MELSEC PLC IP 주소

        var scanMgr = new MxScanManager(delay, timeout, isMonitorOnly);
        var scanner = scanMgr.CreateScanner(plcIp);

        // 테스트 태그 구성
        const int count = 1024;

        string[] bitDeviceTypes = { };//{ "X", "Y", "M", "L", "B", "SM", "SB", "DX", "DY", "F", "Z", "V" };
        string[] wordDeviceTypes = { /*"D", "W",*/ "T", "C" };//, "SD", "SW" };


        var tags = new List<TagInfo>();

        foreach (var dev in bitDeviceTypes)
        {
            for (int i = 0; i < count; i++)
            {
                string address = MxTagParser.ParseFromSegment(dev, i, 1);
                tags.Add(new TagInfo(
                    name: $"{dev}_bit_{i}",
                    address: address,
                    comment: $"Test Bit {address}",
                    isOutput: false
                ));
            }
        }

        foreach (var dev in wordDeviceTypes)
        {
            for (int i = 0; i < count; i++)
            {
                int offset = i * 1; // 워드는 10진수 주소 사용
                string address = MxTagParser.ParseFromSegment(dev, i * 16, 16);
                tags.Add(new TagInfo(
                    name: $"{dev}_word_{i}",
                    address: address,
                    comment: $"Test Word {address}",
                    isOutput: false
                ));
            }
        }

        scanner.TagValueChangedNotify += (s, e) =>
        {
            Console.WriteLine($"📡 [PLC {e.Ip}] {e.Tag.Name} ({e.Tag.Address}) = {e.Tag.Value}");
        };

        scanner.ConnectChangedNotify += (s, e) =>
        {
            Console.WriteLine($"🔌 [PLC {e.Ip}] 연결 상태 변경: {e.State}");
        };

        Console.WriteLine("⏳ MELSEC PLC 스캔 시작 중...");
        scanner.Connect();
        scanner.Scan(tags);

        Console.WriteLine("▶ 스캔 중입니다. 종료하려면 아무 키나 누르세요...");
        Console.ReadKey();

        Console.WriteLine("⛔ 스캔 종료 중...");
        scanner.StopScan();
        scanner.Disconnect();
    }
}

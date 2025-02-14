using Dual.PLC.TagParser.FS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using static DsXgComm.XGTScanModule;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Starting PLC Monitor Test...");

        // 테스트 케이스 정의
        var tagsPerPLC = new Dictionary<string, List<string>>
        {
            { "127.0.0.1", new List<string> { "P0", "P1" } },
            { "192.168.9.103", new List<string> { "M0000", "M0001" } },
            { "192.168.9.100", new List<string> { "%MX0000", "%MW0001" } }
        };
        // 유효 수집 주소로 변경
        var tagsValidPerPLC = tagsPerPLC.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Select(tag => tag.StartsWith("%") ? tag : LsXgkTagParser.ParseValidText(tag, true))
        );

        // PLC 모니터 엔진 초기화
        var scanModule = new XGTScan(tagsValidPerPLC.Keys, 10);

        // Connect 값 변경 이벤트 구독
        scanModule.ConnectChangedNotify += (obj, evt) =>
        {
            Console.WriteLine($"ConnectChanged [{evt.Ip}] {evt.State}");
        };

        // 태그 값 변경 이벤트 구독
        scanModule.TagValueChangedNotify += (obj, evt) =>
        {
            Console.WriteLine($"TagValueChanged [{evt.Ip}] {evt.Tag.TagName} -> {evt.Tag.Value}");
        };


        // 1️ **모니터링 시작**
        Console.WriteLine("\n############## Starting PLC Monitoring...");
        scanModule.Scan(tagsValidPerPLC);

        // 2️ **특정 PLC의 태그 업데이트 (`ScanUpdate1`)**
        Console.WriteLine("\n############## Updating Tags for 192.168.9.100...");
        var updatedTags1 = new List<string> { "%IX0010", "%IW0011" };
        scanModule.ScanUpdate("192.168.9.100", updatedTags1);

        // 3 **전체 PLC 모니터링 중지 (`Disconnect`)**
        Console.WriteLine("\n############## Stopping all PLC monitoring...");
        foreach (var plcIp in tagsValidPerPLC.Keys)
        {
            scanModule.Disconnect(plcIp);
        }

        // 다시 **모니터링 시작**
        Console.WriteLine("\n############## Starting PLC Monitoring...");
        scanModule.Scan(tagsValidPerPLC);

        Thread.Sleep(1000);
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }
}

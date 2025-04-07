using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DsMxComm;
using Dual.Common.Base.CS;
using static DsMxComm.MelsecScanModule;
using static DsMxComm.MxComponentModule;

class Program
{
    static void Main()
    {
        try
        {
            const int cnt = 10;
            // 테스트할 모든 장치 타입
            string[] deviceTypes = new string[]
            {
                //"X", "Y",
                "B",
                //"F", "Z", "V",  "M", "L","D", "W", "R", "T", "C", "ZR", "SM",
                //"SD", "SW", "SB", "DX", "DY"
            };

            // 16진수 형식으로 생성할 장치 타입
            HashSet<string> hexDevices = new HashSet<string>
            {
                "X", "Y", "B", "W", "SW", "SB", "DX", "DY"
            };

            int[] channels = new int[] { 0, 1 };
            MelsecScan scanModule = new MelsecScan(channels);
            // Connect 값 변경 이벤트 구독
            scanModule.ConnectChangedNotify += (obj, evt) =>
            {
                Console.WriteLine($"ConnectChanged [{evt.Ip}] {evt.State}");
            };

            // 태그 값 변경 이벤트 구독
            scanModule.TagValueChangedNotify += (obj, evt) =>
            {
                Console.WriteLine($"TagValueChanged [{evt.Ip}] {evt.Tag.Address} -> {evt.Tag.Value}");
            };


            // 각 장치 타입별 주소/값 배열을 저장할 리스트 (테스트용)
            var allDeviceData = new List<DeviceData>();


            foreach (string device in deviceTypes)
            {
                Console.WriteLine($"=== 테스트 장치 타입: {device} ===");
                string[] addresses = new string[cnt];
                short[] values = new short[cnt];

                for (int i = 0; i < cnt; i++)
                {
                    // 16진수 형식이면 i를 16진수 문자열, 아니면 기본 10진수 문자열로 변환
                    string formattedIndex = hexDevices.Contains(device) ? i.ToString("X") : i.ToString();
                    addresses[i] = $"{device}{formattedIndex}";
                    values[i] = (short)(i);
                    //values[i] = (short)(i%2);
                }

                allDeviceData.Add(new DeviceData
                {
                    DeviceType = device,
                    Addresses = addresses,
                    Values = values
                });

                Console.WriteLine(); // 장치 타입 간 구분
            }

            // 각 장치 타입에 대해 통신 테스트 수행
            foreach (var data in allDeviceData)
            {
                Console.WriteLine($"--- 통신 테스트: {data.DeviceType} ---");

                // ScanSingle 메서드 호출 (채널 0 사용)
                scanModule.Disconnect(0);
                var tags = scanModule.ScanSingle(0, data.Addresses);

                // 통신 업데이트를 시뮬레이션하기 위해 잠시 대기
                Thread.Sleep(100);
                var tagArray = tags.Values.ToArray();
                for (int i = 0; i < tags.Count; i++)
                {
                    tagArray[i].SetWriteValue(data.Values?[i]);
                }

                Console.WriteLine();
            }

            Console.WriteLine("모든 테스트 완료.");
            Console.ReadKey();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"예외 발생: {ex.Message}");
        }
        finally
        {
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}

// 각 장치 타입별 주소와 값 배열을 저장하기 위한 데이터 구조
class DeviceData
{
    public string? DeviceType { get; set; }
    public string[]? Addresses { get; set; }
    public short[]? Values { get; set; }
}

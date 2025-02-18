using DsMxComm;

class Program
{
    static void Main()
    {
        // ActUtlType64 객체 생성
        var plc = new PlcMxComponent("1.1.1.1", 5006);

        try
        {
            // PLC 연결
            if (!plc.Open())
            {
                Console.WriteLine($"MX Simulator 연결 실패!");
                return;
            }
            //var dataSet = new Dictionary<string, int>
            //{
            //    { "D100", 1 },
            //    { "D102", 1 },
            //    { "D104", 1 },
            //};
            var cnt = 512;
            short[] values = new short[cnt];
            string[] devices = new string[cnt];

            // "W0", "W1", ..., "W(cnt-1)" 형태로 디바이스 리스트 생성
            for (int i = 0; i < cnt; i++)
            {
                //if (i % 2 == 0)
                    devices[i] = $"K4X{i * 16:X}";
                //else
                    //devices[i] = $"W{i:X}";

                values[i] = Convert.ToInt16( i); // 테스트용 값 (0, 1, 2, ..., cnt-1)
            }

            plc.WriteDeviceRandom(devices, values);

            var rtn = plc.ReadDeviceRandom(devices);
            Console.WriteLine("다중 쓰기 성공:");
            //for (int i = 0; i < rtn.Length; i++)
            //{
            //    Console.WriteLine($"{devices[i]} {rtn[i]} 값 기록 완료");
            //}


            //int[] values = { 0xfffffff };
            //WriteDeviceRandom(plc, new string[] { "D100" }, values);

            //string[] devices = { "D100", "D101", "D102" };
            //ReadDeviceRandom(plc, devices);

        }
        catch (Exception ex)
        {
            Console.WriteLine($"예외 발생: {ex.Message}");
        }
        finally
        {
            // PLC 연결 종료
            plc.Close();
            Console.WriteLine("MX Simulator 연결 종료");
        }
    }

}

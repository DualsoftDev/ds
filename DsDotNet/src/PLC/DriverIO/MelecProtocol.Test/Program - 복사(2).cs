//using System;
//using System.IO;
//using System.Linq;
//using System.Net.Sockets;
//using System.Threading.Tasks;

//class Program
//{
//    static async Task Main(string[] args)
//    {
//        string ip = "192.168.9.109";
//        int port = 5002;

//        using TcpClient client = new TcpClient();
//        await client.ConnectAsync(ip, port);
//        using NetworkStream stream = client.GetStream();

//        // [1] 연속 읽기 테스트
//        Console.WriteLine("[D100~D101 연속 읽기]");
//        var readCmd = BuildReadCommand(0xA8, 100, 2);
//        await stream.WriteAsync(readCmd, 0, readCmd.Length);
//        byte[] response = new byte[256];
//        int len = await stream.ReadAsync(response, 0, response.Length);

//        if (len >= 15 && BitConverter.ToUInt16(response, 9) == 0x0000)
//        {
//            short d100 = BitConverter.ToInt16(response, 11);
//            short d101 = BitConverter.ToInt16(response, 13);
//            Console.WriteLine($"D100 = {d100:X4}h ({d100})");
//            Console.WriteLine($"D101 = {d101:X4}h ({d101})");
//        }
//        else
//        {
//            Console.WriteLine("연속 읽기 실패");
//        }

//        // [2] 랜덤 읽기 테스트
//        Console.WriteLine("\n[D100, D200 랜덤 리드]");
//        var randCmd = BuildRandomReadCommand(new (byte, int)[]
//        {
//            (0xA8, 100), // D100
//            (0xA8, 200)  // D200
//        });

//        await stream.WriteAsync(randCmd, 0, randCmd.Length);
//        byte[] randRes = new byte[256];
//        int randLen = await stream.ReadAsync(randRes, 0, randRes.Length);

//        if (randLen >= 15 && BitConverter.ToUInt16(randRes, 9) == 0x0000)
//        {
//            short d100 = BitConverter.ToInt16(randRes, 11);
//            short d200 = BitConverter.ToInt16(randRes, 13);
//            Console.WriteLine($"D100 = {d100:X4}h ({d100})");
//            Console.WriteLine($"D200 = {d200:X4}h ({d200})");
//        }
//        else
//        {
//            ushort endCode = BitConverter.ToUInt16(randRes, 9);
//            Console.WriteLine($"랜덤 리드 오류: EndCode = 0x{endCode:X4}");
//        }

//        Console.WriteLine("\n완료되었습니다. 아무 키나 누르세요...");
//        Console.ReadKey();
//    }
//    static byte[] BuildReadCommand(byte deviceCode, int address, ushort points)
//    {
//        using MemoryStream ms = new MemoryStream();
//        using BinaryWriter w = new BinaryWriter(ms);

//        // MC 3E Binary 프레임 Header
//        w.Write((ushort)0x0050); // Subheader
//        w.Write((byte)0x00);     // Network No
//        w.Write((byte)0xFF);     // PC No
//        w.Write((ushort)0x03FF); // I/O No
//        w.Write((byte)0x00);     // Station No

//        // Body 구성
//        using MemoryStream body = new MemoryStream();
//        using BinaryWriter bw = new BinaryWriter(body);

//        bw.Write((ushort)0x0010);      // Monitoring Timer
//        bw.Write((ushort)0x0401);      // Command
//        bw.Write((ushort)0x0000);      // Subcommand

//        // ✅ 24비트 주소 대응
//        bw.Write((byte)(address & 0xFF));         // LSB
//        bw.Write((byte)((address >> 8) & 0xFF));  // MID
//        bw.Write((byte)((address >> 16) & 0xFF)); // MSB
//        bw.Write(deviceCode);                     // 디바이스 코드 (e.g. 0xA8 = D)

//        bw.Write((ushort)points); // 읽을 워드 수

//        // 전체 길이 계산 후 완성
//        byte[] bodyBytes = body.ToArray();
//        w.Write((ushort)bodyBytes.Length); // Data Length
//        w.Write(bodyBytes);                // Body 삽입

//        return ms.ToArray();
//    }

//    static byte[] BuildRandomReadCommand((byte deviceCode, int address)[] devices)
//    {
//        using MemoryStream ms = new MemoryStream();
//        using BinaryWriter w = new BinaryWriter(ms);

//        // Header
//        w.Write((ushort)0x0050); // Subheader
//        w.Write((byte)0x00);     // Network No.
//        w.Write((byte)0xFF);     // PC No.
//        w.Write((ushort)0x03FF); // I/O No.
//        w.Write((byte)0x00);     // Station No.

//        using MemoryStream body = new MemoryStream();
//        using BinaryWriter bw = new BinaryWriter(body);

//        bw.Write((ushort)0x0010);       // Monitoring Timer
//        bw.Write((ushort)0x0403);       // Command
//        bw.Write((ushort)0x0000);       // Subcommand
//        bw.Write((byte)devices.Length); // Word count (must be ushort!)
//        bw.Write((byte)0);              // DWord count (ushort!)

//        foreach (var (code, addr) in devices)
//        {
//            bw.Write((byte)(addr & 0xFF));         // LSB
//            bw.Write((byte)((addr >> 8) & 0xFF));  // Mid
//            bw.Write((byte)((addr >> 16) & 0xFF)); // MSB (24비트 대응)
//            bw.Write(code);
//        }


//        byte[] bodyBytes = body.ToArray();
//        w.Write((ushort)bodyBytes.Length); // body length
//        w.Write(bodyBytes);

//        return ms.ToArray();
//    }
//}

#if X64
namespace Tx64
#else
namespace Tx86
#endif
open T

open System
open Xunit
open NUnit.Framework

open Engine.Common.FS
open System.Collections.Generic
open AddressConvert

[<AutoOpen>]
module XgCommLibSpec =

    type XgCOMCodes =
        | R_F = 0x24a
        | R_U = 0x24e
        | R_I = 0x24f
        | R_Q = 0x250
        | R_M = 0x251
        | R_A = 0x253

        | W_U = 0xc8
        | W_I = 0xc9
        | W_Q = 0xca
        | W_M = 0xcb
        | W_A = 0xcd

    let [<Literal>] MAX_RANDOM_READ_POINTS = 64
    let [<Literal>] MAX_ARRAY_BYTE_SIZE = 512   // 64*8

    let mutable listOfrBufs : byte[] list = []


    [<Struct>]
    type IData = { 
                    MomoryType : string                     //M W I Q ..
                    DataType : string                       //X B W D L
                    LWordName : string                      //변환된 LWord 메모리 이름  "ML0"
                    Offset : byte                           //변환된 LWord 메모리 안에서의 offset
                    Size : byte                             //byte size(X = 1, B = 1, W = 2, D = 4, L = 8)
                    mutable Location : int*int*int*int      // ( 1. listOfrBufs index, 2. Byte index in rBuf, (3. first bit index, 4. last bit index] )
                              //mutable DataArray : byte[]                    //Array.Sub로 buf에서 참조
                    //mutable ListOffset : int 
                    //mutable ArrayOffset : int
                 }


(*
    XGComLib summary : working API (V20 API 기준)
    ----------------
    - ReadRandomDevice
    - WriteRandomDevice
       - 단, 64점 이상인 경우, fail 함
    - WriteDevice_Bit
    - WriteDevice_Block
*)
[<Collection("SerialXgiGenerationTest")>]
type XgCOM10ReadTest() =
    inherit XgCOMBaseClass()

    [<Test>]
    member x.``Basic read/write test`` () =
        x.CommObject.IsConnected() === 1
        let plcId = x.CommObject.GetPLCID;

        let di = x.Factory.CreateDevice()
        di.ucDeviceType <- Convert.ToByte('M')
        di.ucDataType <- Convert.ToByte('B')

        let wBuf = Array.zeroCreate<byte>(MAX_ARRAY_BYTE_SIZE)
        let rBuf = Array.zeroCreate<byte>(MAX_ARRAY_BYTE_SIZE)
        x.CommObject.RemoveAll()
        for i = 0 to MAX_RANDOM_READ_POINTS-1 do
            di.lSize <- 8
            di.lOffset <- i * 8
            wBuf[i] <- byte i
            x.CommObject.AddDeviceInfo(di)


        // working : 단 random device 갯수가 64 이하 일 때...
        x.CommObject.ReadRandomDevice(rBuf) === 1


[<Collection("SerialXgiGenerationTest")>]
type XgCOM20ReadTest() =
    inherit XgCOMBaseClass20()



    (*
        bit 0 ~ 7   |   0   |   1   |   2   |   3   |   4   |   5   |   6   |   7   |
            ||
        byte0[0..7] |   7   |   6   |   5   |   4   |   3   |   2   |   1   |   0   |
    *)
    member x.ReadDevice_Bit(bstrDevice:string, nBitOffset:int, lpValue: byref<int>): int =
        x.CommObject.RemoveAll()
        let _offset = nBitOffset / 8
        let _size = nBitOffset % 8
        let di = x.CreateDevice((char)bstrDevice, 'B', 1, _offset)
        x.CommObject.AddDeviceInfo(di)
        let rBuf = Array.zeroCreate<byte>(1)
        if x.CommObject.ReadRandomDevice(rBuf) <> 1 then
            0
        else if (rBuf[0] &&& pown 2uy _size) > 0uy then     //bit:true      lpValue = 1
            lpValue <- 1
            1
        else if (rBuf[0] &&& pown 2uy _size) = 0uy then     //bit:false     lpValue = 0
            lpValue <- 0
            1
        else
            0


    //Read((int)XgCOMCodes.R_M, rBuf, 1, 0)
    // x.CommObject.ReadDevice_Block("M", offset, &rBuf[0], MAX_ARRAY_BYTE_SIZE-1, ref nRead)




    [<Test>]
    member x.``Not working: Read/Write`` () =
        let wBuf = Array.zeroCreate<byte>(MAX_ARRAY_BYTE_SIZE)
        let rBuf = Array.zeroCreate<byte>(MAX_ARRAY_BYTE_SIZE)
        rBuf[0] = 0xFFuy

        let sWrite = x.CommObject.Write((int)XgCOMCodes.W_M, wBuf, 1, 0)
        let sRead = x.CommObject.Read((int)XgCOMCodes.R_M, rBuf, 1, 0)

        (*
            1. COM 호출 return 값 모두 0 임.  sRead, sWrite
            1. rBuf[0] 값이 0xFF 로 변경되지 않음.
         *)
        sWrite === 0     // 1 이 되어야 할 것 같은데...
        sRead  === 0     // 1 이 되어야 할 것 같은데...
        rBuf[0] === 0uy    // 1 이 되어야 함...

        // does *NOT* working
        //x.CommObject.Command((int)XgCOMCodes.W_M, wBuf, 1024, 0) =!= 0
        //x.CommObject.Command((int)XgCOMCodes.R_M, rBuf, 1024, 0) =!= 0



    [<Test>]
    member x.``Not working: ReadDevice_Bit`` () =
        let offset = 16*10
        x.CommObject.WriteDevice_Bit("M", offset, 1) === 1      // WriteDevice_Bit 는 정상 동작
        let mutable buf:int = 0
        let sRead = x.CommObject.ReadDevice_Bit("M", offset, &buf)
        // COM 호출 return 값 sRead 가 0 임
        sRead === 0     // 1 이 되어야 할 것 같은데...
        buf === 0       // 1 이 되어야 함...
        noop()



    [<Test>]
    member x.``custom ReadDevice_Bit`` () =
        let targetValue = 1                                                 //1 or 0
        let offset = 65
        x.CommObject.WriteDevice_Bit("M", offset, targetValue) === 1        // WriteDevice_Bit 는 정상 동작
        let mutable buf = 0
        let sRead = x.ReadDevice_Bit("M", offset, &buf)                     //새로 만든 ReadDevice_Bit
        sRead === 1
        buf === targetValue
        noop()


    [<Test>]
    member x.``Not working: ReadDevice_Block`` () =
        (*
           ReadDevice_Block 호출 시, test process crash.
           The active test run was aborted. Reason: Test host process crashed *)
        //let mutable nRead = 0
        //let rBuf = Array.zeroCreate<byte>(MAX_ARRAY_BYTE_SIZE)
        //x.CommObject.ReadDevice_Block("M", offset, &rBuf[0], MAX_ARRAY_BYTE_SIZE-1, ref nRead)
        ////x.CommObject.ReadDevice_Block("M", offset, &rBuf[0], MAX_ARRAY_BYTE_SIZE-1, &nRead)

        noop()

    [<Test>]
    member x.``WriteRandomDevice`` () =
        let di = x.CreateDevice('M', 'B')

        let wBuf = Array.zeroCreate<byte>(MAX_ARRAY_BYTE_SIZE)
        x.CommObject.RemoveAll()
        for i = 0 to MAX_RANDOM_READ_POINTS-1 do
            di.lOffset <- 180 + i * 8
            wBuf[i] <- byte i
            x.CommObject.AddDeviceInfo(di)
        x.CommObject.WriteRandomDevice(wBuf) === 1


    [<Test>]
    member x.``read/write random device test`` () =
        x.CommObject.IsConnected() === 1
        let plcId = x.CommObject.GetPLCID;

        let di = x.CreateDevice('M', 'B')

        let wBuf = Array.zeroCreate<byte>(MAX_ARRAY_BYTE_SIZE)
        let rBuf = Array.zeroCreate<byte>(MAX_ARRAY_BYTE_SIZE)
        x.CommObject.RemoveAll()
        for i = 0 to MAX_RANDOM_READ_POINTS-1 do
            di.lOffset <- 100 + i * 8
            wBuf[i] <- byte i
            x.CommObject.AddDeviceInfo(di)
        x.CommObject.WriteRandomDevice(wBuf) === 1
        x.CommObject.ReadRandomDevice(rBuf) === 1
        for i = 0 to MAX_RANDOM_READ_POINTS-1 do
            rBuf[i] === wBuf[i]

        noop()
    [<Test>]
    member x.``write device bit test`` () =
        for i = 100 to 1024 do
            x.CommObject.WriteDevice_Bit("M", i, 0) === 1

    [<Test>]
    member x.``WriteDevice_Block`` () =
        let offset = 512
        let wBuf = [| 0uy .. byte (MAX_ARRAY_BYTE_SIZE-1) |]
        let clearBuf = Array.zeroCreate<byte> 512
        x.CommObject.WriteDevice_Block("M", offset, &(wBuf[0]), MAX_ARRAY_BYTE_SIZE) === 1      // WriteDevice_Block 는 정상 동작


    [<Test>]
    member x.``Write Q0 bit test`` () =
        x.CommObject.WriteDevice_Bit("Q", 0, 1) === 1
    [<Test>]
    member x.``Write Q0 and read it test`` () =
        x.CommObject.WriteDevice_Bit("Q", 0, 1) === 1

        x.CommObject.RemoveAll()
        let di = x.CreateDevice('Q', 'B', 8, 0)
        x.CommObject.AddDeviceInfo(di)
        let rBuf = Array.zeroCreate<byte>(8)
        x.CommObject.ReadRandomDevice(rBuf) === 1
        (rBuf[0] &&& 1uy) === 1uy


    [<Test>]
    member x.``Write bit and read random test`` () =
        //let start = 16*5
        let start = 0

        //for i = start to 1023 do
        //    x.CommObject.WriteDevice_Bit("M", i, 1) === 1

        let di = x.CreateDevice('M', 'B')

        let rBuf = Array.zeroCreate<byte>(MAX_ARRAY_BYTE_SIZE)
        x.CommObject.RemoveAll()
        for i = start to start+MAX_RANDOM_READ_POINTS-1 do
            di.lOffset <- i * 8
            x.CommObject.AddDeviceInfo(di)

        x.CommObject.ReadRandomDevice(rBuf) === 1
        noop()


    [<Test>]
    member x.``Write bit and read random with offset test`` () =
        x.CommObject.RemoveAll()
        let start = 1

        //for i = start to 1023 do
        //    x.CommObject.WriteDevice_Bit("M", i, 1) === 1

        let di = x.CreateDevice('M', 'B')

        let rBuf = Array.zeroCreate<byte>(MAX_ARRAY_BYTE_SIZE)
        for i in start*8 .. 8 .. ((start+MAX_RANDOM_READ_POINTS)*8 - 1) do
            //di.lOffset <- i * 8
            di.lOffset <- i
            x.CommObject.AddDeviceInfo(di)


        x.CommObject.ReadRandomDevice(rBuf) === 1
        noop()

    [<Test>]
    member x.``Read random of Q test`` () =
        x.CommObject.RemoveAll()

        let rBuf = Array.zeroCreate<byte>(8)
        let di = x.CreateDevice('Q', 'B')
        x.CommObject.AddDeviceInfo(di)
        x.CommObject.ReadRandomDevice(rBuf) === 1
        noop();


    [<Test>]
    member x.``Read random of I test`` () =
        x.CommObject.RemoveAll()

        let di = x.CreateDevice('I', 'B')

        let rBuf = Array.zeroCreate<byte>(8)
        x.CommObject.AddDeviceInfo(di)

        x.CommObject.ReadRandomDevice(rBuf) === 1
        noop();


    /// Slot change: Does NOT work
    [<Test>]
    member x.``Read random of Q 2nd slot test`` () =
        x.CommObject.RemoveAll()

        let rBuf = Array.zeroCreate<byte>(8)
        let di = x.CreateDevice('Q', 'B')
        x.CommObject.AddDeviceInfo(di)

        (* SetBaseSlot 은 동작하지않고, 위의 di.lOffset 에 8 을 넣었을 때(8byte skip 해서 QX0.1.??) 에는 제대로 읽어 들임.  *)
        x.CommObject.SetChNo(0uy)               // retuns unit ???
        x.CommObject.SetBaseSlot(0uy, 1uy)      // retuns unit ???

        x.CommObject.ReadRandomDevice(rBuf) === 1
        noop();



    [<Test>]
    member x.``Clear bit test`` () =
        for i = 0 to 1023 do
            x.CommObject.WriteDevice_Bit("M", i, 0) === 1


    [<Test>]
    member x.``Read random of "X" "B" W D L test`` () =
        (*memoryType는 B와 X만 가능 *)
        x.CommObject.RemoveAll()

        let di0 = x.CreateDevice('I', 'B', 2 ,0)
        let di1 = x.CreateDevice('M', 'B', 8 ,128)
        let di2 = x.CreateDevice('W', 'B', 1 ,7)
        let di3 = x.CreateDevice('Q', 'B', 1 ,1)

        let di4 = x.CreateDevice('Q', 'X', 0 ,1)
        let di5 = x.CreateDevice('Q', 'X', 1 ,1)
        let di6 = x.CreateDevice('Q', 'X', 2 ,1)
        let di7 = x.CreateDevice('Q', 'X', 3 ,1)
        let di8 = x.CreateDevice('Q', 'X', 4 ,1)
        let di9 = x.CreateDevice('Q', 'X', 5 ,1)
        let di10 = x.CreateDevice('Q', 'X', 6 ,1)
        let di11 = x.CreateDevice('Q', 'X', 7 ,1)

        let rBuf0 = Array.zeroCreate<byte>(2 + 8 + 1 + 1 + 8)
        x.CommObject.AddDeviceInfo(di0)
        x.CommObject.AddDeviceInfo(di1)
        x.CommObject.AddDeviceInfo(di2)
        x.CommObject.AddDeviceInfo(di3)
        x.CommObject.AddDeviceInfo(di4)
        x.CommObject.AddDeviceInfo(di5)
        x.CommObject.AddDeviceInfo(di6)
        x.CommObject.AddDeviceInfo(di7)
        x.CommObject.AddDeviceInfo(di8)
        x.CommObject.AddDeviceInfo(di9)
        x.CommObject.AddDeviceInfo(di10)
        x.CommObject.AddDeviceInfo(di11)

        x.CommObject.ReadRandomDevice(rBuf0) === 1
        let out0 = rBuf0
        let mutable bitBuf = 0

        let mutable i = 0
        for i = 0 to 7 do
            let b = int(rBuf0.[i + 12])
            bitBuf <- bitBuf + b * int(Math.Pow (2, i))
             
        rBuf0[11]  === byte(bitBuf) 
        noop();





        (* 새 프로젝트에 옮길 것 *)
    [<Test>]
    member x.``In progress.. : Input memory  initialize Test`` () =
        (* 전처리, 메모리 정복struct 생성 , dictionary생성 , LWords 메모리주소 리스트 생성 *)
        let dict = new Dictionary<string, IData>()
        let mutable lWords = Set.empty

        //let TestInputset = [|"%WX5"; "MX8"; "%WX15"; "QX17"; "%IX15"; "QX15"; "%MW3"; "%MX15"; "%MB15"; "%WX21"; "%WX151"; "%MX155"; "%WX32"; "%MX152"; "%MX151"; "%MX154"; "MX1.0.1"|]
        let TestInputset = [|"QX1.1.5";"IX1.0.2";"IX0.3.5";"IX0.0.5";"%WX5"; "MX8"; "%WX15"; "QX17"; "%IX15"; "QX15";"%MX15";"%WX21"; "%WX151"; "%MX155"; "%WX32"; "%MX152"; "%MX151"; "%MX154"; "MX1.0.1"|]

        let inputSet:string[] = TestInputset |> Array.map(fun s -> s.Replace("%",""))        
        for item in inputSet do
            if not <| dict.ContainsKey(item) then
                let mutable _address: byte = 0uy
                let convertBit = getBitOffset ("%" + item)
                //Byte.TryParse(item.[2..item.Length-1], &_address) === true
                let _memory =  item.[0].ToString()
                let _data = item.[1].ToString()
                let _size = 
                    match _data with
                    | "X"-> 1uy
                    | "B"-> 1uy
                    | "W"-> 2uy
                    | "D"-> 4uy
                    | "L"-> 8uy
                    | _ -> 1uy

                //let _offsetSnap = 
                //    match _data with
                //    | "X"-> 64uy
                //    | "B"-> 8uy
                //    | "W"-> 4uy
                //    | "D"-> 2uy
                //    | "L"-> 1uy
                //    | _ -> 1uy
                //let testDiv = uint64(convertBit)/64uy
                //let _fullLWord = item.[0].ToString() + "L" + (_address/_offsetSnap).ToString()
                let _fullLWord = item.[0].ToString() + "L" + (convertBit/64).ToString()

                let _value = {
                    MomoryType = item.[0].ToString()
                    DataType = item.[1].ToString()
                    LWordName = _fullLWord
                    Offset = byte (convertBit % 64)
                    Size = _size
                    Location = (0,0,0,0)
                }

                dict.[item] <- _value
                (*LWords 리스트 중복 제거 -> list err -> set으로 자동으로 날림*)
                lWords <- Set.add _fullLWord lWords

        (*LWord 단위 deviceinfo 생성, readbuf array 만들기*)
        let mutable _offset = 0
        for item in lWords do
            let s = item.[2..item.Length-1]
            Int32.TryParse(item.[2..item.Length-1], &_offset) === true
            let di = x.CreateDevice(item.[0], 'B', 8, _offset * 8 )
            x.CommObject.AddDeviceInfo(di)
        //let rBuf = Array.zeroCreate<byte>(lWords.Count) 

        //data buffer array set 만들기. MAX_ARRAY_BYTE_SIZE 를 넘길 때마다 갯수를 늘린다.
        let rBuf = Array.zeroCreate<byte>(MAX_ARRAY_BYTE_SIZE)      // + rBuf는 하나만 만든다. list의 array를 돌아가면서 읽고 비교하고 덮어쓴다.
        x.CommObject.ReadRandomDevice(rBuf) === 1

        
            
        (*IData의 구조체에 참조 배열 넣기*)
        (*참조 배열 모니터링 등록하기 - 모니터링 구현하기*)
        (*readbuf 최대크기를 넘어가면, array 하나 더 만든 후 list에 넣어 관리*)
        noop()
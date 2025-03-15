namespace Dsu.PLCConverter.FS.XgiSpecs
open System

open System
open System.Xml.Serialization
open System.IO
open System.Collections.Generic
open Dsu.PLCConverter.FS

[<AutoOpen>]
module XgiOptionModule =

   /// Xgi로 변환하기 위한 옵션 객체
    type XgiOption() =
        // MELSEC 영역 시작 기본 주소 및 설정을 위한 mutable 필드들
        static let mutable xgiConfig:XgiConfig = XgiConfig() // XgiConfig 객체 생성

        /// L 영역의 word 단위 갯수
        //static let mutable lAreaCount = 0
        static let mutable nSelectCPU = 5
        static let mutable sPathCommandMapping = ""
        static let mutable sPathFBList = ""
        static let mutable maxIQLevelM = 32

        // 영역 시작 설정
        static member Config with get() = xgiConfig and set(value) = xgiConfig <- value
        //static member LAreaCount with get() = lAreaCount and set(value) = lAreaCount <- value
      
      // CPU와 관련된 설정
        static member SelectCPU with get() = nSelectCPU and set(value) = nSelectCPU <- value
        static member PathCommandMapping with get() = sPathCommandMapping and set(value) = sPathCommandMapping <- value
        static member PathFBList with get() = sPathFBList and set(value) = sPathFBList <- value
        static member MaxIQLevelM with get() = maxIQLevelM and set(value) = maxIQLevelM <- value

        // 각 CPU 타입의 이름, ID, 및 최대 용량 설정
        static member CPUs = [| "XGI-CPUE"; "XGI-CPUH"; "XGI-CPUS";  "XGI-CPUU";  "XGI-CPUUN"; "XGI-CPUZ3" ; "XGI-CPUZ5" ; "XGI-CPUZ7" |]
        static member CPUsID = [| "106"; "102"; "104"; "100"; "111"; "703"; "705"; "700" |]
        static member I_CPUMax = [| 2048; 8192; 2048;  8192;  8192; 10240; 10240; 10240 |]
        static member Q_CPUMax = [| 2048; 8192; 2048;  8192;  8192; 10240; 10240; 10240 |]
        static member M_CPUMax = [| 16384; 131072; 32768;  131072;  262144; 262144; 524288; 1048576 |]
        static member L_CPUMax = [| 11264; 11264; 11264;  11264;  11264; 11264; 11264; 11264 |]
        static member N_CPUMax = [| 25088; 25088; 25088;  25088;  25088; 25088; 25088; 25088 |]
        static member K_CPUMax = [| 2100; 8400; 2100;  8400;  8400; 9216; 9216; 9216 |]
        static member U_CPUMax = [| 1024; 4096; 2048;  4096;  4096; 4096; 4096; 4096 |]
        static member R_CPUMax = [| 16384; 32768; 32768;  32768;  32768; 32768; 32768; 32768 |]
        static member A_CPUMax = [| 32768; 262144; 65536;  262144;  524288; 524288; 1048576; 2097152 |]
        static member W_CPUMax = [| 16384; 65536; 32768;  65536;  524288; 1048576; 1048576; 1048576 |]
        static member F_CPUMax = [| 2048; 2048; 2048;  2048;  4096; 65536; 65536; 65536 |]


        static member MaxIQLevelS = 16 // CPUUN 기준 slot
        static member MaxIQLevelB = 128 // CPUUN 기준 base
        static member MaxMBits = 262144 // CPUUN 기준
        static member MaxWBits = 262144 // CPUUN 기준
        static member MaxRBits = 32768  // CPUUN 기준

        // Mapping을 설정 및 변환하는 멤버
        static member MappingSys = xgiConfig.ListSystemAddress |> Seq.map(fun s -> s.Split(';')) |> Seq.map (fun a -> a.[0], a.[1]) |> dict
        static member SetUserMappingIO(mappings:seq<string*string>)=
                              xgiConfig.ListSystemAddress.Clear()
                              mappings |> Seq.iter (fun (k, v)-> xgiConfig.ListSystemAddress.Add $"{k};{v}")
        
        static member MappingIOTuple = xgiConfig.ListIOAddress |> Seq.map(fun s -> s.Split(';')) |> Seq.map (fun a -> a.[0], a.[1]) 
        static member MappingIO = XgiOption.MappingIOTuple |> dict
        static member SetMappingIO(mappings:seq<string*string>)=
                              xgiConfig.ListIOAddress.Clear()
                              mappings |> Seq.iter (fun (k, v)-> xgiConfig.ListIOAddress.Add $"{k};{v}")
        
        // I/O 매핑을 변환하기 위한 함수
        static member private getMappingIO xy =
            let getBaseSlotModule xgiSpec =
                match xgiSpec with
                | ActivePattern.RegexPattern @"%([IQM])([XBWD])(\d+)\.\[(\d+)\]\.\[(\d+)\]$" [iom; xw; d1; d2; d3] -> int d1, int d2, (int d3)
                | _ -> failwith (sprintf "Invalid address spec [%s]" xgiSpec)

            XgiOption.MappingIOTuple 
            |> Seq.filter (fun (mel, xgi) -> mel.ToUpper().StartsWith xy)
            |> Seq.map (fun (mel, xgi) -> Convert.ToInt32(mel.Substring(1, (mel.Length - 1)), 16), getBaseSlotModule xgi)

        static member MappingIO_X with get() = XgiOption.getMappingIO "X"
        static member MappingIO_Y with get() = XgiOption.getMappingIO "Y"
        

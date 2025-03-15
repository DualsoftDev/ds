namespace Dsu.PLCConverter.FS.XgiSpecs
open System

open System
open System.Xml.Serialization
open System.IO
open System.Collections.Generic
[<AutoOpen>]
module XgiConfigModule =

    //[<CLIMutable>]
    type XgiConfig() =
        /// 0 ~ 6 : "XGI-CPUE"; "XGI-CPUH"; "XGI-CPUS"; "XGI-CPUS/P"; "XGI-CPUU"; "XGI-CPUU/D"; "XGI-CPUUN"
        member val TimerLowSpeed = 0 with get, set
        member val TimerHighSpeed = 0 with get, set

        member val ListSystemAddress = ResizeArray<string>() with get, set    // meslecAddress;xgiAddress
        member val ListIOAddress = ResizeArray<string>() with get, set  //melsecIOSlot;xgiIOSlot
        
        member val LAreaCount = 0 with get, set
        member val MaxIQLevelM = 0 with get, set

        member val MSmartAreaStart = 0 with get, set
        // Bit 영역
        member val BAreaStart = 0 with get, set
        member val MAreaStart = 0 with get, set
        member val LAreaStart = 0 with get, set
        member val FAreaStart = 0 with get, set
        member val VAreaStart = 0 with get, set
        member val SAreaStart = 0 with get, set
        member val SBAreaStart = 0 with get, set
        member val FXAreaStart = 0 with get, set
        member val FYAreaStart = 0 with get, set
        // Word 영역
        member val WAreaStart = 0 with get, set
        member val DAreaStart = 0 with get, set
        member val SWAreaStart = 0 with get, set
        member val FDAreaStart = 0 with get, set
        member val ZAreaStart = 0 with get, set
        member val RAreaStart = 0 with get, set
        member val ZRAreaStart = 0 with get, set
        // Area 타입
        member val MSmartAreaType = "M" with get, set
        member val WAreaType = "M" with get, set
        member val FAreaType = "M" with get, set
        member val MAreaType = "M" with get, set
        member val LAreaType = "M" with get, set
        member val VAreaType = "M" with get, set
        member val SAreaType = "M" with get, set
        member val SBAreaType = "M" with get, set
        member val BAreaType = "M" with get, set
        member val FXAreaType = "M" with get, set
        member val FYAreaType = "M" with get, set
        member val DAreaType = "M" with get, set
        member val SWAreaType = "M" with get, set
        member val FDAreaType = "M" with get, set
        member val ZAreaType = "M" with get, set
        member val RAreaType = "M" with get, set
        member val ZRAreaType = "M" with get, set

        // XML로 저장
        member this.SaveToXml(filePath: string) =
            let serializer = XmlSerializer(typeof<XgiConfig>)
            use writer = new StreamWriter(filePath)
            serializer.Serialize(writer, this)

        // XML에서 읽기
        static member LoadFromXml(filePath: string) =
            let serializer = XmlSerializer(typeof<XgiConfig>)
            use reader = new StreamReader(filePath)
            serializer.Deserialize(reader) :?> XgiConfig

namespace DsXgComm

open Dual.Common.Core.FS
open XGCommLib
open System
open System.Threading
open System.Diagnostics
open System.Collections
open System.Collections.Generic
open System.Reactive.Subjects

[<AutoOpen>]
module XGTagModule =
    
    let [<Literal>] MAX_RANDOM_BYTE_POINTS = 64
    let [<Literal>] MAX_ARRAY_BYTE_SIZE = 512   // 64*8
   

    [<DebuggerDisplay("{Tag}=>{LWordTag}, {BitOffset}, {LWordOffset}:{StartBitOffset}")>]
    type XgTagInfo(fenetTagInfo:LsFEnetTagInfo) =
        let ti = fenetTagInfo
        member x.Tag = ti.Tag
        member x.Device = ti.Device
        member x.DataType = ti.DataType
        member x.BitOffset = ti.BitOffset

        member val LWordOffset = -1 with get, set
        /// 주어진 byte array 에서 현재 tag 에 해당하는 bit 의 값을 읽어 낼 수 있는 방법 정의
        member val BitSetChecker = fun (bs:byte[]) -> false with get, set
        /// FormByteOffset 기준 bit offset
        member x.ByteOffset = x.BitOffset / 8
        member x.ByteSize   =  
                match x.DataType with
                | DataType.Bit -> 1         //bit randomwrite using 1 byte
                | DataType.Byte -> 1
                | DataType.Word -> 2
                | DataType.DWord -> 4
                | DataType.LWord -> 8
                | DataType.Continuous -> 
                    failwithlog $"Unsupported device type Continuous"
        member x.RandomReadWriteDataType   =  
            let t = 
                match x.DataType with
                | DataType.Bit ->   'X'
                | DataType.Byte ->  'B'
                | DataType.Word ->  'B'
                | DataType.DWord -> 'B'
                | DataType.LWord -> 'B'
                | DataType.Continuous -> 
                    failwithlog $"Unsupported device type Continuous"
            Convert.ToByte t

        /// FormLWordOffset 기준 bit offset
        member x.StartBitOffset = x.BitOffset % 64
        /// 현재 tag 의 값을 LWord 환산 했을 때의 이름
        member x.LWordTag = $"%%{x.Device}L{x.BitOffset/64}"
        member val WriteValue:obj = null with get, set //쓰기후 WriteDevices에서 null로 초기화
        member val Value:obj = null with get, set 
    
    let dTypeChar deviceType =
                match deviceType with
                | DeviceType.M -> 'M'
                | DeviceType.I -> 'I'
                | DeviceType.Q -> 'Q'
                | DeviceType.W -> 'W'
                | DeviceType.R -> 'R'
                | DeviceType.L -> 'L'
                | DeviceType.F -> 'F'
                | _ -> failwithlog $"Unsupported device type {deviceType}"

    let dValueBytes (tag:XgTagInfo) =
        let v = tag.WriteValue
        match tag.DataType  with
        |DataType.Bit   -> assert(v :? bool);   BitConverter.GetBytes(Convert.ToBoolean(v))
        |DataType.Byte  -> assert(v :? uint8);  [| v :?> uint8 |]
        |DataType.Word  -> assert(v :? uint16); BitConverter.GetBytes(Convert.ToUInt16 (v))
        |DataType.DWord -> assert(v :? uint32); BitConverter.GetBytes(Convert.ToUInt32 (v))
        |DataType.LWord -> assert(v :? uint64); BitConverter.GetBytes(Convert.ToUInt64 (v))
        |DataType.Continuous ->
            failwithlog $"Unsupported device type DataType.Continuous"

    let dValueObj (tag:XgTagInfo, byteArr:byte array) =
            match tag.DataType  with
            |DataType.Bit   ->  assert(byteArr.length() = 1);BitConverter.ToBoolean(byteArr) |> box
            |DataType.Byte  ->  assert(byteArr.length() = 1);byteArr[0] |> box
            |DataType.Word  ->  assert(byteArr.length() = 2);BitConverter.ToUInt16(byteArr)   |> box
            |DataType.DWord ->  assert(byteArr.length() = 4);BitConverter.ToUInt32(byteArr)   |> box
            |DataType.LWord ->  assert(byteArr.length() = 8);BitConverter.ToUInt64(byteArr)   |> box
            |DataType.Continuous ->
                failwithlog $"Unsupported device type DataType.Continuous"

    let chunkBySumByteSize maxSize (tags:XgTagInfo seq) =
        let tags = tags.ToFSharpList()
        let rec loop acc sum chunk (remaining:XgTagInfo list) =
            match remaining with
            | x::xs when sum + x.ByteSize <= maxSize ->
                loop (x::acc) (sum + x.ByteSize) chunk xs
            | x::xs ->
                loop [x] x.ByteSize (List.rev acc::chunk) xs
            | [] when acc = [] -> chunk
            | [] -> (List.rev acc)::chunk
        
        tags |> loop [] 0 [] |> List.rev


    let getBuffer (tags:XgTagInfo list, bRead:bool) =
        let sumByte = tags |> Seq.map(fun t ->t.ByteSize) |> Seq.sum
        let buff = Array.zeroCreate<byte>(sumByte)
        let mutable offset = 0
        for i = 0 to tags.Length-1 do
            let index = i+offset
            let bytes = 
                if bRead
                then Array.zeroCreate<byte>(tags[i].ByteSize)
                else dValueBytes tags[i]

            for b = 0 to bytes.Length-1 do
                buff[index+b] <- bytes[b]

            offset <- offset + tags[i].ByteSize-1

        buff

    let PLCTagSubject = new Subject<XgTagInfo>()
    let bufferToTagValue (tags:XgTagInfo list, buff:byte array) =
        let mutable offset = 0
        for i = 0 to tags.Length-1 do
            let index = i+offset
            let buffArr = buff[index..index+tags[i].ByteSize-1]
            let newValue =dValueObj(tags[i], buffArr)
            let oldValue =tags[i].Value 
            if oldValue  <> newValue
            then 
                logDebug $"Tag change detected: {tags[i].Tag} = {tags[i].Value}"
                tags[i].Value <- newValue
                PLCTagSubject.OnNext( tags[i])

            offset <- offset + tags[i].ByteSize-1

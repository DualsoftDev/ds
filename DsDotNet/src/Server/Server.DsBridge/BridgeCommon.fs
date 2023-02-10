namespace Server.DsBridge

open System
open System.IO
open System.Collections
open System.Collections.Generic
open System.Threading.Tasks
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Server.Common.Kafka
open Server.Common.NMC
open Server.Common.NMF
open WMX3ApiCLR

[<AutoOpen>]
module BridgeCommon =
    type IBridgeHandler<'T> =
        abstract Transfer : int * int -> unit
        abstract Receive  : ('T array * string -> unit) -> unit

    type DsPaixHandlerNMC(_ip:string, _numIn:int, _numOut:int) =
        let mutable isAvailable = false
        let ip      = _ip.Split('.')[3] |> int16
        let inputs:int16[]  = Array.zeroCreate _numIn
        let outputs:int16[] = Array.zeroCreate _numOut

        let pingChecker() = 
            let nRet = NMC2.nmc_PingCheck(ip, 50);
            match nRet |> int with
            | 0 -> 0
            | _ -> 
                printfn "nmc ping check error"
                1

        do
            let pingCheckRes = pingChecker()
            let devOpenRes   = int(NMC2.nmc_OpenDevice(ip))
            if pingCheckRes + devOpenRes = 0 then
                isAvailable <- true
            else
                isAvailable <- false
        
        override x.Finalize() = NMC2.nmc_CloseDevice(ip)

        interface IBridgeHandler<int16> with
            member x.Transfer(_idx, _onoff) = 
                if isAvailable then
                    NMC2.nmc_SetDIOOutputBit(ip, int16(_idx), int16(_onoff))
                    |> ignore
                else
                    failwith "transfer error"
            member x.Receive _receiver = 
                while isAvailable do
                    NMC2.nmc_GetDIOInput(ip, inputs) |> ignore
                    _receiver(inputs, "paix-en")

        member x.Transfer _idx _onoff = 
            (x :> IBridgeHandler<int16>).Transfer(_idx, _onoff)
        member x.Receive _receiver = 
            (x :> IBridgeHandler<int16>).Receive _receiver

    type DsPaixHandlerNMF(_ip:string, _numIn:int, _numOut:int) =
        let mutable isAvailable = false
        let ip      = _ip.Split('.') |> Array.map(int16)
        let inputs:int16[]  = Array.zeroCreate _numIn
        let outputs:int16[] = Array.zeroCreate _numOut

        let pingChecker() = 
            let nRet = NMF.nmf_PingCheck(ip[3], ip[0], ip[1], ip[2], 50);
            match nRet |> int with
            | 0 -> 0
            | _ -> 
                printfn "nmf ping check error"
                1

        do
            let pingCheckRes = pingChecker()
            let devOpenRes   = int(NMF.nmf_Connect(ip[3], ip[0], ip[1], ip[2]))
            if pingCheckRes + devOpenRes = 0 then
                isAvailable <- true
            else
                isAvailable <- false
        
        override x.Finalize() = NMF.nmf_Disconnect(ip[3]);

        interface IBridgeHandler<int16> with
            member x.Transfer(_idx, _onoff) = 
                if isAvailable then
                    NMF.nmf_DOSetPin(ip[3], int16(_idx), int16(_onoff))
                    |> ignore
                else
                    failwith "transfer error"
            member x.Receive _receiver = 
                while isAvailable do
                    NMF.nmf_DIGet(ip[3], inputs) |> ignore
                    _receiver(inputs, "paix-en")

        member x.Transfer _idx _onoff = 
            (x :> IBridgeHandler<int16>).Transfer(_idx, _onoff)
        member x.Receive _receiver = 
            (x :> IBridgeHandler<int16>).Receive _receiver

    type DsWMX3Handler(_numIn:int, _numOut:int) =
        let mutable disposed    = false
        let mutable isAvailable = false
        let wmx3Lib   = new WMX3Api()
        let wmx3LibIO = new Io(wmx3Lib)
        let enStatus  = new EngineStatus()
        let inputs:byte[] ref  = ref (Array.zeroCreate _numIn)
        let outputs:byte[] ref = ref (Array.zeroCreate _numOut)
    
        let cleanup(disposing:bool) = 
            if not disposed then
                disposed <- true
                if disposing then
                    wmx3Lib.StopCommunication(uint32(0xFFFFFFFF)) |> ignore
                    wmx3Lib.CloseDevice() |> ignore
                    wmx3LibIO.Dispose()
                    wmx3Lib.Dispose()

        interface IDisposable with
            member self.Dispose() =
                cleanup(true)
                GC.SuppressFinalize(self)

        override self.Finalize() = 
            cleanup(false)
            
        interface IBridgeHandler<byte> with
            member x.Transfer(_idx, _onoff) =
                if isAvailable then
                    let byteIdx = int(_idx / 8)
                    let bitIdx  = _idx % 8
                    wmx3LibIO.SetOutBit(byteIdx, bitIdx, byte(_onoff)) |> ignore
                else
                    failwith "transfer error"
            member x.Receive _receiver = 
                wmx3Lib.CreateDevice("C:\\Program Files\\SoftServo\\WMX3\\", 
                    DeviceType.DeviceTypeNormal, uint32(0xFFFFFFFF)) |> ignore
                while false = isAvailable do
                    wmx3Lib.StartCommunication(uint32(0xFFFFFFFF)) |> ignore
                    wmx3Lib.GetEngineStatus(ref enStatus) |> ignore
                    if enStatus.State = EngineState.Communicating then
                        isAvailable <- true
                printfn "is available %A and getting values %A" isAvailable _numIn
                while isAvailable do
                    wmx3LibIO.GetInBytes(0, _numIn, inputs) |> ignore
                    _receiver(inputs.Value, "wmx3")

        member x.Transfer _idx _onoff = 
            (x :> IBridgeHandler<byte>).Transfer(_idx, _onoff)
        member x.Receive _receiver = 
            (x :> IBridgeHandler<byte>).Receive _receiver
        
    type StreamData = {
        name:string;
        from:string;
        value:bool;
    }

    type MesssageHandler(
            mapInput:Dictionary<string, int>,
            valueInput:Dictionary<string, bool>,
            producer:KafkaProduce option) = 

        let JsonWrapping (data:StreamData) =
            let jsonSettings = new JsonSerializerSettings()
            jsonSettings.Converters.Add(
                new Newtonsoft.Json.Converters.StringEnumConverter()
            )
            JsonConvert.SerializeObject(data, jsonSettings)

        let getStreamData (name:string) (value:bool) =
            JsonWrapping {
                name = name;
                value = value;
                from = "ds-bridge";
            }

        let getValueFromByteArray (input:byte[]) (idx:int) =
            let bits = new BitArray(input)
            //for bit in bits do
            //    let mutable c = 'X';
            //    if bit then
            //        Console.ForegroundColor <- ConsoleColor.Green
            //        c <- 'O'
            //    else
            //        Console.ForegroundColor <- ConsoleColor.Red;
            //    Console.Write(c);
            //printfn ""
            bits[idx]

        let getValueFromInt16Array (input:int16[]) (idx:int) =
            Convert.ToBoolean(input[idx].ToString())

        member x.Receiver(input:'T[], bridgeType:string) =
            let getValue (bridgeType:string) (idx:int) = 
                match bridgeType with
                | "wmx3"    -> getValueFromByteArray input idx
                //| "paix-en" -> getValueFromInt16Array input idx // To do..
                //| "paix-ec" -> getValueFromInt16Array input idx // To do..
                | _ -> failwith "bridge type error"
            let getNowValue = getValue bridgeType 
            mapInput
            |> Seq.iter(fun checker ->
                let name = checker.Key
                let idx  = checker.Value
                if idx <> -1 then
                    let onoff = getNowValue idx
                    if valueInput[name] <> onoff then
                        printfn "local print changed input %A[%A] : %A" name idx onoff
                        let streamData = getStreamData name onoff
                        match producer with
                        | Some(producer) -> 
                            producer.TransferData(streamData)
                        | _ -> 
                            failwith "produce object error"
                        valueInput[name] <- onoff
            )
            
    type DsBridge() =
        let mutable producer:KafkaProduce option     = None
        let mutable consumer:KafkaConsume option     = None
        let mutable brdHnd:IBridgeHandler<'T> option = None
        let mapInput   = new Dictionary<string, int>() // name, index
        let mapOutput  = new Dictionary<string, int>() // name, index
        let valueInput = new Dictionary<string, bool>() // name, value
        
        let JsonWrapping (data:StreamData) =
            let jsonSettings = new JsonSerializerSettings()
            jsonSettings.Converters.Add(
                new Newtonsoft.Json.Converters.StringEnumConverter()
            )
            JsonConvert.SerializeObject(data, jsonSettings)

        let getStreamData (name:string) (value:bool) =
            JsonWrapping {
                name = name;
                value = value;
                from = "ds-bridge";
            }

        let updateMessage (content:string) =
            printfn "%A" content
            let ctt  = JObject.Parse(content)
            let from = ctt["from"].ToString()
            match from with
            | "ds-bridge" -> ()
            | "ds-hmi" ->
                let name    = ctt["name"].ToString()
                let target  = ctt["targets"]
                let btnType = ctt["button_type"].ToString()
                let onoff   = ctt["value"].ToObject()
                if mapOutput.ContainsKey(name) then
                    let idx   = mapOutput[name]
                    match brdHnd with
                    | Some(brdHnd) -> brdHnd.Transfer(idx, onoff)
                    | None -> printfn "None object"
            | _ -> ()

        let activeConsumer (consumer:KafkaConsume option) =
            match consumer with
            | Some(consumer) ->  consumer.StreamConsume(updateMessage)
            | None -> failwith "consumer generation error"

        let getDsMapIO(bridgeType:string) = 
            // To do..
            let ioList = [
                    ("INPUT.\"+\"",  3 , 1 );
                    ("INPUT.\"-\"",  2 , 0 );
                                     
                    ("USB1.\"+\"",   7 , 5 );
                    ("USB2.\"+\"",   11, 9 );
                    ("USB3.\"+\"",   15, 13);
                    ("USB4.\"+\"",   19, 17);
                                     
                    ("STP1.\"+\"",   5 , 3 );
                    ("STP2.\"+\"",   9 , 7 );
                    ("STP3.\"+\"",   13, 11);
                    ("STP4.\"+\"",   17, 15);
                                     
                    ("USB1.\"-\"",   6 , 4 );
                    ("USB2.\"-\"",   10, 8 );
                    ("USB3.\"-\"",   14, 12);
                    ("USB4.\"-\"",   18, 16);
                                     
                    ("STP1.\"-\"",   4 , 2 );
                    ("STP2.\"-\"",   8 , 6 );
                    ("STP3.\"-\"",   12, 10);
                    ("STP4.\"-\"",   16, 14);
                    
                    ("OUTPUT.\"+\"", 21, 19);
                    ("OUTPUT.\"-\"", 20, 18);
                    
                    ("CvForward",    -1, 23);
                ]

            for data in ioList do
                let name, i, o = data
                mapInput.Add(name, i)
                mapOutput.Add(name, o)
                valueInput.Add(name, false)

        let bridging (kafkaInfo:JToken) (bridgeInfo:JToken) =
            let addr    = bridgeInfo["ip"].ToString()
            let numIn   = int(bridgeInfo["numIn"].ToString())
            let numOut  = int(bridgeInfo["numOut"].ToString())
            let bridge  = bridgeInfo["type"].ToString()
            let topic   = kafkaInfo["topic"].ToString()
            let kafkaIp = kafkaInfo["ip"].ToString()

            producer <- Some(new KafkaProduce(topic, kafkaIp))
            consumer <- Some(new KafkaConsume(topic, kafkaIp))
            async {
                activeConsumer consumer
            } |> Async.StartAsTask :> Task |> ignore
            getDsMapIO(bridge)
            let msgHnd = new MesssageHandler(mapInput, valueInput, producer)
            match bridge with
            | "wmx3"    -> 
                brdHnd <- Some(new DsWMX3Handler(numIn, numOut))
                brdHnd.Value.Receive msgHnd.Receiver
            //| "paix-en" -> // To do..
            //    brdHnd <- Some(new DsPaixHandlerNMC(addr, numIn, numOut))
            //    brdHnd.Value.Receive msgHnd.Receiver
            //| "paix-ec" -> // To do..
            //    brdHnd <- Some(new DsPaixHandlerNMF(addr, numIn, numOut))
            //    brdHnd.Value.Receive msgHnd.Receiver
            | _         -> 
                failwith "communication type error"
            
        let startup (configPath:string) = 
            let json       = File.ReadAllText(configPath)
            let config     = JObject.Parse(json)
            let kafkaInfo  = config["kafka"]
            let bridgeInfo = config["bridge"]
            bridging kafkaInfo bridgeInfo

        member x.StartUp (configPath:string) = startup configPath
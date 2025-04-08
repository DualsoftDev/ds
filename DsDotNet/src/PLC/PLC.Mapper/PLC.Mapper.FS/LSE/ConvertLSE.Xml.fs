namespace PLC.Mapper.LSElectric

open Dual.Common.Core.FS
open System
open System.Xml
open System.Text.RegularExpressions
open System.Collections.Generic
open PLC.Mapper.FS
open Dual.PLC.Common.FS
open XgtProtocol

[<AutoOpen>]
module XgxXml =

    [<Literal>]
    let private globalVarPath = "Project/Configurations/Configuration/GlobalVariables/GlobalVariable"
    


    let tryGetAttribute (node: XmlNode) (attr: string) =
        if isNull node || isNull node.Attributes || isNull node.Attributes.[attr] then "" 
        else node.Attributes.[attr].Value

    let tryGetEFMTBIp (node: XmlNode) (attrPrefix: string) =
        let getIpByte idx = 
            match tryGetAttribute node (attrPrefix + idx.ToString()) with
            | "" -> 0uy
            | raw -> match Byte.TryParse(raw) with true, value -> value | _ -> 0uy
        $"{getIpByte 0}.{getIpByte 1}.{getIpByte 2}.{getIpByte 3}"

    let tryGetIp (node: XmlNode) (attr: string) =
        match UInt32.TryParse(tryGetAttribute node attr) with
        | true, ipInt ->
            let b1 = byte (ipInt &&& 0xFFu)
            let b2 = byte ((ipInt >>> 8) &&& 0xFFu)
            let b3 = byte ((ipInt >>> 16) &&& 0xFFu)
            let b4 = byte ((ipInt >>> 24) &&& 0xFFu)
            $"{b1}.{b2}.{b3}.{b4}"
        | _ -> "0.0.0.0"

    let IsXg5kXGT (xmlPath: string) =
        let doc = DualXmlDocument.loadFromFile xmlPath
        doc.GetXmlNode("//Configurations/Configuration/Parameters/Parameter/XGTBasicParam") <> null

    let IsXg5kXGI(xmlPath:string) =
        let xdoc = DualXmlDocument.loadFromFile xmlPath
        xdoc.GetXmlNode("//Configurations/Configuration/Parameters/Parameter/XGIBasicParam") <> null

    let getGlobalSymbolXmlNodes (doc: XmlDocument) =
        doc.SelectNodes($"{globalVarPath}/Symbols/Symbol")

    let getDirectVarXmlNodes (doc: XmlDocument) =
        doc.SelectNodes($"{globalVarPath}/DirectVarComment/DirectVar")

type XmlReader =

    static member ReadTags(xmlPath: string, ?usedOnly: bool) : PlcTagBase[] * string array =
        let isXGI = IsXg5kXGI xmlPath
        let usedOnly = defaultArg usedOnly true
        let xdoc: XmlDocument = DualXmlDocument.loadFromFile xmlPath

        // Offset 계산
        let getBitOffset(device:string) =
            let parseData = if isXGI then  tryParseXgiTag device else tryParseXgkTag device
            match parseData with
            | Some (_, _, offset) -> offset
            | None -> failwith $"Invalid device format: {device}"

        let ipNode = xdoc.SelectSingleNode("//Parameter[@Type='FENET PARAMETER']/Safety_Comm")
        let ip = XgxXml.tryGetIp ipNode "IPAddress"

        let ipSubNodes = xdoc.SelectNodes("//XGPD_CONFIG_INFO_FENET")
        let subIps = 
            ipSubNodes 
            |> _.ToEnumerables()
            |> Seq.map (fun node -> XgxXml.tryGetEFMTBIp node "IpAddr_")
            |> Seq.toArray

        let isOutput(smartOutputFlag:bool option, address:string) = 
            match smartOutputFlag with
            | Some v -> v
            | None ->  
                (**  //<Parameter Type="IO PARAMETER">  xgk P영역 in/out 해석 처리 필요 일단 전부 out으로
				//<Module Base="0" Slot="0" Id="42243" SubType="0" Name="입력 모듈:XGI-D28A/B (DC 24V 입력, 64점 (전류소스 싱크공용/ 전류소스 입력))" Comment="" Details="0000000000000000"></Module>
				//<Module Base="0" Slot="1" Id="42287" SubType="0" Name="출력 모듈:XGQ-TR8A/B (트랜지스터 출력, 64점 (0.1A용, 싱크출력/소스출력))" Comment="" Details="0000000000000000"></Module>
				//<BaseInfo> **)
                if address.StartsWith "%Q" || address.StartsWith "P" then true 
                else false

        let parseGlobal (node: XmlNode) : PlcTagBase option =
            let name     = XgxXml.tryGetAttribute node "Name"
            let address  = XgxXml.tryGetAttribute node "Address"
            let typStr   = XgxXml.tryGetAttribute node "Type"
            let comment  = XgxXml.tryGetAttribute node "Comment"
            let moduleInfo = XgxXml.tryGetAttribute node "ModuleInfo" //ModuleInfo="REMOTEIO:0:7:0:OUT:2:11:18" 
            let smartOutputFlag = if moduleInfo = "" then None 
                                  elif moduleInfo.Contains ":OUT:"  then Some (true)
                                  elif moduleInfo.Contains ":IN:"  then Some (false)
                                  else 
                                       None
            


            match PlcDataSizeType.TryFromString(typStr) with
            | None -> None
            | Some v -> 
                let devType =  
                        if address.IsNullOrEmpty() then PlcDataSizeType.UserDefined
                        else v

                if not(address.IsNullOrEmpty()) && PlcDataSizeType.FromString(typStr) <> devType 
                then 
                    failwith $"{typStr} <> {address} ({devType}) err"
                

                let bitOffset = 
                        if address.IsNullOrEmpty() then 0
                        else getBitOffset address

                let isOutput =  isOutput(smartOutputFlag, address)

                let tag =
                    XGTTag(
                        name,
                        address,
                        devType,
                        bitOffset,
                        isOutput,
                        comment = comment
                    )
                Some tag


        let _DirectVarNames = Dictionary<string, PlcTagBase>()

        let parseDirect (node: XmlNode) : PlcTagBase option =
            let used    = XgxXml.tryGetAttribute node "Used"
            let device  = XgxXml.tryGetAttribute node "Device"
            let comment = XgxXml.tryGetAttribute node "Comment" |> validName

            if device <> "" && comment <> "" && (not usedOnly || used = "1") then
                let uniqName =
                    if _DirectVarNames.ContainsKey comment then $"{comment}_{device}" else comment
                
                let size, offset =
                    match isXGI with
                    | true  -> LsXgiTagParser.Parse device |> fun (_, s, o) -> s, o
                    | false -> LsXgkTagParser.Parse device |> fun (_, s, o) -> s, o

                let dataType = PlcDataSizeType.FromBitSize size
                let isOutput =  isOutput(None, device)

                let tag =
                    XGTTag(
                        uniqName,
                        device,
                        dataType,
                        offset,
                        isOutput,
                        comment = comment
                    ) :> PlcTagBase

           
                // 중복 등록 방지용 이름 보관
                _DirectVarNames[uniqName] <- tag

                Some(tag)
            else
                None


        let tags =
            [|
                for node in XgxXml.getGlobalSymbolXmlNodes xdoc |> _.ToEnumerables() do
                    match parseGlobal node with
                    | Some tag -> yield tag
                    | None -> ()

                for node in XgxXml.getDirectVarXmlNodes xdoc |> _.ToEnumerables() do
                    match parseDirect node with
                    | Some tag -> yield tag
                    | None -> ()
            |]

        tags, Array.append [|ip|] subIps
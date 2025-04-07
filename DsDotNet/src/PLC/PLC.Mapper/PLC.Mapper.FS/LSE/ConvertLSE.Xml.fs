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

    static member ReadTags(xmlPath: string, ?usedOnly: bool) : XGTTag[] * string array =
        let isXGI = IsXg5kXGI xmlPath
        let usedOnly = defaultArg usedOnly true
        let xdoc: XmlDocument = DualXmlDocument.loadFromFile xmlPath
        let addrPattern = Regex("^%(?<iom>[IQM])(?<size>[XBW])", RegexOptions.Compiled)

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

        let parseGlobal (node: XmlNode) : XGTTag =
            let name     = XgxXml.tryGetAttribute node "Name"
            let address  = XgxXml.tryGetAttribute node "Address"
            let typStr   = XgxXml.tryGetAttribute node "Type"
            let comment  = XgxXml.tryGetAttribute node "Comment"
            let moduleInfo = XgxXml.tryGetAttribute node "ModuleInfo"

            let tag =
                XGTTag(
                    name,
                    address,
                    PlcTagExt.ToSystemDataType typStr,
                    getBitOffset address,
                    comment = comment
                )
            tag


        let _DirectVarNames = Dictionary<string, XGTTag>()

        let parseDirect (node: XmlNode) : XGTTag option =
            let used    = XgxXml.tryGetAttribute node "Used"
            let device  = XgxXml.tryGetAttribute node "Device"
            let comment = XgxXml.tryGetAttribute node "Comment"

            if device <> "" && comment <> "" && (not usedOnly || used = "1") then
                // 데이터 타입 추출
                let typeStr =
                    match addrPattern.Match(device) with
                    | m when m.Success ->
                        match m.Groups.["size"].Value with
                        | "X" -> "BOOL"
                        | "B" -> "BYTE"
                        | "W" -> "WORD"
                        | "D" -> "DWORD"
                        | "L" -> "LWORD"
                        | unknown -> failwithf "Unknown data type: %s" unknown
                    | _ -> ""

                // 이름 중복 방지 처리
                let uniqName =
                    if _DirectVarNames.ContainsKey comment then $"{comment}_{device}" else comment
                

                let tag =
                    XGTTag(
                        uniqName,
                        device,
                        PlcTagExt.ToSystemDataType(typeStr),
                        getBitOffset device,
                        comment = comment
                    )

           
                // 중복 등록 방지용 이름 보관
                _DirectVarNames[uniqName] <- tag

                Some(tag)
            else
                None


        let tags =
            [|
                for node in XgxXml.getGlobalSymbolXmlNodes xdoc |> _.ToEnumerables() do
                    yield parseGlobal node

                for node in XgxXml.getDirectVarXmlNodes xdoc |> _.ToEnumerables() do
                    match parseDirect node with
                    | Some tag -> yield tag
                    | None -> ()
            |]

        tags, Array.append [|ip|] subIps
namespace Old.Dual.ConvertPLC.FS.LsXGI

open FSharpPlus
open Old.Dual.Common
open System.Xml
open System.Net
open System.Collections.Generic
open AddressConvert

[<AutoOpen>]
/// XG5000 을 프로젝트 파일을 Xml로 저장한 후 DS로 열었을때 가져온는 속성
type XmlXG5000(filePath, cpu, ip, ioCard, globalVars, directVars, dicMaxDevice, rungs, systemVars, xgkCMD) = 
    member x.FilePath = filePath 
    member x.Cpu = cpu 
    member x.Ip = ip
    member x.IOCard = ioCard 
    member x.GlobalVars = globalVars 
    member x.DirectVars = directVars 
    member x.DicMaxDevice = dicMaxDevice  // 주소 타입별 Max word size를 저장 Dict<deviceHead, MaxSize:int>
    member x.Rungs = rungs 
    member x.SystemVars = systemVars //System Device영역 불러온다
    member x.XgkCMD = xgkCMD //XgkCMD 속성을 불러온다

and ModuleBase(Id, plcBase, plcSlot, comment) = 
       member x.Id = Id : int
       member x.PlcBase = plcBase 
       member x.PlcSlot = plcSlot 
       member x.Comment = comment : string

and ModuleIO(Id, plcBase, plcSlot, comment, pointIn, pointOut) = 
    inherit ModuleBase(Id, plcBase, plcSlot, comment)
    member x.PointIn = pointIn      //max Input Size  (0일 경우 Input 사용안함)
    member x.PointOut = pointOut    //max Output Size (0일 경우 Output 사용안함)

and ModuleEthernet(Id, plcBase, plcSlot, comment, stationNo, ip) = 
    inherit ModuleBase(Id, plcBase, plcSlot, comment)
    member x.StationNo = stationNo 
    member x.Ip = ip 

module FileRead =
    let getIpCpuEthernet (xdoc:XmlDocument) =
        xdoc.SelectNodes(xmlCnfPath+"/Parameters/Parameter/Safety_Comm")
        |> XmlExt.ToEnumerables
        |> Seq.map (fun v -> 
                let intIp = v.Attributes.GetNamedItem("IPAddress").InnerText
                let addressSplit = IPAddress.Parse(intIp).ToString().Split('.')
                let ip  = sprintf "%s.%s.%s.%s" addressSplit.[3] addressSplit.[2] addressSplit.[1] addressSplit.[0]
                ModuleEthernet(0, 0, 0, "Ethernet CPU 일체형", 0, ip) //CPU에 장착된 Ethernet 은 ip 외 정보는 0 으로 할당
                )

    let getIpFenet (xdoc:XmlDocument) =
        let FenetNodes1 = xdoc.SelectNodes(xmlCnfPath+"/XGPD/XGPD_CONFIG_INFO_GROUP/XGPD_CONFIG_INFO_FENET") |> XmlExt.ToEnumerables
        let FenetNodes2 = xdoc.SelectNodes(xmlCnfPath+"/XGPD/XGPD_CONFIG_INFO_GROUP/XGPD_CONFIG_INFO_FENET_XBCUXECU/XGPD_CONFIG_INFO_FENET") |> XmlExt.ToEnumerables
           
        FenetNodes1 @@ FenetNodes2
        |> Seq.map (fun v -> 
                let intIpA = v.Attributes.GetNamedItem("IpAddr_0").InnerText
                let intIpB = v.Attributes.GetNamedItem("IpAddr_1").InnerText
                let intIpC = v.Attributes.GetNamedItem("IpAddr_2").InnerText
                let intIpD = v.Attributes.GetNamedItem("IpAddr_3").InnerText
                let stationNo = v.Attributes.GetNamedItem("StationNo").InnerText |> int
                let typeId = v.Attributes.GetNamedItem("Type").InnerText |> int
                let baseE = v.Attributes.GetNamedItem("Base").InnerText |> int
                let slot = v.Attributes.GetNamedItem("Slot").InnerText |> int
                let ip = sprintf "%s.%s.%s.%s" intIpA intIpB intIpC intIpD
                ModuleEthernet(typeId, baseE, slot, "", stationNo, ip)
            )

    let getModules (xdoc:XmlDocument) =
        let parameterModule = xdoc.SelectNodes(xmlCnfPath+"/Parameters/Parameter/Module")
        let modules = parameterModule 
                    |> XmlExt.ToEnumerables
                    |> Seq.map (fun e -> e.Attributes.["Id"].InnerText |> int
                                       , e.Attributes.["Base"].InnerText  |> int
                                       , e.Attributes.["Slot"].InnerText  |> int)
        modules

    let getCpu (xdoc:XmlDocument) =
          let configuration = xdoc.SelectSingleNode(xmlCnfPath)
          let cpuType = configuration.Attributes.["Type"].InnerText  
          cpuType

    let getGlobalVarXGI (xdoc:XmlDocument) =
        let globals = xdoc.SelectSingleNode(xmlCnfPath+"/GlobalVariables/GlobalVariable")
        let numGlobals =  globals.Attributes.["Count"].Value |> System.Int32.Parse

        globals.SelectNodes("//Symbols/Symbol")
        |> XmlExt.ToEnumerables
        |> Seq.map (fun e -> e.Attributes.["Name"].InnerText
                            , e.Attributes.["Kind"].InnerText |> int
                            , e.Attributes.["Type"].InnerText
                            , e.Attributes.["Address"].InnerText
                            , e.Attributes.["Comment"].InnerText
                            , e.Attributes.["Device"].InnerText
                            )
        |> Seq.map(fun (name, kind, plctype, address, comment, device) -> 
        {Name=name; Comment=comment; Device=device; Kind = kind; Type=plctype; State=0; Address=address; DevicePos=0;})

    let getSymbolFromAddress (address, tag, comment, cpuType) =
        if(ableAddress address) then
            match AddressConvert.tryParseTag(cpuType) address  with
            | Some devFull -> 
                        let name =  if(tag = "") 
                                    then sprintf "%s[%s]" comment address
                                    else tag
                        let devDataType = if(cpuType.IsXGI()) then devFull.DataType.TotextXGI() else devFull.DataType.TotextXGK()  
                        let device = devFull.Device.ToString()
                        let devicePos = 
                            match devFull.DataType with
                            |Bit -> devFull.BitOffset
                            |Byte -> devFull.BitOffset / 8
                            |Word -> devFull.BitOffset / 16
                            |DWord -> devFull.BitOffset / 32 
                            |LWord -> devFull.BitOffset / 64
                            | _ ->  failwithf "[%s] 해당 타입은 아직 지원하지 않습니다."  (devDataType)
                        Some{Name=name; Comment=comment; Device=device; Kind = 0; Type=devDataType; State=0; Address=address; DevicePos=devicePos;}
            | None -> None
        else None

    let getGlobalVarXGKnB (xdoc:XmlDocument, dicMaxDevice:IDictionary<string, int>, cpuType) =
        let globals = xdoc.SelectSingleNode(xmlCnfPath+"/GlobalVariables/VariableComment")
        let numGlobals =  globals.SelectSingleNode("Symbols").Attributes.["Count"].Value |> int

        globals.SelectNodes("//Symbols/Symbol")
        |> XmlExt.ToEnumerables
        |> Seq.map (fun e -> e.Attributes.["Name"].InnerText
                            , e.Attributes.["Device"].InnerText
                            , e.Attributes.["DevicePos"].InnerText |> int
                            , e.Attributes.["Type"].InnerText
                            , e.Attributes.["Comment"].InnerText
                            )
        |> Seq.map(fun (name, device, devicePos, devType, comment) ->  
        let address = getAddressXGK(device, devicePos, devType, dicMaxDevice)
        //ls xml에서 가져온것은 주소체계는 무조건 심볼로 변환 가능해야 한다.
        let symbol = getSymbolFromAddress(address, name, comment, cpuType) |> Option.get
        if(symbol.Device <> device || (devType <> "BIT/WORD" && symbol.Type <> devType) || symbol.DevicePos <> devicePos) 
        then logWarn "Failed to parse tag : %s pos : %d" device devicePos else ()
        symbol)
    
        
        

    let getDirectVarXGI  (xdoc:XmlDocument, cpuType) =
        let usingDirectVar(xdoc:XmlDocument) = 
            xdoc.SelectSingleNode(xmlCnfPath+"/GlobalVariables/GlobalVariable/DirectVarComment") <> null

        if(usingDirectVar xdoc)
        then
            let globals = xdoc.SelectSingleNode(xmlCnfPath+"/GlobalVariables/GlobalVariable/DirectVarComment")
            let numGlobals =  globals.Attributes.["Count"].Value |> System.Int32.Parse
            
            globals.SelectNodes("//DirectVar")
            |> XmlExt.ToEnumerables
            |> Seq.map (fun e -> e.Attributes.["Device"].InnerText
                                , e.Attributes.["Comment"].InnerText
                                )
            |> Seq.map(fun (address, comment) ->  getSymbolFromAddress(address, "", comment, cpuType))
            |> Seq.filter(fun v ->  v.IsSome)
            |> Seq.map(fun v -> v.Value)
        else 
            Seq.empty


    /// XmlXG5000 타입으로 XML 정보를 불러온다.
    let getXml(fileName:string) =
        let newFile = fileName 
       // let newFile = if(fileName = "") then testSampleXGI else fileName
        let xdoc = newFile |> DsXml.load

        let modules = getModules xdoc
        let cpuId = getCpu xdoc |> int
        let cpu = readConfigCPU() |> Seq.filter (fun f-> f.nPLCID = cpuId) |> head

        let findCnf (id:int) = 
            readConfigIO() 
            |> Seq.tryFind(fun f-> f.HwID = id)
            |> Option.map (fun f-> f.NRefreshIn, f.NRefreshOut, f.Comments)

        let ioCards =
            modules
            |> Seq.map (fun (Id, pBase, pSlot) ->
                                match findCnf(Id) with
                                | Some (nRefreshIn, nRefreshOut, comments) -> ModuleIO(Id, pBase, pSlot, comments, nRefreshIn, nRefreshOut)
                                | _ -> ModuleIO(Id, pBase, pSlot, "카드정보가 없습니다", 0, 0)
                        )

        let ethernetIPs =  getIpFenet xdoc @@ getIpCpuEthernet xdoc
        let dicMaxDevice =  
            readConfigDevice() 
            |> Seq.filter (fun f-> f.nPLCID = cpuId) 
            |> Seq.map (fun m -> m.strDevice, m.nSize)
            |> dict

        let xgkCMD = readXGKCMDCodeDB()
        let cpuType = CpuType.FromID(cpu.nPLCID)
        let globalVars = 
            match cpuType with
            | Xgi | XgbIEC   -> getGlobalVarXGI(xdoc)
            | Xgk | XgbMk -> getGlobalVarXGKnB(xdoc, dicMaxDevice, cpuType) 
            | _ ->  failwithf "[%s] 해당 CPU 기종은 아직 지원하지 않습니다."  cpu.strPLCType // 나머지 기종 테스트 필요
        
        //XGI만 directVars 있는듯..
        let directVars = getDirectVarXGI(xdoc, cpuType)
        let systemVars =  readConfigFlag(dicMaxDevice, cpuType)

        let totalVar = 
            globalVars @@ directVars @@ systemVars
            |> Seq.map(fun v -> if(cpuType.IsXGI()) then v.Name, v else  v.Address, v)
            |> dict

        let rungs = getRungs(xdoc, cpuType, dicMaxDevice, totalVar)

        XmlXG5000(newFile, cpuType, ethernetIPs, ioCards, globalVars, directVars, dicMaxDevice, rungs, systemVars, xgkCMD)


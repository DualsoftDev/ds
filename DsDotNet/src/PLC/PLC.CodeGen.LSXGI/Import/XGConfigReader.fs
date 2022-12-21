namespace Dual.ConvertPLC.FS.LsXGI

open FSharpPlus
open Engine.Common.FS
open System.Xml
open System

[<AutoOpen>]
module XGConfigReader  =

    //let [<Literal>] testSampleXGIDemoKit =    __SOURCE_DIRECTORY__ + @"\..\..\LS\Xg5000Sample\DEMOKIT.xml"
    //let [<Literal>] testSampleXGI =    __SOURCE_DIRECTORY__ + @"\..\..\LS\Xg5000Sample\XGI Socket.xml"
    //let [<Literal>] testSampleXGK =    __SOURCE_DIRECTORY__ + @"\..\..\LS\Xg5000Sample\XGK Socket.xml"
    //let [<Literal>] testSampleXGKDevice =    __SOURCE_DIRECTORY__ + @"\..\..\LS\Xg5000Sample\XGK Device.xml"
    //let [<Literal>] testSampleXGB =    __SOURCE_DIRECTORY__ + @"\..\..\LS\Xg5000Sample\XGB Socket.xml"


    //내부 코드로 이동 ConfigXML\FLAG_INFO_0.fs  FLAG_COMMENT.fs ...
    //let [<Literal>] pathConfigCPU =     __SOURCE_DIRECTORY__ + @"\..\..\LS\Xg5000Config\PLC_TYPE_LIST.xml"
    //let [<Literal>] pathConfigIO =      __SOURCE_DIRECTORY__ + @"\..\..\LS\Xg5000Config\tblIOModule.xml"
    //let [<Literal>] pathConfigDevice =  __SOURCE_DIRECTORY__ + @"\..\..\LS\Xg5000Config\DEVICE_INFO.xml"
    //let [<Literal>] pathConfigFlag =    __SOURCE_DIRECTORY__ + @"\..\..\LS\Xg5000Config\FLAG_INFO_0.xml"
    //let [<Literal>] pathConfigFlagComment =    __SOURCE_DIRECTORY__ + @"\..\..\LS\Xg5000Config\FLAG_COMMENT.xml"

    let [<Literal>] xmlCnfPath =  "Project/Configurations/Configuration"

    
    /// PLC ConfigCPU  설정 정보를 읽을때 하나의 CPU들 대한 정보
    type ConfigCPU = {
        strPLCType : string
        nPLCID:  int
    }
    /// PLC ModuleIO BASES 설정 정보를 읽을때 하나의 모듈에 대한 정보
    /// NRefreshIn Out Count가 0일 경우 입출력 디지털 모듈이 아님
    type ModuleIO = {
        HwID : int     
        NRefreshIn:   int // in 카드 bit 수
        NRefreshOut:  int // out카드 bit 수
        Comments:  string 
    }

    /// CPU기종별 Device Max size를 XG5000 설정 파일로 부터 가져온다
    type DeviceMax = {
        nPLCID : int     
        strDevice :   string 
        nSize:  int //max word size
    } 

    /// system Device flag 를 XG5000 설정 파일로 부터 가져온다
    type DeviceFlag = {
        strFlagName :   string 
        strType :   string 
        strDevice :   string 
        strComment :   string 
        nDevicePos :  int //max word size
        nCommentIndex : int
    } 

    
    /// Xml Symbol tag 가 가지는 속성
    type SymbolInfo = {
        Name:string
        Comment:string 
        /// "M"
        Device:string
        /// "%MX1"
        Address:string
        DevicePos:int //XGK 일경우 DevicePos 정보 필요
        Kind:int
        /// "BOOL"
        Type:string
        State:int
        AddressIEC : string //XGK 일경우 IEC 주소로 변환해서 가지고 있음
    } with
        /// Symbol 관련 XML tag attributes 생성
        member private x.GetXmlArgs() =
            seq {
                yield sprintf "Name=\"%s\""    x.Name
                yield sprintf "Kind=\"%d\""    x.Kind
                yield sprintf "Type=\"%s\""    x.Type
                yield sprintf "Comment=\"%s\"" x.Comment
                yield sprintf "Device=\"%s\""  x.Device
                yield sprintf "Address=\"%s\"" x.Address
                yield sprintf "State=\"%d\""   x.State
            } |> String.concat " "
        /// Symbol 관련 XML tag 생성
        member x.ToText() =
                sprintf "<Symbol %s/>" (x.GetXmlArgs())
                //Address="" Trigger="" InitValue="" Comment="" Device="" DevicePos="-1" TotalSize="0" OrderIndex="0" HMI="0" EIP="0" SturctureArrayOffset="0" ModuleInfo="" ArrayPointer="0"><MemberAddresses></MemberAddresses>
        member x.GenerateXml() =
            seq {
                yield sprintf "\t<Symbol %s>" (x.GetXmlArgs())
                // 사용되지 않지만, 필요한 XML children element 생성
                yield! ["Addresses"; "Retains"; "InitValues"; "Comments"]
                    |> Seq.map (fun k -> sprintf "\t\t<Member%s/>" k)
                yield "\t</Symbol>"
            } |> String.concat "\r\n"

    ///CPU ID에 해당하는 이름을 XG5000 설정 파일로 부터 가져온다
    ///원본 경로 C:\XG5000\l.kor\Symbol.mdb 에  PLC_TYPE_LIST table을 Xml으로 Export해서 사용
    let readConfigCPU () =
        let xdoc =  ConfigXml.getPlcTypeListText().Value |> DsXml.loadXml

        let PLC_TYPE_LISTs = xdoc.SelectNodes("dataroot/PLC_TYPE_LIST")
        PLC_TYPE_LISTs
        |> XmlExt.ToEnumerables
        |> Seq.map (fun e  -> {
                               strPLCType = e.SelectSingleNode("strPLCType").InnerText
                               nPLCID = e.SelectSingleNode("nPLCID").InnerText  |> int
                           })

    ///IO Module ID에 해당하는 이름을 XG5000 설정 파일로 부터 가져온다
    ///원본 경로 C:\XG5000\l.kor\Hardware.mdb 에  tblIOModule table을 Xml으로 Export해서 사용
    let readConfigIO () =
        let xdoc = ConfigXml.getTblIoModuleText().Value |> DsXml.loadXml

        let IOModules = xdoc.SelectNodes("dataroot/tblIOModule")
        IOModules
        |> XmlExt.ToEnumerables
        |> Seq.map (fun e  -> {
                        HwID =  Convert.ToInt32(e.SelectSingleNode("HwID").InnerText, 16)
                        Comments =  e.SelectSingleNode("Comments").InnerText
                        NRefreshIn = match e.SelectSingleNode("nRefreshIn") with 
                                        | null -> 0
                                        | v ->  (v.InnerText |> int) * 8 //기본 in 8접점 기준
                        NRefreshOut = match e.SelectSingleNode("nRefreshOut") with 
                                        | null -> 0
                                        | v ->  (v.InnerText |> int) * 8 //기본 out 8접점 기준
                           }) 
                                       

    ///CPU기종별 Device Max size를 XG5000 설정 파일로 부터 가져온다
    ///원본 경로 C:\XG5000\l.kor\Symbol.mdb 에  DEVICE_INFO table을 Xml으로 Export해서 사용
    let readConfigDevice () =
        let xdoc = ConfigXml.getDeviceInfoText().Value |> DsXml.loadXml

        xdoc.SelectNodes("dataroot/DEVICE_INFO")
        |> XmlExt.ToEnumerables
        |> Seq.map (fun e  -> {
                        nPLCID =  Convert.ToInt32(e.SelectSingleNode("nPLCID").InnerText)
                        nSize =  Convert.ToInt32(e.SelectSingleNode("nSize").InnerText)
                        strDevice =  e.SelectSingleNode("strDevice").InnerText
                            }) 

    ///System Flag Comment를 XG5000 설정 파일로 부터 가져온다
    ///원본 경로 C:\XG5000\l.kor\Symbol.mdb 에  FLAG_COMMENT table을 Xml으로 Export해서 사용
    let dicFlagComment ()  = 
        let xdoc = ConfigXml.getFlagCommentText().Value |> DsXml.loadXml
        
        xdoc.SelectNodes("dataroot/FLAG_COMMENT")
        |> XmlExt.ToEnumerables
        |> Seq.map (fun e  -> Convert.ToInt32(e.SelectSingleNode("nCommentIndex").InnerText), e.SelectSingleNode("strComment").InnerText)
        |> dict 

    ///System Flag Device를 XG5000 설정 파일로 부터 가져온다
    ///원본 경로 C:\XG5000\l.kor\Symbol.mdb 에  FLAG_INFO_0 table을 Xml으로 Export해서 사용
    let readConfigFlag () =
        let xdoc = ConfigXml.getFlagInfoText().Value |> DsXml.loadXml
        
        let dicComment = dicFlagComment()   

        xdoc.SelectNodes("dataroot/FLAG_INFO_0")
        |> XmlExt.ToEnumerables
        |> Seq.map (fun e  -> {
                        strFlagName =  e.SelectSingleNode("strFlagName").InnerText
                        nDevicePos =  Convert.ToInt32(e.SelectSingleNode("nDevicePos").InnerText)
                        nCommentIndex =  Convert.ToInt32(e.SelectSingleNode("nCommentIndex").InnerText)
                        strDevice =  e.SelectSingleNode("strDevice").InnerText
                        strType =  e.SelectSingleNode("strType").InnerText
                        strComment =  dicComment.[Convert.ToInt32(e.SelectSingleNode("nCommentIndex").InnerText)]
                            }) 

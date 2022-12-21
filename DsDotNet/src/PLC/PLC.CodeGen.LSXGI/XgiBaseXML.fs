namespace Dsu.PLCConverter.FS
open System
open System.Text
open System.Xml
open System.Xml.Linq
open System.IO

//https://nbevans.wordpress.com/2015/04/15/super-skinny-xml-document-generation-with-f/
module XgiBaseXML =
    let FXDeclaration version encoding standalone = XDeclaration(version, encoding, standalone)
    let FXLocalName localName namespaceName = XName.Get(localName, namespaceName)
    let FXName expandedName = XName.Get(expandedName)
    let FXDocument xdecl content = XDocument(xdecl, content |> Seq.map (fun v -> v :> obj) |> Seq.toArray)
    let FXComment (value:string) = XComment(value) :> obj
    let FXElementNS localName namespaceName content = XElement(FXLocalName localName namespaceName, content |> Seq.map (fun v -> v :> obj) |> Seq.toArray) :> obj
    let FXElement expandedName content = XElement(FXName expandedName, content |> Seq.map (fun v -> v :> obj) |> Seq.toArray) :> obj
    let FXAttributeNS localName namespaceName value = XAttribute(FXLocalName localName namespaceName, value) :> obj
    let FXAttribute expandedName value = XAttribute(FXName expandedName, value) :> obj
    let (=>) x y = FXAttribute x y
    ///Xgi로 변환하기위한 옵션 객체
    type XgiOpt() =
        ///MELSEC L영역 갯수
        static let mutable lAreaCount = 0  //word 단위
        ///MELSEC B영역 시작 기본주소
        static let mutable bAreaStart = 0, "M"
        ///MELSEC M영역 시작 기본주소
        static let mutable mAreaStart = 0, "M"
        ///MELSEC L영역 시작 기본주소
        static let mutable lAreaStart = 0, "M"
        ///MELSEC F영역 시작 기본주소
        static let mutable fAreaStart = 0, "M"
        ///MELSEC V영역 시작 기본주소
        static let mutable vAreaStart = 0, "M"
        ///MELSEC S영역 시작 기본주소
        static let mutable sAreaStart = 0, "M"
        ///MELSEC SB영역 시작 기본주소
        static let mutable sbAreaStart = 0, "M"
        ///MELSEC FX영역 시작 기본주소
        static let mutable fxAreaStart = 0, "M"
        ///MELSEC FY영역 시작 기본주소
        static let mutable fyAreaStart = 0, "M"
        ///MELSEC SM영역 시작 기본주소
        static let mutable smAreaStart = 0, "M"
        ///MELSEC W영역 시작 기본주소
        static let mutable wAreaStart = 0, "W"
        ///MELSEC D영역 시작 기본주소
        static let mutable dAreaStart = 0, "M"
        ///MELSEC SW영역 시작 기본주소
        static let mutable swAreaStart = 0, "M"
        ///MELSEC FD영역 시작 기본주소
        static let mutable fdAreaStart = 0, "M"
        ///MELSEC SD영역 시작 기본주소
        static let mutable sdAreaStart = 0, "M"
        ///MELSEC Z영역 시작 기본주소
        static let mutable zAreaStart = 0, "M"
        ///MELSEC R영역 시작 기본주소
        static let mutable rAreaStart = 0, "M"
        ///MELSEC ZR영역 시작 기본주소
        static let mutable zrAreaStart = 0, "M"
        ///MELSEC 스마트증설 시작 주소 영역 시작 기본주소
        static let mutable mSmartAreaStart = 0, "M"
        ///변환 CPU 타입 주소 영역 시작 기본주소 
        ///0: "XGI-CPUE" 1:"XGI-CPUH" 2:"XGI-CPUS" 3:"XGI-CPUS/P" 4:"XGI-CPUU" 5:"XGI-CPUU/D" 6:"XGI-CPUUN"
        static let mutable nSelectCPU = 5
        static let mutable sPathCommandMapping = ""
        static let mutable sPathFBList = ""
        static let mutable timerLowSpeed = 100
        static let mutable timerHighSpeed = 10
        ///IO 변환규칙
        ///EX) "X0100", "%IX1.[2].[0]" 
        static let mutable dicMapping:seq<string*string> = Seq.empty
            //seq
            //    {
                   
            //        //yield "X0000", "%IX0.[2..].[0..63]"
            //        //yield "X0100", "%IX1.[2..].[0..63]"
            //        //yield "X0200", "%IX2.[2..].[0..63]"
            //        //yield "X1000", "%IX10.[10..].[0..63]"
            //        //yield "Y0000", "%QX0.[2..].[0..63]"
            //        //yield "Y0100", "%QX1.[2..].[0..63]"
            //        //yield "Y0200", "%QX2.[2..].[0..63]"
            //        //yield "Y1000", "%QX10.[10..].[0..63]"
            //        }

        static member LAreaCount   with get() = lAreaCount and set(value) = lAreaCount <- value

        static member WAreaStart   with get() = wAreaStart  and set(value) = wAreaStart  <- value
        static member FAreaStart   with get() = fAreaStart  and set(value) = fAreaStart  <- value
        static member MAreaStart   with get() = mAreaStart  and set(value) = mAreaStart  <- value
        static member LAreaStart   with get() = lAreaStart  and set(value) = lAreaStart  <- value
        static member VAreaStart   with get() = vAreaStart  and set(value) = vAreaStart  <- value
        static member SAreaStart   with get() = sAreaStart  and set(value) = sAreaStart  <- value
        static member SBAreaStart  with get() = sbAreaStart and set(value) = sbAreaStart <- value
        static member BAreaStart   with get() = bAreaStart  and set(value) = bAreaStart  <- value
        static member FXAreaStart  with get() = fxAreaStart and set(value) = fxAreaStart <- value
        static member FYAreaStart  with get() = fyAreaStart and set(value) = fyAreaStart <- value
        static member SMAreaStart  with get() = smAreaStart and set(value) = smAreaStart <- value
        static member DAreaStart   with get() = dAreaStart  and set(value) = dAreaStart  <- value
        static member SWAreaStart  with get() = swAreaStart and set(value) = swAreaStart <- value
        static member FDAreaStart  with get() = fdAreaStart and set(value) = fdAreaStart <- value
        static member SDAreaStart  with get() = sdAreaStart and set(value) = sdAreaStart <- value
        static member ZAreaStart  with get() = zAreaStart and set(value) = zAreaStart <- value
        static member RAreaStart  with get() = rAreaStart and set(value) = rAreaStart <- value
        static member ZRAreaStart  with get() = zrAreaStart and set(value) = zrAreaStart <- value
        static member MSmartAreaStart  with get() = mSmartAreaStart and set(value) = mSmartAreaStart <- value
        static member SelectCPU  with get() = nSelectCPU and set(value) = nSelectCPU <- value
        static member PathCommandMapping  with get() = sPathCommandMapping and set(value) = sPathCommandMapping <- value
        static member PathFBList  with get() = sPathFBList and set(value) = sPathFBList <- value
        static member TimerLowSpeed  with get() = timerLowSpeed and set(value) = timerLowSpeed <- value
        static member TimerHighSpeed  with get() = timerHighSpeed and set(value) = timerHighSpeed <- value
        
        
        static member CPUs      = [|"XGI-CPUE"; "XGI-CPUH"; "XGI-CPUS"; "XGI-CPUS/P"; "XGI-CPUU"; "XGI-CPUU/D"; "XGI-CPUUN" |]
        static member CPUsID    = [|"106"     ; "102"     ; "104"     ; "110"       ; "100"     ; "107"       ; "111"       |]
        static member I_CPUMax  = [|     2048 ;    8192   ;    2048   ;     2048    ; 8192      ;  8192       ;    8192     |]
        static member Q_CPUMax  = [|     2048 ;    8192   ;    2048   ;     2048    ; 8192      ;  8192       ;    8192     |]
        static member M_CPUMax  = [|     16384;    131072 ;    32768  ;     32768   ; 131072    ;  131072     ;    262144   |]
        static member L_CPUMax  = [|     11264;    11264  ;    11264  ;     11264   ; 11264     ;  11264      ;    11264    |]
        static member N_CPUMax  = [|     25088;    25088  ;    25088  ;     25088   ; 25088     ;  25088      ;    25088    |]
        static member K_CPUMax  = [|     2100 ;    8400   ;    2100   ;     2100    ; 8400      ;  8400       ;    8400     |]
        static member U_CPUMax  = [|     1024 ;    4096   ;    2048   ;     2048    ; 4096      ;  4096       ;    4096     |]
        static member R_CPUMax  = [|     16384;    32768  ;    32768  ;     32768   ; 32768     ;  32768      ;    32768    |]
        static member A_CPUMax  = [|     32768;    262144 ;    65536  ;     65536   ; 262144    ;  262144     ;    524288   |]
        static member W_CPUMax  = [|     16384;    65536  ;    32768  ;     32768   ; 65536     ;  524288     ;    524288   |]
        static member F_CPUMax  = [|     2048 ;    2048   ;    2048   ;     2048    ; 2048      ;  2048       ;    4096     |]

        static member MaxIQLevelM = 32  // CPUUN 기준 module //모비스 32점 카드 기준
        static member MaxIQLevelS = 16   // CPUUN 기준 slot
        static member MaxIQLevelB = 128  // CPUUN 기준 base
        static member MaxMBits = 262144  // CPUUN 기준
        static member MaxWBits = 262144  // CPUUN 기준
        static member MaxRBits = 32768   // CPUUN 기준

        static member Mapping with get() = dicMapping and  set(value) = dicMapping <- value

        static member dicSystem =
            seq
                {
                    yield "SM400", "FX153"
                    yield "SM401", "FX154"
                    yield "SM402", "FX155"
                    yield "SM403", "FX156"
                    yield "SM60500", "FX154"

                    //%FD0	PLC 모드와 운전 상태
                    //%FD1	시스템의 에러(중고장)
                    //%FD2	시스템의 경고(경고장)
                    //%FD23	OS 버전 번호
                    //%FD24	OS 날짜
                    //%FD43	현재 로컬 KEY 상태
                    //%FD477	베이스 고장 마스크 정보
                    //%FD478	베이스 스킵 정보
                    //%FD498	전원 가동시간(단위: 초)
                    //%FD69	RTC의 현재 시간(ms단위)
                    //%FD89	OS 패치 버전
                    //%FW1026	외부 기기의 중고장 정보
                    //%FW1027	외부 기기의 경고장 정보
                    //%FW13	순시 정전 발생 횟수
                    //%FW136	RTC의 현재 날짜
                    //%FW137	RTC의 현재 요일
                    //%FW14	FALS 번호
                    //%FW158	현재 사용중인 블록 번호
                    //%FW1784	사용자가 읽어간 SOE event 개수
                    //%FW1785	사용자가 읽어간 SOE event 로테이트 정보
                    //%FW1786	SOE event 발생 개수
                    //%FW1787	SOE event 로테이트 정보
                    //%FW44	CPU 타입 정보
                    //%FW45	CPU 버전 번호
                    //%FW50	최대 스캔 시간
                    //%FW51	최소 스캔 시간
                    //%FW52	현재 스캔 시간
                    //%FW58	고속링크 파라미터 이상 - 전체 정보
                    //%FW59	P2P 파라미터 이상 - 전체 정보
                    //%FW90	모듈 타입 불일치 슬롯 넘버
                    //%FW91	모듈 착탈 슬롯 넘버
                    //%FW92	퓨즈 단선 슬롯 넘버
                    //%FX0	RUN
                    //%FX1	STOP
                    //%FX10	런중 수정 완료
                    //%FX11	런중 수정 비정상 완료
                    //%FX12	키에 의한 운전모드 변경
                    //%FX13	로컬 PADT에 의한 운전모드 변경
                    //%FX14	리모트 PADT에 의한 운전모드 변경
                    //%FX144	20ms 주기 CLOCK
                    //%FX145	100ms 주기 CLOCK
                    //%FX146	200ms 주기 CLOCK
                    //%FX147	1s 주기 CLOCK
                    //%FX148	2s 주기 CLOCK
                    //%FX149	10s 주기 CLOCK
                    //%FX15	리모트 통신 모듈에 의한 운전 모드 변경
                    //%FX150	20s 주기 CLOCK
                    //%FX151	60s 주기 CLOCK
                    //%FX15232	퓨즈 에러시 운전 속행 설정
                    //%FX15233	IO 모듈 에러시 운전 속행 설정
                    //%FX15234	특수 모듈 에러시 운전 속행 설정
                    //%FX15235	통신 모듈 에러시 운전 속행 설정
                    //%FX153	상시 ON
                    //%FX154	상시 OFF
                    //%FX155	1 스캔 ON
                    //%FX156	1 스캔 OFF
                    //%FX157	매 스캔 반전
                    //%FX16	강제 입력
                    //%FX16384	RTC에 데이터 쓰기
                    //%FX16385	스캔 값 초기화
                    //%FX16386	외부 기기 중고장 검출 요청
                    //%FX16387	외부 기기 경고장 검출 요청
                    //%FX16392	정주기 태스크 스캔 값 초기화
                    //%FX16400	초기화 태스크 수행 완료
                    //%FX17	강제 출력
                    //%FX176	연산 에러 플래그
                    //%FX179	전출력 OFF
                    //%FX18	입출력 SKIP 실행 중
                    //%FX181	연산 에러 플래그(래치)
                    //%FX19	고장 마스크 실행 중
                    //%FX2	ERROR
                    //%FX20	모니터 실행 중
                    //%FX21	STOP 펑션에 의한 STOP
                    //%FX22	ESTOP 펑션에 의한 STOP
                    //%FX24	초기화 태스크 수행 중
                    //%FX28	프로그램 코드1
                    //%FX28864	배열 인덱스 범위 초과 에러 플래그
                    //%FX28896	배열 인덱스 범위 초과 래치 에러 플래그
                    //%FX29	프로그램 코드2
                    //%FX3	DEBUG
                    //%FX33	모듈 타입 불일치 에러
                    //%FX34	모듈 착탈 에러
                    //%FX35	퓨즈 단선 에러
                    //%FX38	외부기기의 중고장 검출 에러
                    //%FX4	로컬 컨트롤
                    //%FX40	기본 파라미터 이상
                    //%FX41	IO 구성 파라미터 이상
                    //%FX42	특수 모듈 파라미터 이상
                    //%FX43	통신 모듈 파라미터 이상
                    //%FX44	프로그램 에러
                    //%FX45	프로그램 코드 에러
                    //%FX46	CPU 비정상 종료 또는 고장
                    //%FX47	베이스 전원 에러
                    //%FX48	스캔 워치독 에러
                    //%FX49	베이스 정보 이상
                    //%FX6	리모트 모드 ON
                    //%FX64	RTC 데이터 이상
                    //%FX67	비정상 운전 정지
                    //%FX68	태스크 충돌
                    //%FX69	배터리 이상
                    //%FX70	외부 기기의 경고장 검출
                    //%FX72	고속링크 파라미터 이상 - 대표 플래그
                    //%FX8	런중 수정 중(프로그램 다운로드 중)
                    //%FX84	P2P 파라미터 이상 - 대표 플래그
                    //%FX9	런중 수정 중(내부 처리 중)
                    //%FX92	고정주기 오류
                    //%FX95	EtherNet/IP TAG 정보 이상

                    //%FB106	현재시각	ARRAY[0..7] OF BYTE
                    //%FB2068	설정하고자 하는 시간	ARRAY[0..7] OF BYTE
                    //%FX944	P2P 파라미터 이상 - 상세 플래그	ARRAY[0..7] OF BOOL
                    //%FX15872	P2P enable/disable 현재상태	ARRAY[0..7] OF BOOL
                    //%FX16512	P2P enable/disable 요청	ARRAY[0..7] OF BOOL
                    //%FX16528	P2P enable/disable 설정	ARRAY[0..7] OF BOOL
                    //%FW190	정주기 태스크 스캔 시간	ARRAY[0..31, 0..2] OF WORD
                    //%FX928	고속링크 파라미터 이상 - 상세 플래그	ARRAY[0..11] OF BOOL
                    //%FX15840	고속링크 enable/disable 현재상태	ARRAY[0..11] OF BOOL
                    //%FX16480	고속링크 enable/disable 요청	ARRAY[0..11] OF BOOL
                    //%FX16496	고속링크 enable/disable 설정	ARRAY[0..11] OF BOOL
                    //%FW96	모듈 타입 불일치 에러	ARRAY[0..1] OF WORD
                    //%FW104	모듈 착탈 에러	ARRAY[0..1] OF WORD
                    //%FW112	퓨즈 단선 에러	ARRAY[0..1] OF WORD
                    //%FW150	베이스 정보	ARRAY[0..1] OF WORD
                    //%FW958	슬롯 고장마스크 정보	ARRAY[0..1] OF WORD
                    //%FW966	슬롯 스킵 정보	ARRAY[0..1] OF WORD
                }
            |> dict

    type XDocument with
        /// Saves the XML document to a MemoryStream using UTF-8 encoding, indentation and character checking.
        member doc.Save() =
            let ms = new MemoryStream()
            use xtw = XmlWriter.Create(ms, XmlWriterSettings(Encoding = Encoding.UTF8, Indent = true, CheckCharacters = true))
            doc.Save(xtw)
            ms.Position <- 0L
            ms

    /// XDocument --> XmlDocument
    // https://stackoverflow.com/questions/1508572/converting-xdocument-to-xmldocument-and-vice-versa
    let toXmlDocument (xdoc:XDocument) =
        let xmlDocument = XmlDocument()
        use xmlReader = xdoc.CreateReader()
        xmlDocument.Load(xmlReader);
        xmlDocument

    /// XmlDocument --> XDocument
    // https://stackoverflow.com/questions/1508572/converting-xdocument-to-xmldocument-and-vice-versa
    let toXDocument_Dirty (xmlDocument:XmlDocument) =
        use nodeReader = new XmlNodeReader(xmlDocument)
        nodeReader.MoveToContent() |> ignore
        System.Xml.Linq.XDocument.Load(nodeReader)

    /// XmlDocument --> XDocument
    // https://stackoverflow.com/questions/1508572/converting-xdocument-to-xmldocument-and-vice-versa
    let toXDocument (xmlDocument:XmlDocument) = System.Xml.Linq.XDocument.Parse(xmlDocument.OuterXml)

    let printXDoc (xdoc:XDocument) =
        let sb = new StringBuilder();
        let xmlWriterSettings =
            new XmlWriterSettings(
                Indent = false,
                OmitXmlDeclaration = true,
                IndentChars = "  ",
                NewLineChars = "\r\n",
                NewLineHandling = NewLineHandling.Replace,
                NewLineOnAttributes = true
                )
        let writer = XmlWriter.Create(sb, xmlWriterSettings)
        let xmlDoc = toXmlDocument xdoc
        xmlDoc.Save(writer)
        // Mixed-content XML 문법이라 별도 개행처리
        let Xml =  sb.ToString().Split([|'<'|], StringSplitOptions.RemoveEmptyEntries)
                    |> String.concat "\r\n<"
        sprintf "<%s" Xml

    let createXgiXml() =
        let doc = FXDocument (FXDeclaration "1.0" "UTF-8" "yes") [
            //FXComment "This document was automatically generated by a configuration script."
            FXElement "Project" [
                "Attribute"    => "3145730"
                "Version"      => "513"
                "Comment"      => ""
                "WksNodeCount" => "16"
                "GUID"         => "a48476b3-9c89-4b57-99a4-5a8f6bcc5ecd"
                // https://stackoverflow.com/questions/12130228/can-a-xml-element-contain-text-and-child-elements-at-the-same-time
                "NewProject" |> box        // 여기에 project 이름이 들어가 있는데, Mixed-content XML 문법.
                FXElement "NetworkConfiguration" [
                    FXElement "NetworkList" [
                        FXElement "Network" [
                            "Type"        =>"NETWORK ITEM:UNKNOWN"
                            "Name"        =>"기본 네트워크"
                            "NetworkType" =>""
                        ]
                    ]
                ]
                FXElement "SystemVariable" ["HMIGUID" => ""]
                FXElement "Configurations" [
                    FXElement "Configuration" [
                        "Version" => "259"
                        "Attribute" => "272119649"
                        "Comment" => ""
                        "Kind" => "1"
                        "Type" => sprintf "%s" XgiOpt.CPUsID.[XgiOpt.SelectCPU]//         //106 CPU-E
                        "FindGlobal" => "1"
                        "FindSymbol" => "1"
                        "Encrytption" => ""
                        "Encrytption_UDF" => ""
                        "UploadProhibit" => "75"
                        "GUID" => "e8fc95d4-bb51-4e40-a34e-4b5f820abda3"
                        "SD_UploadProhibit" => "74"
                        "SD_Password" => ""
                        "SD_MacLimit" => ""
                        "NewPLC" |> box        // Mixed-content XML 문법.
                        FXElement "Parameters" [
                            FXElement "Parameter" [
                                "Type" => "BASIC PARAMETER"
                                FXElement "XGIBasicParam" [
                                    "bp_ver"                      => "100"
                                    "head"                        => "1145128264"
                                    "size"                        => "400"
                                    "pmt_reserved1_0"             => "0"
                                    "pmt_reserved1_1"             => "0"
                                    "pmt_reserved1_2"             => "0"
                                    "pmt_reserved1_3"             => "0"
                                    "pmt_reserved1_4"             => "0"
                                    "OsVersion"                   => "1"
                                    "CPUType"                     => "43009"
                                    "STATIC_PERIOD_MODE"          => "0"
                                    "STATIC_PERIOD_TIME"          => "10"
                                    "RESTART_METHOD"              => "43521"
                                    "REMOTE_ACCESS_PERMIT"        => "43522"
                                    "RESET_DISABLE"               => "0"
                                    "O_RESET_DISABLE"             => "0"
                                    "DCLEAR_DISABLE"              => "0"
                                    "O_DCLEAR_DISABLE"            => "0"
                                    "BASIC_MOD_RESERVED_0"        => "0"
                                    "BASIC_MOD_RESERVED_1"        => "0"
                                    "BASIC_MOD_RESERVED_2"        => "0"
                                    "BASIC_MOD_RESERVED_3"        => "0"
                                    "BASIC_MOD_RESERVED_4"        => "0"
                                    "BASIC_MOD_RESERVED_5"        => "0"
                                    "BASIC_MOD_RESERVED_6"        => "0"
                                    "BASIC_MOD_RESERVED_7"        => "0"
                                    "BASIC_MOD_RESERVED_8"        => "0"
                                    "BASIC_MOD_RESERVED_9"        => "0"
                                    "SCAN_WD_TIME_0"              => "500"
                                    "SCAN_WD_TIME_1"              => "500"
                                    "SCAN_WD_TIME_2"              => "500"
                                    "SCAN_WD_TIME_3"              => "500"
                                    "STANDARD_INPUT_FILTER_TIME"  => "3"
                                    "BASIC_TIME_RESERVED_0"       => "0"
                                    "BASIC_TIME_RESERVED_1"       => "0"
                                    "BASIC_TIME_RESERVED_2"       => "0"
                                    "BASIC_TIME_RESERVED_3"       => "0"
                                    "BASIC_TIME_RESERVED_4"       => "0"
                                    "BASIC_TIME_RESERVED_5"       => "0"
                                    "BASIC_TIME_RESERVED_6"       => "0"
                                    "BASIC_TIME_RESERVED_7"       => "0"
                                    "BASIC_TIME_RESERVED_8"       => "0"
                                    "BASIC_TIME_RESERVED_9"       => "0"
                                    "BASIC_TIME_RESERVED_10"      => "0"
                                    "DEBUG_OUTPUT_PARAMETER"      => "0"
                                    "ERROR_MODE_OUTPUT_PARAMETER" => "0"
                                    "RUN2STOP_OUTPUT_PARAMETER"   => "0"
                                    "STOP2RUN_OUTPUT_PARAMETER"   => "0"
                                    "ERROR_MODE_LATCH_PARAMETER"  => "0"
                                    "OUTPUT_PARAMETER_RESERVED_0" => "3728"
                                    "OUTPUT_PARAMETER_RESERVED_1" => "121"
                                    "OUTPUT_PARAMETER_RESERVED_2" => "0"
                                    "OUTPUT_PARAMETER_RESERVED_3" => "0"
                                    "OUTPUT_PARAMETER_RESERVED_4" => "0"
                                    "SOE_RETAIN_HISTORY"          => "43544"
                                    "SOE_PARAMETER_RESERVED_0"    => "0"
                                    "SOE_PARAMETER_RESERVED_1"    => "0"
                                    "SOE_PARAMETER_RESERVED_2"    => "0"
                                    "SOE_PARAMETER_RESERVED_3"    => "0"
                                    "SOE_PARAMETER_RESERVED_4"    => "0"
                                    "RETAIN_SET_MODE"             => "43552" //사용 43552 사용안함 0
                                    "M_AREA_INIT"                 => "0"
                                    "M_AREA_SIZE_0"               => "32"
                                    "M_AREA_SIZE_1"               => "32"
                                    "M_AREA_SIZE_2"               => "32"
                                    "M_AREA_SIZE_3"               => "32"
                                    "M_AREA_LATCH1_START"         => (XgiOpt.LAreaStart |> fun (offset,head) -> offset).ToString()
                                    "M_AREA_LATCH1_END"           => ((XgiOpt.LAreaStart |> fun (offset,head) -> offset) + XgiOpt.LAreaCount).ToString()
                                    "BASIC_RETAIN_RESERVED_0"     => "0"
                                    "BASIC_RETAIN_RESERVED_1"     => "0"
                                    "BASIC_RETAIN_RESERVED_2"     => "0"
                                    "BASIC_RETAIN_RESERVED_3"     => "0"
                                    "BASIC_RETAIN_RESERVED_4"     => "0"
                                    "BASIC_RETAIN_RESERVED_5"     => "0"
                                    "BASIC_RETAIN_RESERVED_6"     => "0"
                                    "BASIC_RETAIN_RESERVED_7"     => "0"
                                    "BASIC_RETAIN_RESERVED_8"     => "0"
                                    "BASIC_RETAIN_RESERVED_9"     => "0"
                                    "BASIC_RETAIN_RESERVED_10"    => "0"
                                    "BASIC_RETAIN_RESERVED_11"    => "0"
                                    "BASIC_RETAIN_RESERVED_12"    => "0"
                                    "BASIC_RETAIN_RESERVED_13"    => "0"
                                    "BASIC_RETAIN_RESERVED_14"    => "0"
                                    "BASIC_RETAIN_RESERVED_15"    => "0"
                                    "BASIC_RETAIN_RESERVED_16"    => "0"
                                    "BASIC_RETAIN_RESERVED_17"    => "0"
                                    "BASIC_RETAIN_RESERVED_18"    => "0"
                                    "BASIC_RETAIN_RESERVED_19"    => "0"
                                    "BASIC_RETAIN_RESERVED_20"    => "0"
                                    "BASIC_RETAIN_RESERVED_21"    => "0"
                                    "BASIC_RETAIN_RESERVED_22"    => "0"
                                    "BASIC_RETAIN_RESERVED_23"    => "0"
                                    "BASIC_RETAIN_RESERVED_24"    => "0"
                                    "BASIC_RETAIN_RESERVED_25"    => "0"
                                    "BASIC_RETAIN_RESERVED_26"    => "0"
                                    "BASIC_RETAIN_RESERVED_27"    => "0"
                                    "BASIC_RETAIN_RESERVED_28"    => "0"
                                    "BASIC_RETAIN_RESERVED_29"    => "0"
                                    "BASIC_RETAIN_RESERVED_30"    => "0"
                                    "BASIC_RETAIN_RESERVED_31"    => "0"
                                    "BASIC_RETAIN_RESERVED_32"    => "0"
                                    "BASIC_RETAIN_RESERVED_33"    => "0"
                                    "BASIC_RETAIN_RESERVED_34"    => "0"
                                    "BASIC_RETAIN_RESERVED_35"    => "0"
                                    "BASIC_RETAIN_RESERVED_36"    => "0"
                                    "BASIC_RETAIN_RESERVED_37"    => "0"
                                    "BASIC_RETAIN_RESERVED_38"    => "0"
                                    "BASIC_RETAIN_RESERVED_39"    => "0"
                                    "BASIC_RETAIN_RESERVED_40"    => "0"
                                    "BASIC_RETAIN_RESERVED_41"    => "0"
                                    "BASIC_RETAIN_RESERVED_42"    => "0"
                                    "BASIC_RETAIN_RESERVED_43"    => "0"
                                    "BASIC_RETAIN_RESERVED_44"    => "0"
                                    "BASIC_RETAIN_RESERVED_45"    => "0"
                                    "BASIC_RETAIN_RESERVED_46"    => "0"
                                    "BASIC_RETAIN_RESERVED_47"    => "0"
                                    "BASIC_RETAIN_RESERVED_48"    => "0"
                                    "BASIC_RETAIN_RESERVED_49"    => "0"
                                    "TIMER_RANGE_RESERVED_0"      => "0"
                                    "TIMER_RANGE_RESERVED_1"      => "0"
                                    "TIMER_RANGE_RESERVED_2"      => "0"
                                    "TIMER_RANGE_RESERVED_3"      => "0"
                                    "TIMER_RANGE_RESERVED_4"      => "0"
                                    "TIMER_RANGE_RESERVED_5"      => "0"
                                    "TIMER_RANGE_RESERVED_6"      => "0"
                                    "TIMER_RANGE_RESERVED_7"      => "0"
                                    "TIMER_RANGE_RESERVED_8"      => "0"
                                    "TIMER_RANGE_RESERVED_9"      => "0"
                                    "TIMER_RANGE_RESERVED_10"     => "0"
                                    "TIMER_RANGE_RESERVED_11"     => "0"
                                    "TIMER_RANGE_RESERVED_12"     => "0"
                                    "TIMER_RANGE_RESERVED_13"     => "0"
                                    "TIMER_RANGE_RESERVED_14"     => "0"
                                    "TIMER_RANGE_RESERVED_15"     => "0"
                                    "CHECK_FUSE_ERROR"            => "43572"
                                    "FUSE_ERROR_MODE"             => "43573"
                                    "CHECK_IO_ERROR"              => "43574"
                                    "IO_ERROR_MODE"               => "43575"
                                    "CHECK_EXT_BASE_ERROR"        => "0"
                                    "EXT_BASE_ERROR_MODE"         => "0"
                                    "ERROR_REACTION_RESERVED_0"   => "0"
                                    "ERROR_REACTION_RESERVED_1"   => "0"
                                    "ERROR_REACTION_RESERVED_2"   => "0"
                                    "ERROR_REACTION_RESERVED_3"   => "0"
                                    "ERROR_REACTION_RESERVED_4"   => "0"
                                    "ERROR_REACTION_RESERVED_5"   => "0"
                                    "ERROR_REACTION_RESERVED_6"   => "0"
                                    "ERROR_REACTION_RESERVED_7"   => "0"
                                    "ERROR_REACTION_RESERVED_8"   => "0"
                                    "ERROR_REACTION_RESERVED_9"   => "0"
                                    "ERROR_REACTION_RESERVED_10"  => "0"
                                    "ERROR_REACTION_RESERVED_11"  => "0"
                                    "ERROR_REACTION_RESERVED_12"  => "0"
                                    "ERROR_REACTION_RESERVED_13"  => "0"
                                    "ERROR_REACTION_RESERVED_14"  => "0"
                                    "ERROR_REACTION_RESERVED_15"  => "0"
                                    "ERROR_REACTION_RESERVED_16"  => "0"
                                    "ERROR_REACTION_RESERVED_17"  => "0"
                                    "ERROR_REACTION_RESERVED_18"  => "0"
                                    "ERROR_REACTION_RESERVED_19"  => "0"
                                    "ERROR_REACTION_RESERVED_20"  => "0"
                                    "ERROR_REACTION_RESERVED_21"  => "0"
                                    "CHECK_SP_ERROR"              => "43576"
                                    "SP_ERROR_MODE"               => "43577"
                                    "CHECK_CP_ERROR"              => "43578"
                                    "CP_ERROR_MODE"               => "43579"
                                    "MODBUS_STATION"              => "63"
                                    "MODBUS_BAUDRATE"             => "4434"
                                    "MODBUS_DATABIT"              => "43592"
                                    "MODBUS_PARITY"               => "43602"
                                    "MODBUS_STOPBIT"              => "43617"
                                    "MODBUS_TRX_MODE"             => "43632"
                                    "DI_START_DEVICE_TYPE"        => "73"
                                    "DO_START_DEVICE_TYPE"        => "81"
                                    "AI_START_DEVICE_TYPE"        => "77"
                                    "AO_START_DEVICE_TYPE"        => "77"
                                    "DI_DEVICE_OFFSET"            => "0"
                                    "DO_DEVICE_OFFSET"            => "0"
                                    "AI_DEVICE_OFFSET"            => "2000"
                                    "AO_DEVICE_OFFSET"            => "4000"
                                    "pmt_reserved2_0"             => "0"
                                    "pmt_reserved2_1"             => "0"
                                    "pmt_reserved2_2"             => "0"
                                    "pmt_reserved2_3"             => "0"
                                    "pmt_reserved2_4"             => "0"
                                    "pmt_reserved2_5"             => "0"
                                    "check_sum"                   => "0"
                                    "tail"                        => "1414483782"
                                ]
                            ]
                            FXElement "Parameter" [
                                "Type" => "IO PARAMETER"
                                FXElement "Base" ["Base" => "0"; "SlotCount" => "12"]
                                FXElement "Base" ["Base" => "1"; "SlotCount" => "12"]
                                FXElement "Base" ["Base" => "2"; "SlotCount" => "12"]
                                FXElement "Base" ["Base" => "3"; "SlotCount" => "12"]
                                FXElement "Base" ["Base" => "4"; "SlotCount" => "12"]
                                FXElement "Base" ["Base" => "5"; "SlotCount" => "12"]
                                FXElement "Base" ["Base" => "6"; "SlotCount" => "12"]
                                FXElement "Base" ["Base" => "7"; "SlotCount" => "12"]
                            ]
                        ]

                        //FXElement "GlobalVariables" [
                        //    FXElement "GlobalVariable" [
                        //        FXElement "Symbols" []
                        //        FXElement "TempVar" ["Count" => "0"]
                        //        FXElement "HMIFlags" []
                        //        FXElement "DirectVarComment" ["Count" => "0"]
                        //    ]
                        //]
                        FXElement "Tasks" [
                            FXElement "Task" [
                                "Version" => "257"
                                "Type" => "0"
                                "Attribute" => "2"
                                "Kind" => "0"
                                "Priority" => "0"
                                "TaskIndex" => "0"
                                "Device" => ""
                                "DeviceType" => "0"
                                "WordValue" => "0"
                                "WordCondition" => "0"
                                "BitCondition" => "0"
                                "스캔 프로그램"  |> box        // XML 문법과는 맞지 않음.
                            ]
                        ]
                        FXElement "POU" [
                            FXElement "InsertPoint" ["Content" => "Programs"]
                            //FXElement "Programs" [
                            //    FXElement "Program" [
                            //    "Task" => "스캔 프로그램"
                            //    "LocalVariable" => "1"
                            //    "Kind" => "0"
                            //    "InstanceName" => ""
                            //    "Comment" => ""
                            //    "FindProgram" => "1"
                            //    "Encrytption" => ""
                            //    "Auto Convert"  |> box        // XML 문법과는 맞지 않음.
                            //    FXElement "Body" [
                            //        FXElement "LDRoutine" [
                            //            FXElement "InsertPoint" ["Content" => "Rungs"]
                            //            //FXElement "OnlineUploadData" ["Compressed" => "1"; "dt:dt" => "bin.base64"; "xmlns:dt" => "urn:schemas-microsoft-com:datatypes"]
                            //            ]
                            //        ]
                            //    //FXElement "InsertPoint" ["Content" => "LocalVar"]
                            //    //FXElement "LocalVar" []
                            //    FXElement "RungTable" []
                            //    ]
                            //]
                        ]

                        FXElement "GlobalVariables" [
                            FXElement "InsertPoint" ["Content" => "GlobalVariable"]
                            ]
                    ]
                ]
                FXElement "UserDefinedMetadata" [
                    FXElement "Address1" ["경기도 군포시 당정동"]
                    FXElement "Postcode" ["123-456"]
                    FXElement "Patchcode" ["1"]
                    FXElement "Reviewdate" [DateTime.UtcNow.AddYears(1).ToString("s")]
                ]
           ]
        ]
        printXDoc doc

    //module TestMe =
    //    let test() = createXgiXml()
    //       // let doc = createXgiXml()
    //       // doc.ToString()
    //        //printXDoc doc

    //    let test2() =
    //        FXElement "Project" [
    //            "Attribute"    => "3145730"
    //            "XXX" |> box
    //        ]
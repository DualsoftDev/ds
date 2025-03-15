namespace Dsu.PLCConverter.FS

open ActivePattern
open Microsoft.FSharp.Collections
open System
open Dsu.PLCConverter.FS.XgiSpecs.Config
open XgiBaseXML
open Dsu.PLCConverter.FS.XgiSpecs

open Dsu.PLCConverter.FS.ActivePattern

/// Mitsubishi PLC의 다양한 장치 유형을 정의하는 타입
[<AutoOpen>]
type MelsecDevice =
    | X    | Y    | M    | L     | B    | F     | Z    | V
    | D    | W    | R    | ZR    | T    | ST    | C    
    | SM   | SD   | SW   | SB    | DX   | DY
    with
        /// 장치 타입을 텍스트로 변환하는 멤버
        member x.ToText = ActivePattern.toString x

        /// 문자열을 통해 MelsecDevice 인스턴스를 생성하는 함수
        static member Create s =
            let x = ActivePattern.fromString<MelsecDevice> s
            match x with
            | Some(dev) -> 
                match dev with
                | DX -> X  // DX는 X로 매핑
                | DY -> Y  // DY는 Y로 매핑
                | _ -> dev
            | None -> failwith (sprintf "Invalid address [%s]" s)

/// XGI 태그 타입 정의
[<AutoOpen>]
type XGI =
    | I    // 입력
    | Q    // 출력
    | M    // 메모리
    | R    // 파일 레지스터 (64k * 16 블록)
    | W    // 파일 레지스터 전체 (1024k)
    | A    // 자동 변수 타입 (주소 번지 없음)
    | S    // 시스템 변수
    with
        /// Works2, 3 장치 패턴을 정의
        static member Works2Devices = "(^[X|Y|M|L|B|D|W|R|T|C|F|Z|V])([0-9A-F]+)(\.|)([0-9A-F]+|)"
        /// 확장 장치 패턴 정의
        static member Works2Extends = "([Z|S|D])([R|M|D|W|B|X|Y|T])([0-9A-F]+)(\.|)([0-9A-F]+|)"

        /// 주소의 헤더 타입을 반환
        static member HeadType(address) =
            match address with
            | StartsWith "%I" () -> "I"
            | StartsWith "%Q" () -> "Q"
            | StartsWith "%M" () -> "M"
            | StartsWith "%W" () -> "W"
            | StartsWith "%R" () -> "R"
            | StartsWith "%A" () -> "A"
            | StartsWith "%F" () -> "S"
            | EndsWith "_NOT_XGI" () -> "S"
            | _ -> failwith (sprintf "Invalid address [%s]" address)

        /// Works2 주소에서 MelsecDevice와 인덱스를 추출하는 함수
        static member Parsing(address: string) =
            let parsingEnd (head: MelsecDevice, d1: string, d2: string) =
                match d2 with
                | "" -> match head with
                        | MelsecDevice.X | MelsecDevice.Y | MelsecDevice.B | MelsecDevice.W | MelsecDevice.SW | MelsecDevice.SB
                            -> head, (Convert.ToInt32(d1, 16)), -1
                        | _ -> head, (Convert.ToInt32(d1, 10)), -1
                | _ -> match head with
                        | MelsecDevice.W | MelsecDevice.SW
                            -> head, (Convert.ToInt32(d1, 16)), (Convert.ToInt32(d2, 16))
                        | _ -> head, (Convert.ToInt32(d1, 10)), (Convert.ToInt32(d2, 16))
            match address with
            | ActivePattern.RegexPattern (sprintf @"%s" XGI.Works2Devices) [head; d1; dot; d2] -> parsingEnd(MelsecDevice.Create head, d1, d2)
            | ActivePattern.RegexPattern (sprintf @"%s" XGI.Works2Extends) [head1; head2; d1; dot; d2] -> parsingEnd(MelsecDevice.Create (head1 + head2), d1, d2)
            | _ -> failwith (sprintf "Invalid address [%s]" address)

        /// Melsec 주소가 유효한지 확인하는 함수
        static member IsMelsecAddress(address: string) =
            match address with //@간접주소 지원불가
            | ActivePattern.RegexPattern (sprintf @"%s" XGI.Works2Devices) _ -> true
            | ActivePattern.RegexPattern (sprintf @"%s" XGI.Works2Extends) _ -> true
            | ActivePattern.RegexPattern (NibbleText) _ -> true
            | _ -> false

        static member IsXGIAddress(address: string) =
            match address with
            | ActivePattern.RegexPattern (@"%(I|Q|M|W|R|A|F)(\S)(\d)") _ -> true
            | _ -> false

        /// flat 인덱스를 IEC index의 n2, n1, n0로 변환하여 반환
        static member private GetIECIndex(flatIndex) =
            let n = flatIndex
            let n0 = n % XgiOption.MaxIQLevelM
            let n1 = (n / XgiOption.MaxIQLevelM) % XgiOption.MaxIQLevelS
            let n2 = (n / (XgiOption.MaxIQLevelM * XgiOption.MaxIQLevelS))
            n2, n1, n0

        /// flat 인덱스를 Smart M의 x, y index로 변환하여 반환
        static member private GetSmartMIndex(flatIndex, input: bool) =
            let nWord = flatIndex / 16
            let nShiftWord = XgiOption.MaxIQLevelM / 16
            let nShiftBit = flatIndex % XgiOption.MaxIQLevelM
            let resultX = (nShiftWord * 2 * (nWord / nShiftWord))
            let resultY = resultX + nShiftWord
            (if input then resultX else resultY) * 16 + nShiftBit

        /// flat 인덱스에 IO Mapping Offset을 적용하여 XGI index로 변환
        static member private GetXGIIndex(head, flatIndex) =
            let IOSpec = if head = "IX" then XgiOption.MappingIO_X else XgiOption.MappingIO_Y
            let xOffset, xXGI =
                let addlst = 
                    IOSpec
                    |> Seq.sortBy (fun (index, _) -> -index)
                    |> Seq.filter (fun (index, _) -> index <= flatIndex)
                if Seq.length addlst > 0 then Seq.head addlst
                else 0, (0, 0, 0)

            let xBase, xSlot, xModule = xXGI
            let n2, n1, n0 = XGI.GetIECIndex(flatIndex - xOffset)
            n2 + xBase, n1 + xSlot, n0

        /// Works2 주소를 XGI IQ Address로 변환
        static member private GetIQAddress(head, d1, varType: VarType) =
            let n2, n1, n0 = XGI.GetXGIIndex(head, d1)
            (sprintf "%%%s%d.%d.%d" head n2 n1 n0, varType, d1)

        /// Works2 주소를 XGI Smart M Address로 변환
        static member private GetMSmartAddress(head, d1, offset, inputX: bool, varType: VarType) =
            let m = XGI.GetSmartMIndex(d1, inputX)
            (sprintf "%%%sW%d.%d" head (m / 16 + offset) (m % 16), varType, m)
        
        /// Works2 주소를 XGI M Address로 변환
        static member private GetMAddress(head:string, d1, d2, varType) =
            if head.isNullOrEmpty()
            then 
                failwithf $"GetMAddress Error {d1} {d2} {varType}"
            let newHead = head + if varType = VarType.BOOL then "X" else "W"
            match d2 with
            | -1 -> (sprintf "%%%s%d" newHead d1, varType, if varType = VarType.BOOL then  d1 else d1*16)
            | _ -> (sprintf "%%%s%d.%d" newHead d1 d2, varType, d1*16+d2)

        /// Works2 주소를 XGI Auto Address로 변환
        static member private GetAutoAddress(head, d1, varType) =
            ("%A", varType, -1)

        /// 시스템 주소를 가져오는 함수
        static member private GetSystemAddress(melsecHead: MelsecDevice, d1, varType) =
            let systemKey = melsecHead.ToText + d1.ToString()
            let dicSys = XgiOption.MappingSys 

            let systemSymbol =
                if dicSys.ContainsKey(systemKey) then sprintf "%%%s" dicSys.[systemKey]
                else systemKey + "_NOT_XGI"
            (systemSymbol, varType, d1)

        /// Works2 주소를 XGI 심볼 형식으로 변환하는 함수
        static member MakeXgiSymbolName(head: MelsecDevice, d1: int, d2: int) =
            let symbol =
                match d2 with
                | -1 -> match head with
                        | MelsecDevice.X | MelsecDevice.Y | MelsecDevice.B | MelsecDevice.SW | MelsecDevice.SB
                            -> sprintf "%s%X" (head.ToText) d1
                        | MelsecDevice.W 
                            -> sprintf "%s_%X" (head.ToText) d1
                        | MelsecDevice.T -> sprintf "%s%d" (head.ToText) d1
                        | MelsecDevice.M | MelsecDevice.L | MelsecDevice.R | MelsecDevice.F -> sprintf "%s_%d" (head.ToText) d1
                        | _ -> sprintf "%s%d" (head.ToText) d1
                | _ -> match head with
                        | MelsecDevice.W | MelsecDevice.SW | MelsecDevice.SD -> sprintf "%s%X_%X" (head.ToText) d1 d2
                        | MelsecDevice.D | MelsecDevice.ZR -> sprintf "%s%d_%X" (head.ToText) d1 d2
                        | MelsecDevice.R -> sprintf "%s_%d_%X" (head.ToText) d1 d2
                        | _ -> failwith (sprintf "Invalid address [%s%d.%d]" head.ToText d1 d2)
            symbol
        static member ZDeviceType = VarType.DINT
        static member ZDeviceCheckType = CheckType.DINT

        /// Works2 주소를 XGI 주소 형식으로 변환하는 함수
        static member MakeXgiAddressWithOffset(melsecHead: MelsecDevice, d1: int, d2: int) =
            let ret =
                match d2 with
                | -1 -> // 단순 주소 형식 (예: X12, Y232, D122)
                    match melsecHead with
                    | MelsecDevice.DX 
                    | MelsecDevice.X -> 
                            match XgiOption.Config.MSmartAreaType with
                            | "M" -> XGI.GetMSmartAddress("M", d1, XgiOption.Config.MSmartAreaStart, true, VarType.BOOL)
                            | _ -> XGI.GetIQAddress("IX", d1, VarType.BOOL)
                    | MelsecDevice.DY
                    | MelsecDevice.Y -> 
                            match XgiOption.Config.MSmartAreaType with
                            | "M" -> XGI.GetMSmartAddress("M", d1, XgiOption.Config.MSmartAreaStart, false, VarType.BOOL)
                            | _ -> XGI.GetIQAddress("QX", d1, VarType.BOOL)

                    | MelsecDevice.M -> XGI.GetMAddress(XgiOption.Config.MAreaType, d1 + XgiOption.Config.MAreaStart * 16, d2, VarType.BOOL)
                    | MelsecDevice.L -> XGI.GetMAddress(XgiOption.Config.LAreaType, d1 + XgiOption.Config.LAreaStart * 16, d2, VarType.BOOL)
                    | MelsecDevice.B -> XGI.GetMAddress(XgiOption.Config.BAreaType, d1 + XgiOption.Config.BAreaStart * 16, d2, VarType.BOOL)
                    | MelsecDevice.F -> XGI.GetMAddress(XgiOption.Config.FAreaType, d1 + XgiOption.Config.FAreaStart * 16, d2, VarType.BOOL)
                    | MelsecDevice.SB -> XGI.GetMAddress(XgiOption.Config.SBAreaType, d1 + XgiOption.Config.SBAreaStart * 16, d2, VarType.BOOL)

                    | MelsecDevice.V -> XGI.GetMAddress(XgiOption.Config.VAreaType, d1 + XgiOption.Config.VAreaStart, d2, VarType.WORD)
                    | MelsecDevice.D -> XGI.GetMAddress(XgiOption.Config.DAreaType, d1 + XgiOption.Config.DAreaStart, d2, VarType.WORD)
                    | MelsecDevice.W -> XGI.GetMAddress(XgiOption.Config.WAreaType, d1 + XgiOption.Config.WAreaStart, d2, VarType.WORD)
                    | MelsecDevice.R -> XGI.GetMAddress(XgiOption.Config.RAreaType, d1 + XgiOption.Config.RAreaStart, d2, VarType.WORD)
                    | MelsecDevice.ZR -> XGI.GetMAddress(XgiOption.Config.ZRAreaType, d1 + XgiOption.Config.ZRAreaStart, d2, VarType.WORD)
                    | MelsecDevice.SW -> XGI.GetMAddress(XgiOption.Config.SWAreaType, d1 + XgiOption.Config.SWAreaStart, d2, VarType.WORD)
                    
                    | MelsecDevice.Z -> XGI.GetAutoAddress("Z", d1, ZDeviceType)
                    | MelsecDevice.T -> XGI.GetAutoAddress("T", d1, VarType.TON_UINT)
                    | MelsecDevice.ST -> XGI.GetAutoAddress("T", d1, VarType.TMR)
                    | MelsecDevice.C -> XGI.GetAutoAddress("C", d1, VarType.CTU_UINT)
                    | MelsecDevice.SM -> XGI.GetSystemAddress(melsecHead, d1, VarType.BOOL)
                    | MelsecDevice.SD -> XGI.GetSystemAddress(melsecHead, d1, VarType.WORD)
                | _ -> // 확장 주소 형식 (예: D333.F)
                    match melsecHead with
                    | MelsecDevice.D -> XGI.GetMAddress(XgiOption.Config.DAreaType, d1 + XgiOption.Config.DAreaStart, d2, VarType.WORD)
                    | MelsecDevice.W -> XGI.GetMAddress(XgiOption.Config.WAreaType, d1 + XgiOption.Config.WAreaStart, d2, VarType.WORD)
                    | MelsecDevice.R -> XGI.GetMAddress(XgiOption.Config.RAreaType, d1 + XgiOption.Config.RAreaStart, d2, VarType.WORD)
                    | MelsecDevice.ZR ->XGI.GetMAddress(XgiOption.Config.ZRAreaType, d1 + XgiOption.Config.ZRAreaStart, d2, VarType.WORD)
                    | MelsecDevice.SW ->XGI.GetMAddress(XgiOption.Config.SWAreaType, d1 + XgiOption.Config.SWAreaStart, d2, VarType.WORD)
                    | MelsecDevice.SD ->XGI.GetSystemAddress(melsecHead, d1, VarType.WORD)
                    | _ -> failwith (sprintf "Invalid address [%s%d.%d]" melsecHead.ToText d1 d2)

            ret

        static member MakeXgiAddress(melsecHead: MelsecDevice, d1: int, d2: int) =
            MakeXgiAddressWithOffset (melsecHead, d1, d2)
            |> fun (addr, varType, offset) -> addr, varType
                
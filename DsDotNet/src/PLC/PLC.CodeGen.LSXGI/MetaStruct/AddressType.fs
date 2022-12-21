namespace Dsu.PLCConverter.FS

open Dual.Common
open ActivePattern
open Microsoft.FSharp.Collections
open System
open Dsu.PLCConverter.FS.XgiSpecs.Config
open XgiBaseXML

[<AutoOpen>]
type MelsecDevice =
    | X    | Y    | M    | L     | B    | F     | Z    | V
    | D    | W    | R    | ZR    | T    | ST    | C    
    | SM   | SD   | SW   | SB    | DX   | DY
    with
        member x.ToText = Dual.Common.DiscriminatedUnions.toString x
        static member Create s =
            let x = Dual.Common.DiscriminatedUnions.fromString<MelsecDevice> s
            match x with
            | Some(dev) -> 
                        match dev with
                        |DX -> X
                        |DY -> Y
                        |_ -> dev
            | None -> failwithlog (sprintf "Invalid address [%s]" s)
///XGI Tag
[<AutoOpen>]
type XGI =
    | I
    | Q
    | M
    | R     //파일 레지스터 64k * 16 블록
    | W     //파일 레지스터 전체 1024k (64k*16)
    | A     //timer count 등 주소번지 가 없는 자동 변수 타입
    | S
    with
    static member Works2Devices = "(^[X|Y|M|L|B|D|W|R|T|C|F|Z|V])([0-9A-F]+)(\.|)([0-9A-F]+|)"
    static member Works2Extends = "([Z|S|D])([R|M|D|W|B|X|Y])([0-9A-F]+)(\.|)([0-9A-F]+|)"
    static member HeadType (address) =
        match address with
        | StartsWith "%I" () -> "I"
        | StartsWith "%Q" () -> "Q"
        | StartsWith "%M" () -> "M"
        | StartsWith "%W" () -> "W"
        | StartsWith "%R" () -> "R"
        | StartsWith "%A"   () -> "A"
        | StartsWith "%F" () -> "S"
        | EndsWith "_NOT_XGI" () -> "S"
        | _ -> failwithlog (sprintf "Invalid address [%s]" address)
    ///Works2 주소를 파싱해서 MelsecDevice 와 INDEX를 가져옴
    static member Parsing(address:string)  =
        let parsingEnd (head:MelsecDevice, d1:string, d2:string) =
            match d2 with
            | "" -> match head with
                        | MelsecDevice.X | MelsecDevice.Y | MelsecDevice.B | MelsecDevice.W | MelsecDevice.SW | MelsecDevice.SB
                            -> head, (Convert.ToInt32(d1, 16)), -1
                        | _-> head, (Convert.ToInt32(d1, 10)), -1
            | _  -> match head with
                        | MelsecDevice.W | MelsecDevice.SW
                            -> head, (Convert.ToInt32(d1, 16)), (Convert.ToInt32(d2, 16))
                        | _-> head, (Convert.ToInt32(d1, 10)), (Convert.ToInt32(d2, 16))
        match address with
        | ActivePattern.RegexPattern (sprintf @"%s" XGI.Works2Devices) [head; d1; dot; d2] -> parsingEnd  (MelsecDevice.Create(head), d1, d2)
        | ActivePattern.RegexPattern (sprintf @"%s" XGI.Works2Extends) [head1;head2; d1; dot; d2] -> parsingEnd(MelsecDevice.Create(head1+ head2) , d1, d2)
        | _ -> failwithlog (sprintf "Invalid address [%s]" address)

    ///Works2 주소를 파싱해서 Address 인지 확인
    static member IsAddress(address:string) =
        match address with
        | ActivePattern.RegexPattern (sprintf @"%s" XGI.Works2Devices) [head; d1; dot; d2] -> true
        | ActivePattern.RegexPattern (sprintf @"%s" XGI.Works2Extends) [head1;head2; d1; dot; d2] -> true
        | _ -> false

    ///Works2 주소로 XGI 심볼 형태로 변환
    static member MakeXgiSymbolName(head:MelsecDevice, d1:int, d2:int)  =
        let symbol =
            match d2 with
            | -1 -> match head with
                    | MelsecDevice.X | MelsecDevice.Y | MelsecDevice.B | MelsecDevice.SW  | MelsecDevice.SB
                        -> sprintf "%s%X" (head.ToText) (int d1)
                    | MelsecDevice.W 
                            -> sprintf "%s_%X" (head.ToText) (int d1)
                    | MelsecDevice.T -> sprintf "%s%d" (head.ToText) (int d1)
                    | MelsecDevice.M | MelsecDevice.L | MelsecDevice.R | MelsecDevice.F -> sprintf "%s_%d" (head.ToText) (int d1)
                    | _-> sprintf "%s%d" (head.ToText) (int d1)

            | _ -> match head with
                    | MelsecDevice.W | MelsecDevice.SW| MelsecDevice.SB-> sprintf "%s%X_%X" (head.ToText) d1 d2  //W4F.F
                    | MelsecDevice.D | MelsecDevice.ZR -> sprintf "%s%d_%X" (head.ToText) d1 d2 //D124.F
                    | MelsecDevice.R -> sprintf "%s_%d_%X" (head.ToText) d1 d2 //R_124.F
                    | _ -> failwithlog (sprintf "Invalid address [%s%d.%d]" head.ToText d1 d2)
        symbol

    /// flat index 를 component n2, n1, n0 의 index 로 환산해서 반환
    static member private GetIECIndex(flatIndex) =
                let n = flatIndex
                let n0 = n % XgiOpt.MaxIQLevelM
                let n1 = (n / XgiOpt.MaxIQLevelM) % XgiOpt.MaxIQLevelS
                let n2 = (n / (XgiOpt.MaxIQLevelM * XgiOpt.MaxIQLevelS))
                n2, n1, n0

    /// flat index 를 IO Mapping Offset을 적용해서 반환
    static member private GetXGIIndex(head, flatIndex) =
        let getBaseSlotModule (xgiSpec) =
                match xgiSpec with
                | ActivePattern.RegexPattern @"%([IQM])([XBWD])(\d+)\.\[(\d+)\]\.\[(\d+)\]$" [iom; xw; d1; d2; d3] -> int d1, int d2, (int d3) + 1
                | _ -> failwithlog (sprintf "Invalid address spec [%s]" xgiSpec)

        let melAddList (xy) =
            XgiOpt.Mapping
            |> Seq.filter (fun (mel, xgi) -> mel.ToUpper().StartsWith xy)
            |> Seq.map (fun (mel, xgi)
                         -> (XGI.Parsing mel |> fun (xy,index,c) -> index, getBaseSlotModule xgi))

        let HeadType  = if(head = "IX") then "X" else "Y"
        let xOffset, xXGI =
            let addlst = 
                melAddList HeadType
                |> Seq.sortBy (fun (index, xgi) -> -index)
                |> Seq.filter (fun (index, xgi) ->  index <= flatIndex)
            if(addlst |> Seq.length > 0) then  addlst |> Seq.head
            else 0, (0,0,0)

        let xBase, xSlot, xModule  = xXGI
        let n2, n1, n0 =  XGI.GetIECIndex (flatIndex - xOffset + xSlot * XgiOpt.MaxIQLevelM)
        n2 + xBase, n1, n0

    ///Works2 주소로 XGI IQAddress 형태로 변환
    static member private GetIQAddress(head, d1, varType:VarType) =
        let n2, n1, n0 = XGI.GetXGIIndex (head, d1)
        (sprintf "%%%s%d.%d.%d" head n2 n1 n0, varType)

    ///Works2 주소로 XGI MAddress 형태로 변환
    static member private GetMAddress(head, d1,d2, varType) =
        let newHead = head + if(varType = VarType.BOOL) then "X" else "W"
        match d2 with
             | -1 -> (sprintf "%%%s%d" newHead d1, varType)
             | _  -> (sprintf "%%%s%d.%d" newHead d1 d2, varType)

    ///Works2 주소로 XGI AutoAddress 형태로 변환
    static member private GetAutoAddress(head, d1, varType) =
        ("%A", varType)

    static member  private GetSystemAddress(melsecHead:MelsecDevice, d1, varType) =
        let systemKey = melsecHead.ToText + d1.ToString();
        let systemSymbol = if(XgiOpt.dicSystem.ContainsKey(systemKey))   //주소를 심볼처럼 사용
                            then sprintf "%%%s" XgiOpt.dicSystem.[systemKey] else systemKey + "_NOT_XGI" //XGI 대응대는 F주소 없을 경우
        (systemSymbol, varType)

    ///Works2 주소로 XGI 주소 형태로 만듬
    static member MakeXgiAddress(melsecHead:MelsecDevice, d1:int, d2:int)  =
        let address =
            match d2 with
            | -1 -> //D333.F 같은 형태가 아닌 (X12, Y232, D122)
                match melsecHead with
                | MelsecDevice.DX 
                | MelsecDevice.X -> XGI.GetIQAddress ("IX", d1 ,VarType.BOOL)
                | MelsecDevice.DY
                | MelsecDevice.Y -> XGI.GetIQAddress ("QX", d1 ,VarType.BOOL)
                | MelsecDevice.M -> XgiOpt.MAreaStart |> fun (offset, head) -> XGI.GetMAddress (head, d1 + offset * 16, d2, VarType.BOOL)
                | MelsecDevice.L -> XgiOpt.LAreaStart |> fun (offset, head) -> XGI.GetMAddress (head, d1 + offset * 16, d2, VarType.BOOL)
                | MelsecDevice.B -> XgiOpt.BAreaStart |> fun (offset, head) -> XGI.GetMAddress (head, d1 + offset * 16, d2, VarType.BOOL)
                | MelsecDevice.F -> XgiOpt.FAreaStart |> fun (offset, head) -> XGI.GetMAddress (head, d1 + offset * 16, d2, VarType.BOOL)
                | MelsecDevice.Z -> XgiOpt.ZAreaStart |> fun (offset, head) -> XGI.GetMAddress (head, d1 + offset, d2, VarType.WORD)
                | MelsecDevice.V -> XgiOpt.VAreaStart |> fun (offset, head) -> XGI.GetMAddress (head, d1 + offset, d2, VarType.WORD)
                | MelsecDevice.D -> XgiOpt.DAreaStart |> fun (offset, head) -> XGI.GetMAddress (head, d1 + offset, d2, VarType.WORD)
                | MelsecDevice.W -> XgiOpt.WAreaStart |> fun (offset, head) -> XGI.GetMAddress (head, d1 + offset, d2, VarType.WORD)
                | MelsecDevice.R -> XgiOpt.RAreaStart |> fun (offset, head) -> XGI.GetMAddress (head, d1 + offset, d2, VarType.WORD)
                | MelsecDevice.ZR-> XgiOpt.ZRAreaStart |> fun (offset, head) -> XGI.GetMAddress (head, d1 + offset, d2, VarType.WORD)
                | MelsecDevice.T -> XGI.GetAutoAddress ("T", d1, VarType.TON_UINT)
                | MelsecDevice.ST-> XGI.GetAutoAddress ("T", d1, VarType.TON_UINT)
                | MelsecDevice.C -> XGI.GetAutoAddress ("C", d1, VarType.INT)
                | MelsecDevice.SM-> XgiOpt.SMAreaStart |> fun (offset, head) -> XGI.GetSystemAddress (melsecHead, d1, VarType.BOOL)
                | MelsecDevice.SB-> XgiOpt.SWAreaStart |> fun (offset, head) -> XGI.GetMAddress (head, d1 + offset * 16, d2, VarType.BOOL)
                | MelsecDevice.SD-> XgiOpt.SDAreaStart |> fun (offset, head) -> XGI.GetMAddress (head, d1 + offset, d2, VarType.WORD)
                | MelsecDevice.SW-> XgiOpt.SWAreaStart |> fun (offset, head) -> XGI.GetMAddress (head, d1 + offset, d2, VarType.WORD)
            | _ ->
                match melsecHead with
                | MelsecDevice.D -> XgiOpt.DAreaStart |> fun (offset, head) -> XGI.GetMAddress (head, d1 + offset, d2, VarType.WORD)
                | MelsecDevice.W -> XgiOpt.WAreaStart |> fun (offset, head) -> XGI.GetMAddress (head, d1 + offset, d2, VarType.WORD)
                | MelsecDevice.R -> XgiOpt.RAreaStart |> fun (offset, head) -> XGI.GetMAddress (head, d1 + offset, d2, VarType.WORD)
                | MelsecDevice.ZR ->XgiOpt.ZRAreaStart |> fun (offset, head) -> XGI.GetMAddress (head, d1 + offset, d2, VarType.WORD)
                | _ -> failwithlog (sprintf "Invalid address [%s%d.%d]" melsecHead.ToText d1 d2)
        address

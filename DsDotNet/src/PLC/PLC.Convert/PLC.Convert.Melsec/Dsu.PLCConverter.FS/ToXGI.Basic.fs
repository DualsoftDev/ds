namespace Dsu.PLCConverter.FS

open Dsu.PLCConverter.FS.XgiSpecs.Config.POU.Program.LDRoutine
open Dsu.PLCConverter.FS.XgiSpecs
open ActivePattern
open System.Text.RegularExpressions
open System.IO

[<AutoOpen>]
module MelsecAddressType =

    let NibbleText = @"(K)(\d+)(\D+\S+)"
    let ZIndexText = @"(\S+)(Z\d+)"
    
    let isNibbleDevice (address: string) =
        match address with
        | ActivePattern.RegexPattern NibbleText [_;_;dev] -> 
            not(dev.StartsWith("T") || dev.StartsWith("C"))

        | _ -> false

    let isArrayDevice (address: string) =
        match address with
        | ActivePattern.RegexPattern ZIndexText [dev;_index] -> 
            not(dev.StartsWith("T") || dev.StartsWith("C"))

        | _ -> false

[<AutoOpen>]
/// XGI 프로그램 변환의 기본 구조와 관련된 함수들을 정의
module XgiBasic =

    /// Contact 모드 설정 함수
    let getContactMode (opOpt: OpOpt) =
        match opOpt with
        | OpOpt.Normal -> ElementType.ContactMode
        | OpOpt.Neg -> ElementType.ClosedContactMode
        | OpOpt.Rising -> ElementType.PulseContactMode
        | OpOpt.Falling -> ElementType.NPulseContactMode
        | OpOpt.NegRising -> ElementType.ClosedPulseContactMode
        | OpOpt.NegFalling -> ElementType.ClosedNPulseContactMode
        
        | OpOpt.Inverter -> ElementType.InverterMode
        | OpOpt.Compare -> ElementType.VertFBMode
        | OpOpt.RisingEdge -> ElementType.RisingContact
        | OpOpt.FallingEdge -> ElementType.FallingContact

    /// Coil 모드 설정 함수
    let getCoilMode (cmd: string) =
        match cmd with
        | "OUT" -> ElementType.CoilMode
        | "OUTP" -> ElementType.PulseCoilMode
        | "OUTN" -> ElementType.NPulseCoilMode
        | "OUT NOT" -> ElementType.ClosedCoilMode
        | "SET" -> ElementType.SetCoilMode
        | "RST" -> ElementType.ResetCoilMode
        | _ -> failwithf "Unknown cmd [%s]" cmd

    /// CSV 파일에서 Command Mapping 정보를 로드
    let mappingCSV = XgiOption.PathCommandMapping
    let mappingTable = createDictionary mappingCSV
    let mutable pouName = ""
    let mutable pouStep = 0
    let mutable convertOk = true
    let errorHistory = ResizeArray<string>()
    let warningHistory = ResizeArray<string>()
    let insts = ResizeArray<Inst>()

    /// 주어진 프로그램 라인을 변환하는 함수
    let xgiLineConvertor line =
        let args = printArgumentOfLine line
        let cmdMelec = line.Instruction

        if cmdMelec = "" then 
            line, "RungComment", line.LineStatement
        elif mappingTable.ContainsKey(cmdMelec) then
            let il = mappingTable.[cmdMelec].Xgi.ToUpper()
            if il = "X" && cmdMelec.EndsWith("P") && cmdMelec <> "MEP" && cmdMelec <> "EGP" && cmdMelec <> "JMP" then
                line, mappingTable.[cmdMelec.[0..cmdMelec.length() - 2]].Xgi + ";P", args
            else 
                match line.Instruction with
                | "EGP" -> line, "EGP", args
                | "MEP" -> line, "MEP", args
                | "EGF" -> line, "EGF", args
                | "MEF" -> line, "MEF", args
                | "JMP" -> line, "JMP", args
                | "FEND" -> line, "SBRT", args
                | _ -> line, il, args 
        else
            assert(true) // 매핑 테이블에 없는 CMD를 발견
            line, line.Instruction, args

    /// XGI 좌표 계산 함수 (논리 좌표를 LSIS XGI 수치 좌표로 변환)
    let coord x y = x * 3 + y * 1024 + 1

    /// 다양한 요소를 XML 형식으로 표현
    let elementFull type' coordi param tag = sprintf "\t\t<Element ElementType=\"%d\" Coordinate=\"%d\" %s>%s</Element>" type' coordi param tag

    /// 좌표에서 시작하는 수직 라인 요소 생성
    let vline c = elementFull (int ElementType.VertLineMode) (c + 2) "" ""

    /// 좌표에서 시작하는 수평 라인 요소 생성
    let hline c = elementFull (int ElementType.HorzLineMode) c "" ""

    /// 양 방향 검출 라인 요소 생성
    let risingline c = elementFull (int ElementType.RisingContact) c "" ""

    /// 음 방향 검출 라인 요소 생성
    let fallingline c = elementFull (int ElementType.FallingContact) c "" ""

    /// 코일과 관련된 좌표 제한 값 설정
    let coilCellX = 31      // 산전 limit: 가로
    let minFBCellX = 9      // 산전 FB 위치: 가로 
    let fbCellX x:int = if minFBCellX <= x + 3 then x + 4 else minFBCellX

    /// 자동 타입 리스트 정의
    let AutoType = [|"T"; "C"; R_TRIG.ToText; F_TRIG.ToText; "TON_UINT"|] |> System.Collections.Generic.HashSet
    let MelSecAutoType = [|"T";"C"|] |> System.Collections.Generic.HashSet
    let MelSecSysType = [|"SM";"SD"|] |> System.Collections.Generic.HashSet

    /// 수직 라인 요소들을 생성하여 반환
    let vlineDownTo x y n =
        seq {
            if x >= 0 then
                for n in [0..n - 1] do
                    let c = coord x (y + n)
                    yield vline c
        }

    /// 수평 라인 요소들을 생성하여 반환
    let hlineRightTo x y n =
        seq {
            if x >= 0 then
                for n in [0..n - 1] do
                    let c = coord (x + n) y
                    yield hline c
        }

    /// 다중 수평 라인을 생성하는 함수
    let mutiEndLine startX endX y =
        if endX > startX then
            let lengthParam = sprintf "Param=\"%d\"" (3 * (endX - startX))
            let c = coord startX y
            elementFull (int ElementType.MultiHorzLineMode) c lengthParam ""
        else failwithf "Invalid endX startX [%d > %d]" endX startX

    /// 변수 이름에서 잘못된 형식을 수정하여 반환
    let getNewArgs (args: seq<string>) (fbName:string) = 
        args |> Seq.map (fun arg ->
            if arg.StartsWith("@") then arg + "_NOT_XGI"
            elif Regex.IsMatch(arg, NibbleText) 
                then if fbName = "" then arg  else arg+"_NOT_XGI" //fb가 아니면 공란 "" nibble 타입 지원 안함

            elif arg.StartsWith("K") then sprintf "%d" (System.Convert.ToInt32(arg.TrimStart('K'), 10))
            elif arg.StartsWith("H") then sprintf "%d" (System.Convert.ToInt32(arg.TrimStart('H'), 16))
            elif arg.StartsWith("E") then arg.TrimStart('E')
            elif arg.StartsWith("C") then arg + ".Q"
            elif arg.StartsWith("T") then arg + ".Q"
            elif arg.StartsWith("ST") then arg + ".Q"
            elif arg = "NOT" || arg = "MEP" || arg = "MEF" then ""
            else arg.Replace('"', ''')) 
           
 /// 문자열 dataType을 CheckType으로 변환하는 함수
    let parseCheckType (dataType: string) : CheckType =
        match dataType.ToUpper() with
        | "BOOL"           -> CheckType.BOOL
        | "BYTE"           -> CheckType.BYTE
        | "WORD"           -> CheckType.WORD
        | "DWORD"          -> CheckType.DWORD
        | "LWORD"          -> CheckType.LWORD
        | "SINT"           -> CheckType.SINT
        | "INT"            -> CheckType.INT
        | "DINT"           -> CheckType.DINT
        | "LINT"           -> CheckType.LINT
        | "USINT"          -> CheckType.USINT
        | "UINT"           -> CheckType.UINT
        | "UDINT"          -> CheckType.UDINT
        | "ULINT"          -> CheckType.ULINT
        | "REAL"           -> CheckType.REAL
        | "LREAL"          -> CheckType.LREAL
        | "TIME"           -> CheckType.TIME
        | "DATE"           -> CheckType.DATE
        | "TOD"            -> CheckType.TOD
        | "DT"             -> CheckType.DT
        | "STRING"         -> CheckType.STRING
        | "WSTRING"        -> CheckType.WSTRING
        | "CONSTANT"       -> CheckType.CONSTANT
        | "ARRAY"          -> CheckType.ARRAY
        | "STRUCTURE"      -> CheckType.STRUCTURE
        | "FBINSTANCE"     -> CheckType.FBINSTANCE
        | "ANYARRAY"       -> CheckType.ANYARRAY
        | "ONLYDIRECTVAR"  -> CheckType.ONLYDIRECTVAR
        | "NIBBLE"         -> CheckType.NIBBLE
        | "SAFEBOOL"       -> CheckType.SAFEBOOL
        | "ONLYCONSTANT"   -> CheckType.ONLYCONSTANT
        | "ARRAYSIZE"      -> CheckType.ARRAYSIZE
        | "POINTER"        -> CheckType.POINTER
        | _ -> failwithf "Unsupported data type: %s" dataType

    /// 더블 사이즈가 필요한 변수들의 주소를 수정
    let getDoubleSizeArgs (args: seq<string>) = 
        args |> Seq.map (fun arg ->
            if arg.StartsWith("%M") || arg.StartsWith("%R") || arg.StartsWith("%W") then
                arg.Replace("%MW", "%MD").Replace("%RW", "%RD").Replace("%WW", "%WD")
            else arg)

    /// 데이터 타입에 따라 더블 사이즈 적용
    let newDoubleArgs (dataType: CheckType) (args: seq<string>) (func:string)= 
        let temp = getNewArgs args func
        match dataType with
        | CheckType.DWORD | CheckType.STRING | CheckType.REAL | CheckType.DINT 
            -> getDoubleSizeArgs temp
        | _ -> temp

    /// 마지막 항목을 추가하여 확장된 인자 리스트를 반환
    let getAddLast (args: seq<string>) =    
        args |> Seq.append (args |> Seq.skip (args.length() - 1))

    /// 변수들을 삽입하여 수직 라인 요소로 반환
    let InArgs ((items: List<string>), x, y) = 
        let results = ResizeArray<string>()
        for n in [1..items.length()] do
            let c = coord x (y + n)
            results.Add(elementFull (int ElementType.VariableMode) c "" (items.[n - 1]))
        results

    /// 변수들을 삽입하여 수평 라인 요소로 반환
    let OutArgs ((items: List<string>), x, y) = 
        let results = ResizeArray<string>()
        for n in [1..items.length()] do
            let c = coord (x + 2) (y + n)
            results.Add(elementFull (int ElementType.VariableMode) c "" (items.[n - 1]))
        results

    /// Contact 모드에 따라 x, y 축 이동량을 결정
    let addPointX opComp =
        match opComp with
        | OpOpt.Compare -> 3
        | _ -> 1

    let addPointY opComp =
        match opComp with
        | OpOpt.Compare -> 4
        | _ -> 1
         
    let drawPoint (basePoint:LoadPoint) (runPoint:LoadPoint) (opOpt:OpOpt)  = 
        basePoint.SX, basePoint.SY, runPoint.EX, runPoint.EY, addPointX opOpt, addPointY opOpt

    /// 좌표를 업데이트하여 새 좌표 반환
    let getNewPoint(loadPoint: LoadPoint, op, opOpt: OpOpt) = 
        let newPoint = loadPoint.Copy()
        match op with
        | Load -> newPoint.EX <- loadPoint.EX + addPointX opOpt
                  newPoint.EY <- loadPoint.EY + addPointY opOpt
        | And  -> let addY = loadPoint.SY + if (addPointY opOpt - loadPoint.SizeY) > 0 then addPointY opOpt else 0
                  newPoint.EX <- loadPoint.EX + addPointX opOpt
                  newPoint.EY <- max addY loadPoint.EY
        | Or   -> let addX = loadPoint.SX + if (addPointX opOpt - loadPoint.SizeX) > 0 then addPointX opOpt else 0
                  newPoint.EX <- max addX loadPoint.EX
                  newPoint.EY <- loadPoint.EY + addPointY opOpt
        newPoint

    let rec getPoint loader:LoadPoint =
        match loader with
        | LoadBase(contacts, loadPoint) -> loadPoint.Copy()
        | Extend(contacts, basePoint, exPoint) -> LoadPoint(basePoint.SX, basePoint.SY, exPoint.EX, exPoint.EY)
        | Mix(left, loadop, right, extend) ->
            let leftP = getPoint left
            let rightP = getPoint right
            let extendP = getPoint extend
            
            match loadop with
            | AndLoad -> LoadPoint(leftP.SX, leftP.SY, max rightP.EX extendP.EX, max (max leftP.EY rightP.EY) extendP.EY)
            | OrLoad  -> LoadPoint(leftP.SX, leftP.SY, max (max leftP.EX rightP.EX) extendP.EX, max rightP.EY extendP.EY)
            | _ -> failwithf "Unknown loadOp [%s]" loadop.ToText
                     
        | Empty -> LoadPoint(0,0,0,0)
    /// Blank line 요소를 추가하여 반환
    let addBlankLine (leftP: LoadPoint, rightP: LoadPoint) =
        let results = ResizeArray<string>()
        let vertexStartX = min leftP.EX rightP.EX
        let vertexEndX = max leftP.EX rightP.EX
        let vertexStartY = if leftP.EX > rightP.EX then rightP.SY else leftP.SY
        let vertexEndY = max leftP.EY rightP.EY
        let countY = rightP.SY - leftP.SY
        let countX = vertexEndX - vertexStartX

        // 좌측 수직 라인 생성
        vlineDownTo (rightP.SX - 1) leftP.SY countY |> results.AddRange
        // 우측 수직 라인 생성
        vlineDownTo (vertexEndX - 1) leftP.SY countY |> results.AddRange
        // 수평 라인 생성
        if leftP.EX <> rightP.EX then 
            hlineRightTo vertexStartX vertexStartY countX |> results.AddRange
        results

    /// 문자열에서 좌표 정보를 추출하는 함수
    let getCoord (str) =  
        match str with
        | ActivePattern.RegexPattern @"Coordinate=""([0-9]+)" [coord] -> int coord
        | _ -> failwithf "getCoord doesn't contain coord [%s]" str

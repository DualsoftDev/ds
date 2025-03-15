namespace Dsu.PLCConverter.FS

open System.Collections.Generic
open Dsu.PLCConverter.FS.XgiSpecs
open System.Text.RegularExpressions

module XgiRungUtils =
    
 ()
[<AutoOpen>]
/// 프로그램 XML 형태로 저장하기 위한 기본 구조
module XgiRung =
    let rungXml  (startY:int) (rung:Rung)  (usingDrirect:bool): string * int =
        let results = ResizeArray<string>()
        let loadBlocks = List<LoadBlock>()
        let lastBlocks = List<LoadBlock>()
        let branchBlocks = List<LoadBlock>()
        let mutable mutiCoilStartY = 0
        let mutable mutiCoilEndY = 0
        let mutable nX = 0
        let mutable nY = startY

        let getArgs (arg:string) = getXGIArgs arg usingDrirect

        let getlastNremove () =
            if (loadBlocks.Count = 0) then
                if (lastBlocks.length() = 0) then
                    errorHistory.Add(sprintf "Invalid step %d  [pou : %s]" pouStep pouName)
                    convertOk <- false
                    LoadBase(List[], LoadPoint(0,0,0,0))
                else
                    let lastBlock = lastBlocks |> Seq.head
                    let lastBlockP = getPoint lastBlock
                    let endX, startY = lastBlockP |> fun x -> x.EX, x.SY
                    vlineDownTo (endX-1) startY (nY - startY) |> results.AddRange
                    lastBlockP.SX <- lastBlockP.EX
                    lastBlockP.SY <- nY
                    lastBlockP.EX <- lastBlockP.SX
                    lastBlockP.EY <- lastBlockP.SY
                    LoadBase(List[], lastBlockP)
            else
                let last = loadBlocks |> Seq.last
                loadBlocks.Remove(last) |> ignore
                last



        let runLoader() =
            let firstY = nY
            let lastY =
                if (loadBlocks.Count = 1) then
                    let contX, contY, ret = drawLoaders loadBlocks
                    results.AddRange(ret)
                    nX <- contX
                    nY <- max mutiCoilEndY contY
                    let point = getPoint (loadBlocks |> Seq.head)
                    point.EY
                else if (loadBlocks.Count = 0) then
                    nY <- mutiCoilEndY
                    nY
                else
                    convertOk <- false
                    nY
            firstY, lastY


        let clearLoader(firstY, lastY, cmdY) =
            let maxY = max firstY (max lastY cmdY)
            if (loadBlocks.Count = 0) then
                vlineDownTo (nX - 1) (mutiCoilStartY) (lastY - mutiCoilStartY) |> results.AddRange
                nY <- maxY
            else
                if (branchBlocks.Count = 0) 
                    then branchBlocks.Add(loadBlocks |> Seq.head |> fun x-> x.Copy())

                lastBlocks.Clear()
                loadBlocks |> Seq.map (fun f -> f.Copy()) |> lastBlocks.AddRange
                loadBlocks.Clear()
                mutiCoilStartY <- firstY
                nY <- maxY
            mutiCoilEndY <- cmdY


        let mLoad() =
            let lastBlock = branchBlocks |> Seq.last
            let lastBlockP = lastBlock |> getPoint
            let endX, startY = lastBlockP |> fun x -> x.EX, x.SY
            vlineDownTo (endX-1) startY (nY - startY) |> results.AddRange
            nY <- max mutiCoilEndY nY
            lastBlockP.SX <- lastBlockP.EX
            lastBlockP.SY <- nY
            lastBlockP.EX <- lastBlockP.SX
            lastBlockP.EY <- lastBlockP.SY
            let newLoad = LoadBase(List[], lastBlockP)
            loadBlocks.Add(newLoad)


        let addLoad (arg, opOpt, opComp, opCompType) =
            // LoadPoint 생성, 첫 로드는 크기 1, 1로 설정
            let loadPoint = LoadPoint(0, startY, addPointX opOpt, (startY + addPointY opOpt))
            // LoadBase 생성, 첫 contact로 arg와 설정된 옵션을 사용
            let loadbase = LoadBase(List[contact(getArgs arg, Load, opOpt, opComp, opCompType)], loadPoint)
            // loadBlocks에 loadbase 추가
            loadBlocks.Add(loadbase)

        let addContact (arg, op, opOpt:OpOpt, opComp:OpComp, opCompType) =
            // lastBlocks && loadBlocks가 비어 있으면 mLoad 호출
            if(lastBlocks.Count = 0 && loadBlocks.Count = 0) then mLoad()
            // loadBlocks의 마지막 블록 가져옴
            let lastBlock = getlastNremove()
    
            // 재귀 함수로 마지막 블록 업데이트
            let rec updateLast(block) =
                match block with
                | LoadBase(contacts, loadPoint) ->
                    // 첫 추가 contact는 무조건 Load로 설정
                    if(contacts.Count = 0) 
                    then contacts.Add(contact(getArgs arg, Load, opOpt, opComp, opCompType))
                    else contacts.Add(contact(getArgs arg, op, opOpt, opComp, opCompType))
                    // 업데이트된 LoadBase 반환
                    LoadBase(contacts, getNewPoint(loadPoint, op, opOpt))

                | Extend(contacts, basePoint, exPoint) -> 
                    // Extend 블록일 경우 contact 추가 후 새 좌표로 업데이트
                    if(contacts.Count = 0) 
                    then contacts.Add(contact(getArgs arg, Load, opOpt, opComp, opCompType))
                    else contacts.Add(contact(getArgs arg, op, opOpt, opComp, opCompType))
                    Extend(contacts, basePoint, getNewPoint (exPoint, op, opOpt))

                | Mix(left, loadop, right, extend) ->
                    match extend with
                    | Extend(contacts, basePoint, exPoint) ->
                        // Mix 블록의 Extend가 비어 있지 않으면 업데이트 후 Mix로 설정
                        if(contacts.Count <> 0)
                        then Mix(left, loadop, right, updateLast extend)
                        else
                            // 비어 있는 경우 contact를 추가한 새 Extend 생성
                            let oldPoint = getPoint block
                            let conts = List[contact(getArgs arg, op, opOpt, opComp, opCompType)]
                            let newExtend = Extend(conts, oldPoint, getNewPoint (oldPoint, op, opOpt))
                            Mix(left, loadop, right, newExtend)
                    | _ -> failwithf "Extend must be Extend [err: %s]" extend.ToText

                | Empty -> Empty

            // 업데이트된 블록을 loadBlocks에 추가
            let updateLoad = updateLast lastBlock
            loadBlocks.Add(updateLoad)

        let addCompare (arg, newCmp, newOp, opCompType) =
            let op = Op.getOp newOp // 연산자 가져오기
            let opCompType = OpCompType.getOp opCompType // 비교 연산 타입 가져오기
            // 연산자와 비교 유형에 따라 addLoad 또는 addContact 호출
            match newCmp with
                | "GT2"  ->  if(op = Load) then addLoad (arg, Compare, GT, opCompType)  else  addContact (arg, op, Compare, GT, opCompType)
                | "GE2"  ->  if(op = Load) then addLoad (arg, Compare, GE, opCompType)  else  addContact (arg, op, Compare, GE, opCompType)
                | "EQ2"  ->  if(op = Load) then addLoad (arg, Compare, EQ, opCompType)  else  addContact (arg, op, Compare, EQ, opCompType)
                | "LE2"  ->  if(op = Load) then addLoad (arg, Compare, LE, opCompType)  else  addContact (arg, op, Compare, LE, opCompType)
                | "LT2"  ->  if(op = Load) then addLoad (arg, Compare, LT, opCompType)  else  addContact (arg, op, Compare, LT, opCompType)
                | "NE"   ->  if(op = Load) then addLoad (arg, Compare, NE, opCompType)  else  addContact (arg, op, Compare, NE, opCompType)
                | _ -> 
                    // 알 수 없는 연산자일 경우 오류 메시지 추가
                    errorHistory.Add(sprintf  "Unknown op [%s]" newCmp)
                    convertOk <- false

        let mergeLoad op =
            // 연산자에 따라 AndLoad 또는 OrLoad로 설정
            let oper =
                match op with
                | "AND LOAD" -> AndLoad
                | "OR LOAD" -> OrLoad
                | _ -> 
                    errorHistory.Add(sprintf  "Unknown op [%s]" op)
                    convertOk <- false
                    Base

            // 마지막 두 블록 가져오기
            let last = getlastNremove()
            let lastPre = getlastNremove()

            // 마지막 블록의 위치 정보 가져오기
            let lastP = getPoint last
            let lastPreP = getPoint lastPre

            // 연산자에 따른 좌표 이동값 설정
            let shiftX, shiftY = 
                match oper with
                | AndLoad   ->  lastPreP.EX, lastPreP.SY - startY
                | OrLoad    ->  0, lastPreP.SizeY
                | _ -> 
                    errorHistory.Add(sprintf  "Unknown op [%s]" op)
                    convertOk <- false
                    0,0

            // Mix 블록의 모든 요소에 좌표 이동 적용
            let rec updateMixPoint mixloader =
                match mixloader with
                | LoadBase(contacts , loadPoint) ->
                    loadPoint.Shift(shiftX, shiftY)

                | Extend(contacts , basePoint, exPoint) ->
                    if(contacts.Count <> 0) then 
                        basePoint.Shift(shiftX, shiftY)
                        exPoint.Shift(shiftX, shiftY)

                | Mix(left, loadop, right, extend) ->
                    updateMixPoint left
                    updateMixPoint right
                    updateMixPoint extend
                | _ -> ()

            // 마지막 Load 블록 위치 업데이트
            let newLast =
                match last with
                | LoadBase(contacts, loadPoint) -> 
                    updateMixPoint last
                    LoadBase(contacts, loadPoint) 
                | Extend(contacts, basePoint, exPoint) -> 
                    updateMixPoint last
                    Extend(contacts, basePoint, exPoint)
                | Mix(left, loadop, right, extend) ->
                    updateMixPoint left
                    updateMixPoint right
                    updateMixPoint extend
                    Mix(left, loadop, right, extend)
                | Empty -> Empty

            let newLastPoint = getPoint newLast
            let extendLoad = Extend(List[], newLastPoint, newLastPoint)
            // 새로운 Mix 블록 생성하여 loadBlocks에 추가
            let newLoad = Mix(lastPre, oper, newLast, extendLoad)
            loadBlocks.Add(newLoad)

        let actionCoilCmd (arg:string, op) =
            // 현재 로드블록 실행
            let firstY, lastY = runLoader()
            nY <- min firstY lastY

            // 명령어에 따라 타이머, 카운터, 코일 그리기 함수 호출
            let cmdSizeY =
                match arg with
                | ActivePattern.StartsWith "ST" ()
                | ActivePattern.StartsWith "T" () ->
                    let highTmr = op = "OUTH" 
                    let y, ret = drawFBTime ((if(arg.StartsWith("ST")) then "TMR" else "TON_UINT"), getArgs arg, highTmr, nX, nY)
                    results.AddRange(ret); y
                | ActivePattern.StartsWith "C" () ->
                    let y, ret = drawFBCount ("CTU_UINT", getArgs arg, nX, nY)
                    results.AddRange(ret); y
                | _ ->
                    let newArg = getArgs arg |> Seq.head
                    let y, ret = drawCoil (newArg, nX, nY, (getCoilMode op))
                    results.AddRange(ret); y

            // 로더 클리어
            clearLoader(firstY, lastY, cmdSizeY)

        let actionAllCmd (arg, cmdOrg:string, line) =
            // 현재 로드블록 실행
            let firstY, lastY  = runLoader()
            nY <- min firstY lastY

            let cmd = cmdOrg.Split(';').[0]

            // 명령어가 특정 목록에 있는지 확인 후 해당 함수 호출
            let cmdSizeY =
                if ListExCMD.Contains(cmd) then
                    let newY = if(ListExCMDSingleLine.Contains(cmd)) then startY else nY
                    let y, ret = drawExCMD (cmdOrg, getArgs arg, nX, newY)
                    results.AddRange(ret); y
                else if ((cmd = "X") || cmd.Contains(".")) then // X 또는 . 포함 명령어 처리
                    let y, ret = drawX (line.Instruction, getArgs arg, nX, nY)
                    results.AddRange(ret); y
                else 
                    let y, ret = drawFB (cmdOrg, line.Instruction, (getArgs arg), arg, nX, nY)
                    results.AddRange(ret); y

            // 로더 클리어
            clearLoader(firstY, lastY, cmdSizeY)

        let mutable lableOp = "" 

        rung
            |> Seq.map xgiLineConvertor // xgiLineConvertor 함수를 사용하여 rung 내의 각 줄을 변환
            |> Seq.filter (fun f -> convertOk) // 변환 성공 여부를 확인하여 필터링
            |> Seq.iter (fun (line, op, arg) ->
                pouStep <- if(line.StepNo.IsNone) then 0 else line.StepNo.Value // pouStep 값 설정
                match op with
                // new Load 생성
                | "LOAD"     -> addLoad (arg, Normal,   NotComp, NoType)
                | "LOADN"    -> addLoad (arg, Falling,  NotComp, NoType)
                | "LOADP"    -> addLoad (arg, Rising,   NotComp, NoType)
                | "LOADN NOT"-> addLoad (arg, NegFalling,  NotComp, NoType)
                | "LOADP NOT"-> addLoad (arg, NegRising,   NotComp, NoType)
                | "LOAD NOT" -> addLoad (arg, Neg,      NotComp, NoType)
                // and operation
                | "AND"     ->  addContact (arg, And, Normal,   NotComp, NoType)
                | "ANDN"    ->  addContact (arg, And, Falling,  NotComp, NoType)
                | "ANDP"    ->  addContact (arg, And, Rising,   NotComp, NoType)
                | "ANDN NOT"->  addContact (arg, And, NegFalling,  NotComp, NoType)
                | "ANDP NOT"->  addContact (arg, And, NegRising,   NotComp, NoType)
                | "AND NOT" ->  addContact (arg, And, Neg,      NotComp, NoType)
                // or operation
                | "OR"      ->  addContact (arg, Or, Normal,    NotComp, NoType)
                | "ORN"     ->  addContact (arg, Or, Falling,   NotComp, NoType)
                | "ORP"     ->  addContact (arg, Or, Rising,    NotComp, NoType)
                | "ORN NOT" ->  addContact (arg, Or, NegFalling,   NotComp, NoType)
                | "ORP NOT" ->  addContact (arg, Or, NegRising,    NotComp, NoType)
                | "OR NOT"  ->  addContact (arg, Or, Neg,       NotComp, NoType)
        
                | "NOT"     ->  addContact ("NOT", And, Inverter, NotComp, NoType)
                | "EGP"     ->  addContact ("MEP", And, RisingEdge , NotComp, NoType); sprintf "자동변환 항목 : EGP->MEP (%s 삭제)" arg |> warningHistory.Add
                | "MEP"     ->  addContact ("MEP", And, RisingEdge , NotComp, NoType)
                | "EGF"     ->  addContact ("MEF", And, FallingEdge, NotComp, NoType); sprintf "자동변환 항목 : EGF->MEF (%s 삭제)" arg |> warningHistory.Add
                | "MEF"     ->  addContact ("MEF", And, FallingEdge, NotComp, NoType)

                // branch operation
                | "MPUSH" -> if(loadBlocks.Count <> 0) then branchBlocks.Add(loadBlocks |> Seq.head |> fun x -> x.Copy() )
                | "MLOAD"   -> mLoad()
                | "MPOP"    -> mLoad(); branchBlocks.RemoveAt(branchBlocks.Count - 1)

                // Load 블럭 합성
                | "AND LOAD"
                | "OR LOAD" -> mergeLoad op
                // Program 종료
                | "END"     -> drawEnd nY |> results.AddRange
                | "SBRT"    -> 
                     nY <- nY + 1
                     actionAllCmd(lableOp, op, line)
                        
                // Coil 처리
                | "OUT"
                | "OUTP"
                | "OUTN"
                | "OUTH" // 고속 타이머
                | "OUT NOT"
                | "SET"  -> 
                        actionCoilCmd(arg, op)
                
                | "RST"  -> Symbol(arg) |> fun (a) -> if(a.VarType <> VarType.BOOL) 
                                                        then  actionAllCmd("0;" + arg, "MOVE:WORD", line) 
                                                        else  actionCoilCmd(arg, op)

                | "RungComment" -> drawRungComment (arg, nY) |> results.AddRange; nY <- nY + 1

                // 레이블 저장
                | ActivePattern.RegexPattern @"([P])([0-9]+)" [p; label] -> lableOp <- op; drawLabel (op, nY) |> results.AddRange; nY <- nY + 1
                // 비교 operation
                | ActivePattern.RegexPattern @"([A-Z]+)=([A-Z0-9]+):([A-Z]+)" [newOp; newCmp; dataType] ->
                    addCompare (arg, newCmp, newOp, dataType)
                | _ ->  // 모든 CMD 처리
                    actionAllCmd(arg, op, line)
                ) |> ignore

        let xml =
            if(convertOk) 
            then
                results
                    |> Seq.distinct 
                    |> Seq.sortBy (fun f -> getCoord f)
                    |> String.concat "\r\n"
            else
                nY <- startY
                printRung rung +  "\r\n\r\n변환불가원인:" + (errorHistory |> String.concat "\r\n")
                |> System.Security.SecurityElement.Escape 

        xml, nY

namespace Dsu.PLCConverter.FS

open System.Diagnostics
open Dual.Common
open Dsu.PLCConverter.FS.XgiSpecs.Config.POU.Program.LDRoutine
open System.Collections.Generic
open System
open ActivePattern
open Dual.Common
open Dsu.PLCConverter.FS.XgiSpecs


[<AutoOpen>]
/// 프로그램 XML 형태로 저장하기 위한 기본 구조
module XgiRung =
    let rungXml  (startY:int) (rung:Rung)  (dicSym:IDictionary<string,(string*string)>) (usingDrirect:bool): string * int =
        let results = ResizeArray<string>()
        let loadBlocks = List<LoadBlock>()
        let lastBlocks = List<LoadBlock>()
        let branchBlocks = List<LoadBlock>()
        let mutable mutiCoilStartY = 0
        let mutable mutiCoilEndY = 0
        let mutable nX = 0
        let mutable nY = startY
    
        let getArgs (arg:string) =
                arg.SplitBy(' ')
                |> Seq.map (fun f  -> 
                                //dicSym : f.OldAddress(), (f.Address, f.Name) X123, (%IX1.1.1, X_123)
                                if(dicSym.ContainsKey(f)) 
                                then 
                                    if(usingDrirect && not (AutoType.Contains(f.Substring(0,1))))
                                    then
                                        let address = Tuple.tuple1st dicSym.[f]
                                        if(address = "")
                                        then Tuple.tuple2nd dicSym.[f] 
                                        else address
                                    else Tuple.tuple2nd dicSym.[f] 
                                else f
                                )
                |> Seq.toList
                                
        let getlastNremove () =
            if(loadBlocks.Count = 0) 
            then  
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
                if(loadBlocks.Count = 1)
                then
                    let contX, contY, ret = drawLoaders loadBlocks
                    results.AddRange(ret)
                    nX <- contX
                    nY <- max mutiCoilEndY contY  // Coil 그리기위해서 초기 Load 위치 가져옴
                    let point = getPoint (loadBlocks |> Seq.head)
                    point.EY
                else if(loadBlocks.Count = 0)
                then
                    nY <- mutiCoilEndY            // loadBlocks.Count == 0 이면 mutiCoilEndY 적용
                    nY
                else
                    errorHistory.Add(sprintf  "runLoader [count < 2] check clearLoader [current count %d]" loadBlocks.Count)
                    convertOk <- false
                    nY

            firstY, lastY

        let clearLoader(firstY, lastY, cmdY) =
            let maxY = max firstY (max lastY cmdY)
            if(loadBlocks.Count = 0)
            then
                vlineDownTo (nX - 1) (mutiCoilStartY) (lastY - mutiCoilStartY) |> results.AddRange
                nY <- maxY
            else
                if(branchBlocks.Count = 0) // CMD 처리 후 마지막 MPUSH 일경우 임시 추가
                    then branchBlocks.Add(loadBlocks |> Seq.head |> fun x-> x.Copy())

                lastBlocks.Clear()
                loadBlocks |> Seq.map (fun f->f.Copy()) |> lastBlocks.AddRange
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
            //let loadY  = if(nY = startY) then startY else 0 // 2번째 로드부터는 상대좌표
            //let loadY  = startY 
            let loadPoint = LoadPoint(0, startY, addPointX opOpt, (startY + addPointY opOpt)) //첫 로드는 Size 1, 1
            let loadbase = LoadBase(List[contact(getArgs arg, Load, opOpt, opComp, opCompType)], loadPoint)
            loadBlocks.Add(loadbase)

        let addContact (arg, op, opOpt:OpOpt, opComp:OpComp, opCompType) =
            if(loadBlocks.Count = 0) then mLoad()
            let lastBlock = getlastNremove()
            let rec updateLast(block) =
                match block with
                | LoadBase(contacts, loadPoint) ->
                    if(contacts.Count = 0) //mLoad로 실행 후 첫 추가 contact는 무조건 Load로 인식
                    then contacts.Add(contact(getArgs arg, Load, opOpt, opComp, opCompType))
                    else contacts.Add(contact(getArgs arg, op, opOpt, opComp, opCompType))
                    LoadBase(contacts, getNewPoint(loadPoint, op, opOpt))

                | Extend(contacts, basePoint, exPoint) -> 
                    if(contacts.Count = 0) 
                    then contacts.Add(contact(getArgs arg, Load, opOpt, opComp, opCompType))
                    else contacts.Add(contact(getArgs arg, op, opOpt, opComp, opCompType))
                    Extend(contacts, basePoint, getNewPoint (exPoint, op, opOpt))

                | Mix(left, loadop, right, extend) ->
                    match extend with
                    | Extend(contacts, basePoint, exPoint) ->
                        if(contacts.Count <> 0)
                        then Mix(left, loadop, right, updateLast extend)
                        else
                            let oldPoint = getPoint block
                            let conts = List[contact(getArgs arg, op, opOpt, opComp, opCompType)]
                            let newExtend = Extend(conts, oldPoint, getNewPoint (oldPoint, op, opOpt))
                            Mix(left, loadop, right, newExtend)
                    | _ -> failwithlogf "Extend must be Extend [err: %s]" extend.ToText

                | Empty -> Empty
            let updateLoad = updateLast lastBlock
            loadBlocks.Add(updateLoad)

        let addCompare (arg, newCmp, newOp, opCompType) =
            let op = Op.getOp newOp
            let opCompType = OpCompType.getOp opCompType
            match newCmp with
                    | "GT2"  ->  if(op = Load) then addLoad (arg, Compare, GT, opCompType)  else  addContact (arg, op, Compare, GT, opCompType)
                    | "GE2"  ->  if(op = Load) then addLoad (arg, Compare, GE, opCompType)  else  addContact (arg, op, Compare, GE, opCompType)
                    | "EQ2"  ->  if(op = Load) then addLoad (arg, Compare, EQ, opCompType)  else  addContact (arg, op, Compare, EQ, opCompType)
                    | "LE2"  ->  if(op = Load) then addLoad (arg, Compare, LE, opCompType)  else  addContact (arg, op, Compare, LE, opCompType)
                    | "LT2"  ->  if(op = Load) then addLoad (arg, Compare, LT, opCompType)  else  addContact (arg, op, Compare, LT, opCompType)
                    | "NE"  ->   if(op = Load) then addLoad (arg, Compare, NE, opCompType)  else  addContact (arg, op, Compare, NE, opCompType)
                    | _ -> 
                        errorHistory.Add(sprintf  "Unknown op [%s]" newCmp)
                        convertOk <- false

        let mergeLoad op =
            let oper =
                match op with
                | "AND LOAD" -> AndLoad
                | "OR LOAD" -> OrLoad
                | _ -> 
                    errorHistory.Add(sprintf  "Unknown op [%s]" op)
                    convertOk <- false
                    Base

            let last = getlastNremove()
            let lastPre =  getlastNremove()

            let lastP = getPoint last
            let lastPreP = getPoint lastPre

            let shiftX, shiftY = 
                match oper with
                | AndLoad   ->  lastPreP.EX, lastPreP.SY - startY
                | OrLoad    ->  0, lastPreP.SizeY
                //| AndLoad   -> if(lastPreP.SizeZero) then lastPreP.EX, lastPreP.EY - startY else lastPreP.SizeX, 0
                //| OrLoad    -> if(lastPreP.SizeZero) then lastPreP.EX, lastPreP.EY - startY else 0, lastPreP.SizeY
                | _ -> 
                        errorHistory.Add(sprintf  "Unknown op [%s]" op)
                        convertOk <- false
                        0,0

            let rec updateMixPoint mixloader=
                match mixloader with
                | LoadBase(contacts , loadPoint)
                    -> loadPoint.Shift(shiftX,shiftY)

                | Extend(contacts , basePoint, exPoint)
                    ->
                        if(contacts.Count <> 0) then 
                            basePoint.Shift(shiftX, shiftY)
                            exPoint.Shift(shiftX, shiftY)

                | Mix(left, loadop, right, extend) ->
                    updateMixPoint left
                    updateMixPoint right
                    updateMixPoint extend
                | _ -> ()

            //마지막 Load 의 Block 위치 업데이트
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
            let newLoad = Mix(lastPre, oper, newLast, extendLoad)
            loadBlocks.Add(newLoad)

        let actionCoilCmd (arg, op) =
            let firstY, lastY = runLoader()
            nY <- min firstY lastY
            let cmdSizeY =
                    match arg with
                    | ActivePattern.StartsWith "T" () ->
                        let highTmr = op = "OUTH" 
                        let y, ret = drawFBTime ("TON_UINT",getArgs arg, highTmr, nX, nY)
                        results.AddRange(ret); y
                    | _->
                        let newArg = getArgs arg |> Seq.head
                        let y, ret = drawCoil (newArg, nX, nY, (getCoilMode op))
                        results.AddRange(ret); y

            clearLoader(firstY, lastY, cmdSizeY)

        let actionAllCmd (arg, cmdOrg:string, line) =
            let firstY, lastY  = runLoader()
            nY <- min firstY lastY
            let cmd = cmdOrg.Split(';').[0]
            let cmdSizeY =
                if(ExCMDType.Contains(cmd))
                then
                    let newY = if(ExCMDTypeSingleLine.Contains(cmd)) then startY else nY
                    let y, ret = drawExCMD (cmdOrg, getArgs arg, nX, newY)
                    results.AddRange(ret);y
                else if((cmd = "X") || cmd.Contains(".")) //변환불가 CMD 변환 사전에 없는 것은 X  //GP.OUT
                then 
                    let y, ret = drawX (line.Instruction, getArgs arg, nX, nY)
                    results.AddRange(ret);y
                else 
                    let y, ret = drawFB (cmdOrg, getArgs arg, nX, nY)
                    results.AddRange(ret);y

            clearLoader(firstY, lastY, cmdSizeY)

        rung
            |> Seq.map xgiLineConvertor
            |> Seq.filter (fun f-> convertOk)
            |> Seq.iter (fun (line, op, arg) ->
                match op with
                //new Load 생성
                | "LOAD"     -> addLoad (arg, Normal,   NotComp, NoType)
                | "LOADN"    -> addLoad (arg, Falling,  NotComp, NoType)
                | "LOADP"    -> addLoad (arg, Rising,   NotComp, NoType)
                | "LOAD NOT" -> addLoad (arg, Neg,      NotComp, NoType)
                //and operation
                | "AND"     ->  addContact (arg, And, Normal,   NotComp, NoType)
                | "ANDN"    ->  addContact (arg, And, Falling,  NotComp, NoType)
                | "ANDP"    ->  addContact (arg, And, Rising,   NotComp, NoType)
                | "AND NOT" ->  addContact (arg, And, Neg,      NotComp, NoType)
                //or operation
                | "OR"      ->  addContact (arg, Or, Normal,    NotComp, NoType)
                | "ORN"     ->  addContact (arg, Or, Falling,   NotComp, NoType)
                | "ORP"     ->  addContact (arg, Or, Rising,    NotComp, NoType)
                | "OR NOT"  ->  addContact (arg, Or, Neg,       NotComp, NoType)

                | "NOT"     ->  addContact ("NOT", And, Inverter, NotComp, NoType)
                | "EGP"     ->  addContact ("MEP", And, RisingEdge , NotComp, NoType); sprintf "자동변환 항목 : EGP->MEP (%s 삭제)" arg |> warningHistory.Add
                | "MEP"     ->  addContact ("MEP", And, RisingEdge , NotComp, NoType)
                | "EGF"     ->  addContact ("MEF", And, FallingEdge, NotComp, NoType); sprintf "자동변환 항목 : EGF->MEF (%s 삭제)" arg |> warningHistory.Add
                | "MEF"     ->  addContact ("MEF", And, FallingEdge, NotComp, NoType)

                //branch operation
                | "MPUSH" -> if(loadBlocks.Count <> 0) then branchBlocks.Add(loadBlocks |> Seq.head |> fun x-> x.Copy() ) // loadBlocks없을경우는 branchBlocks에 미리넣어둠
                | "MLOAD"   -> mLoad()
                | "MPOP"    -> mLoad();branchBlocks.RemoveAt(branchBlocks.Count - 1)

                //Load 블럭 합성
                | "AND LOAD"
                | "OR LOAD" -> mergeLoad op
                //Program 종료
                | "END"     -> drawEnd nY |> results.AddRange
                | "FEND"    -> "자동변환 항목 : JUMP의 마지막 포인트 FEND 무시" |> warningHistory.Add ;  nY <- nY + 1
                //Coil 처리
                | "OUT"
                | "OUTP"
                | "OUTN"
                | "OUTH" //고속 타이머
                | "OUT NOT"
                | "SET"  -> actionCoilCmd(arg, op)
                | "RST"  -> Symbol(arg) |> fun (a) -> if(a.VarType <> VarType.BOOL) then  actionAllCmd("0 "+arg , "MOVE:WORD", line) else  actionCoilCmd(arg, op)

                | "RungComment" -> drawRungComment (arg, nY) |> results.AddRange; nY <- nY + 1


                //레이블 저장
                | ActivePattern.RegexPattern @"([P])([0-9]+)" [p; label] -> drawLabel (op, nY) |> results.AddRange; nY <- nY + 1
                //비교 operation
                | ActivePattern.RegexPattern @"([A-Z]+)=([A-Z0-9]+):([A-Z]+)" [newOp; newCmp; dataType] ->
                                                             addCompare (arg, newCmp, newOp, dataType)
                | _ ->  //모든 CMD 처리
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

        xml , nY
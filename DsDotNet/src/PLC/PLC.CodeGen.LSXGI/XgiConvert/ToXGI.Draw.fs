namespace Dsu.PLCConverter.FS

open Dsu.PLCConverter.FS.XgiSpecs.Config.POU.Program.LDRoutine
open Dual.Common
open System.Collections.Generic

[<AutoOpen>]
/// 프로그램 LADDER 형태로 그리기 기본 구조
module XgiDraw =

    /// 프로그램 END 그리기
    let drawEnd(y:int) =
        let results = ResizeArray<string>()
        let yy = y * 1024 + 1
        sprintf "\t\t<Element ElementType=\"%d\" Coordinate=\"%d\" Param=\"90\"></Element>" 
                    (int ElementType.MultiHorzLineMode) yy |> results.Add
        sprintf "\t\t<Element ElementType=\"%d\" Coordinate=\"%d\" Param=\"END\">END</Element>" 
                    (int ElementType.FBMode) (yy+93) |> results.Add

        results

    /// 프로그램 END 그리기
    let drawRungComment(rungComment:string, y:int) =
        let results = ResizeArray<string>()

        let yy = y * 1024 + 1
        let result =
            sprintf "\t\t<Element ElementType=\"%d\" Coordinate=\"%d\">%s</Element>" 
                    (int ElementType.RungCommentMode) yy (rungComment |> System.Security.SecurityElement.Escape)
        results.Add(result)
        results


    /// 레이블 그리기
    let drawLabel(label:string, y:int) =
        let results = ResizeArray<string>()

        let yy = y * 1024 + 1
        let result =
            sprintf "\t\t<Element ElementType=\"%d\" Coordinate=\"%d\">%s</Element>" 
                    (int ElementType.LabelMode) yy (label)
        results.Add(result)
        results


    /// drawCoil Coil 그리기
    let drawCoil (arg:string, x, y, elementType:ElementType)=
        let results = ResizeArray<string>()
        if(x < coilCellX)
        then
            mutiEndLine x coilCellX y |> results.Add
            let c = coord coilCellX y
            results.Add(elementFull (int elementType) c "" (arg))
        else
            errorHistory.Add(sprintf "Too many contacts  [%d]" x)
            convertOk <- false

        y + 1, results

    let drawNormalC (op, opOpt:OpOpt, (args:seq<string>), basePoint:LoadPoint, runPoint:LoadPoint) =
        let results = ResizeArray<string>()
        let initX, initY, runX, runY, addX, addY = drawPoint basePoint runPoint opOpt    
       
        //산전 타이머 출력을 조건으로 사용시 T123213.Q 형식으로 사용
        let contactXml (c:int) =
            let contactSym = getNewArgs args |> Seq.head
            elementFull (int (getContactMode opOpt))  c "" (contactSym)

        let x, y =
            match op with
            | Load ->
                initX, initY
            | And ->
                runX, initY
            | Or ->
                let leftP  = runPoint
                let rightP = LoadPoint(initX, runY, initX + addX, runY + addY)
                addBlankLine (leftP, rightP) |> results.AddRange
                initX, runY

        let coordLocal = coord x y
        results.Add(contactXml coordLocal)
        let newRunPt = getNewPoint (runPoint, op, OpOpt.Normal)
        newRunPt, results

    let drawCompare (op, opComp:OpComp, opCompType:OpCompType, (args:seq<string>), basePoint:LoadPoint, runPoint:LoadPoint) =
        let results = ResizeArray<string>()
        let initX, initY, runX, runY, addX, addY = drawPoint basePoint runPoint OpOpt.Compare    


        let func = opComp.ToText
        let funcFind =
            if(opComp = OpComp.NE)
            then sprintf "%s_%s" opComp.ToText opCompType.ToText
            else sprintf "%s2_%s" opComp.ToText opCompType.ToText
        let mutable paraX = initX
        let mutable paraY = initY
        let x, y =
            match op with
            | Load ->
                (initX + 1), initY
            | And ->
                paraX <- runX
                paraY <- initY
                (runX + 1), initY
            | Or ->
                paraX <- initX
                paraY <- runY
                let leftP  = runPoint
                let rightP = LoadPoint(initX, runY, initX + addX, runY + addY)
                addBlankLine (leftP, rightP) |> results.AddRange
                (initX + 1), runY

        let coordLocal = coord x y
        let FB_Param = sprintf "Param=\"%s\"" ((getFBXML( funcFind ,func ,",")))
        results.Add(elementFull (int ElementType.VertFBMode) coordLocal FB_Param "")

        let newArgs = newDoubleArgs(opCompType.ToText, args)

        let in1 = 1
        let in2 = 2
        let out1X  = 2
        let out1Y  = 1
        results.Add(elementFull (int ElementType.HorzLineMode) (coord (paraX) (paraY)) "" "")
        results.Add(elementFull (int ElementType.VariableMode) (coord (paraX) (paraY + in1)) "" (newArgs |> Seq.head))
        results.Add(elementFull (int ElementType.VariableMode) (coord (paraX) (paraY + in2)) "" (newArgs |> Seq.last))
        results.Add(elementFull (int ElementType.HorzLineMode) (coord (paraX + out1X) (paraY + out1Y)) "" "")
        vlineDownTo (paraX + 2) paraY 1 |> results.AddRange // 우측 vertical lines
        let newRunPt = getNewPoint (runPoint, op, OpOpt.Compare)
        newRunPt, results

    
    let drawEdge (op, opOpt:OpOpt, (args:seq<string>),  basePoint:LoadPoint, runPoint:LoadPoint) =
        let results = ResizeArray<string>()
        let initX, initY, runX, runY, addX, addY = drawPoint basePoint runPoint opOpt    

        let x, y =
            match op with
            | Load ->
                (initX+1), initY
            | And ->
                (runX+1), initY
            | Or ->
                let leftP  = runPoint
                let rightP = LoadPoint(initX, runY, initX + addX, runY + addY)
                addBlankLine (leftP, rightP) |> results.AddRange
                (initX+1), runY

        let coordLocal = coord x y

        let funcFind = 
            if(opOpt = OpOpt.RisingEdge)       then R_TRIG
            else if(opOpt = OpOpt.FallingEdge) then F_TRIG
            else failwithlogf "Unknown drawEdge [%s]" opOpt.ToText

        let instName = InstFun.getInst(insts, funcFind)
        insts.Add(funcFind, instName)
        
        let FB_Param = sprintf "Param=\"%s\"" ((getFBXML(funcFind.ToText, funcFind.ToText ,sprintf "%s,VAR" instName)))

        results.Add(elementFull (int ElementType.VertFBMode) coordLocal FB_Param "")
        hlineRightTo (x - 1) y 1 |> results.AddRange
        hlineRightTo (x + 1) y 1 |> results.AddRange
        
        let newRunPt = getNewPoint (runPoint, op, opOpt)
        newRunPt, results

    /// drawContact Contact 그리기
    let drawContact (contact:contact, basePoint:LoadPoint, runPoint:LoadPoint) =
        let args, op, opOpt, opComp, opCompType = contact

        match opOpt with
        | RisingEdge 
        | FallingEdge -> drawEdge (op, opOpt,args, basePoint, runPoint)
        | Compare     -> drawCompare (op, opComp, opCompType, args, basePoint, runPoint)
        |_            -> drawNormalC (op, opOpt, args, basePoint, runPoint)

    /// drawLoaders Loader들 그리기
    let drawLoaders (loaders:List<LoadBlock>) =
        let results = ResizeArray<string>()
        let mutable continueX = 0
        let mutable continueY = 0
        let mutable runX = 0
        let mutable runY = 0
        let rec drawLoader(load:LoadBlock) =
            match load with
            | LoadBase(contacts, pt) ->
                if(contacts.Count <> 0)
                then
                    let runPt = pt.Copy()
                    runPt.EX <- runPt.SX
                    runPt.EY <- runPt.SY
                    let basePt = runPt.Copy()

                    continueX <- pt.EX
                    continueY <- pt.SY
                    contacts
                    |> Seq.map (fun con ->
                                    let newPt, ret = drawContact(con, basePt, runPt)
                                    runPt.Update(newPt);  results.AddRange(ret))
                    |> Array.ofSeq 
                    |> ignore
                else
                    continueX <- pt.EX

            | Extend(contacts, basePt, exPt) ->
                if(contacts.Count <> 0)
                then
                    let runPt = basePt.Copy()
                    let basePt = basePt.Copy()
                    continueX <- exPt.EX
                    continueY <- exPt.SY
                    contacts
                    |> Seq.map (fun con ->
                                    let newPt, ret = drawContact(con, basePt, runPt)
                                    runPt.Update(newPt);  results.AddRange(ret))
                    |> Array.ofSeq 
                    |> ignore
                else
                    continueX <- exPt.EX

            | Mix(left, op, right, extend) ->
                drawLoader (left)
                drawLoader (right)
                drawLoader (extend)

                let leftP = getPoint left
                let rightP = getPoint right

                match op with
                | OrLoad ->
                    addBlankLine (leftP, rightP) |> results.AddRange

                | _ -> ()

            | Empty -> ()

        loaders
        |> Seq.map drawLoader
        |> Array.ofSeq 
        |> ignore;
        continueX, continueY, results
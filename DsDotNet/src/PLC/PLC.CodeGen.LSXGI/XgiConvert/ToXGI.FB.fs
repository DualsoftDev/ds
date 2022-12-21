namespace Dsu.PLCConverter.FS

open Dsu.PLCConverter.FS.XgiSpecs.Config.POU.Program.LDRoutine
open Dual.Common
open XgiBaseXML
open Dual.Common.ActivePattern

[<AutoOpen>]
///XgiFB XGI로 FB 변환을 위한 기본 구조
module XgiFB =
    let drawFBTime ((func:string), (args:seq<string>),highTmr:bool, x, y) =
        let results = ResizeArray<string>()
        let fbCellX = fbCellX x
        let in1 = 1
        let in2 = 2

        match func with
        | "TON_UINT" ->
            mutiEndLine (x) (fbCellX - 1) y |> results.Add
            let c = coord (fbCellX) y
            let instance = args |> Seq.head
            let timeArgs = getNewArgs args
            let time =   timeArgs |> Seq.last
            let ton_unit =   if(highTmr) then sprintf "%d" XgiOpt.TimerHighSpeed else sprintf "%d" XgiOpt.TimerLowSpeed

            //let time = args |> Seq.filter (fun a -> a.StartsWith("K")) |> Seq.last |> fun time -> time.TrimStart('K')
            let FB_Param = sprintf "Param=\"%s\"" ((getFBXML(func, func, (instance + ",VAR"))))
            results.Add(elementFull (int ElementType.VertFBMode) c FB_Param instance)

            let c = coord (fbCellX - 1) (y + in1)
            results.Add(elementFull (int ElementType.VariableMode) c "" time)
            let c = coord (fbCellX - 1) (y + in2)
            results.Add(elementFull (int ElementType.VariableMode) c "" ton_unit )

        | _ -> 
            errorHistory.Add(sprintf "Unknown cmd [%s]" func)
            convertOk <- false

        y + 4, results

    let drawPulse (x, y, useFB:bool) =
        let results = ResizeArray<string>()
        let fbCellX = fbCellX x
        let c = coord (fbCellX-2) y
        let instName = InstFun.getInst(insts, R_TRIG)
        insts.Add(R_TRIG, instName)
                  
        let FB_Param = sprintf "Param=\"%s\"" ((getFBXML(R_TRIG.ToText, R_TRIG.ToText ,sprintf "%s,VAR" instName)))
        results.Add(elementFull (int ElementType.VertFBMode) c FB_Param "")
        mutiEndLine (x) (fbCellX-3) y |> results.Add
        mutiEndLine (fbCellX-1) (if(useFB) then (fbCellX) else (coilCellX)) y |> results.Add
        //hlineRightTo (fbCellX - 1) y 2 |> results.AddRange
        results

   
    let drawExCMD ((func:string), (args:seq<string>), x, y) =
        let results = ResizeArray<string>()
        let usePulse = func.Contains(";P")
        let func = func.Split(';').[0] 
        let fbCellX = fbCellX x
        let mutable fX = x
        let mutable fY = y

        if(usePulse)
        then
            drawPulse (fX, fY, false) |> results.AddRange
        else
            mutiEndLine fX coilCellX fY |> results.Add

        //test ahn End line 겹침
        let FB_Param = 
            if(args.length() > 0)
            then 
                if(not (ExCMDTypeHasPara.Contains(func)) && args.length() > 0)
                then
                    sprintf "변환시 자동 삭제 항목 : %s" (args |> String.concat " ") |> warningHistory.Add 
                    sprintf "Param=\"%s\"" func 
                else
                    let para = getNewArgs args |> Seq.head
                    sprintf "Param=\"%s,%s\"" func para
            else
                sprintf "Param=\"%s\"" func 

        let c = coord coilCellX y
        results.Add(elementFull (int ElementType.FBMode) c FB_Param "")

        
        y + 1, results

    let drawX ((func:string), (args:seq<string>), x, y) =
        let results = ResizeArray<string>()
        let mutable fX = x
        let mutable fY = y
        let fbCellX = fbCellX x

        let fbXmlHead = sprintf "Param=\"FNAME: %s&#xA;TYPE: function&#xA;INSTANCE: USER_FB,VAR&#xA;INDEX:
                                121&#xA;COL_PROP: 1&#xA;SAFETY: 0&#xA;VAR_IN: EN, 0x00200001, , 0&#xA;" func
        let fbXmlBody = seq {
                                for n in [1.. (args.length())] do
                                    yield sprintf "VAR_IN: IN%d, 0x00207fe0, , 0" n
                        }|> String.concat "&#xA;" 
        let fbXmlTail = "VAR_OUT: ENO, 0x00000001,&#xA;VAR_OUT: OUT, 0x00007fe0, &#xA;\""
        let newArgs = getNewArgs args

        InsertInArgs (newArgs|> Seq.toList, fbCellX, y ) |> results.AddRange
        mutiEndLine fX fbCellX fY |> results.Add
        let c = coord (fbCellX + 1) y
        let FB_Param = sprintf "%s%s%s" fbXmlHead  fbXmlBody  fbXmlTail
        results.Add(elementFull (int ElementType.VertFBMode) c FB_Param "")
        fY + args.length() + 3, results

    

    let drawFB ((func:string), (args:seq<string>), x, y) =
        let results = ResizeArray<string>()
        let usePulse = func.Contains(";P")
        let func = func.Split(';').[0] 
        let org = func.Split(':').[0]
        let dataType = if(func.Split(':').Length > 1) then func.Split(':').[1] else ""
        let fbCellX = fbCellX x
        let newArgs = getNewArgs args
        

        let findName, fbName, newArgs =
            match org with
            | RegexPattern @"(^BCD_TO_\*\*\*$)" [newCmp] -> (sprintf "%s_BCD_TO_INT" dataType), newCmp, newDoubleArgs(dataType, newArgs)
            | RegexPattern @"(^\*\*\*_TO_BCD$)" [newCmp] -> if(func.Split(':').[1] = "DINT") 
                                                              then "DINT_TO_BCD_DWORD"        , newCmp, newDoubleArgs(dataType, newArgs)
                                                              else "INT_TO_BCD_WORD"          , newCmp, newDoubleArgs(dataType, newArgs)
            | RegexPattern @"(^LIFO_\*\*\*$)" [newCmp] -> (sprintf "LIFO_%s" dataType)        , newCmp, newDoubleArgs(dataType, newArgs)
            | RegexPattern @"(^FIFO_\*\*\*$)" [newCmp] -> (sprintf "FIFO_%s" dataType)        , newCmp, newDoubleArgs(dataType, newArgs)
              
            | RegexPattern @"(^MOVE$)" [newCmp] -> (sprintf "%s_%s" newCmp  dataType) , newCmp, newDoubleArgs(dataType, newArgs)
            | RegexPattern @"(^INC$)"  [newCmp] -> (sprintf "%s_%s" newCmp  dataType) , newCmp, newDoubleArgs(dataType, newArgs)
            | RegexPattern @"(^DEC$)"  [newCmp] -> (sprintf "%s_%s" newCmp  dataType) , newCmp, newDoubleArgs(dataType, newArgs)
                                                
            | RegexPattern @"(^AND$)"  [newCmp]
            | RegexPattern @"(^OR)"    [newCmp]
            | RegexPattern @"(^XOR$)"  [newCmp]
            | RegexPattern @"(^XNR$)"  [newCmp] 

           

            | RegexPattern @"(^MUL$)"  [newCmp] 
            | RegexPattern @"(^ADD$)"  [newCmp] -> (sprintf "%s2_%s" newCmp dataType) , newCmp, if(newArgs.length() = 2) then newDoubleArgs(dataType, getAddLast newArgs) else newDoubleArgs(dataType, newArgs)


            | RegexPattern @"(^DECO$)" [newCmp] 
            | RegexPattern @"(^ENCO$)" [newCmp] 
            | RegexPattern @"(^GET)" [newCmp] 
            | RegexPattern @"(^PUT$)" [newCmp] 

            | RegexPattern @"(^DIV$)"  [newCmp] 
            | RegexPattern @"(^SUB$)"  [newCmp] -> (sprintf "%s_%s" newCmp  dataType) , newCmp, if(newArgs.length() = 2) then newDoubleArgs(dataType, getAddLast newArgs) else newDoubleArgs(dataType, newArgs)
            | _ ->    org, org, newArgs

        let mutable fX = x
        let mutable fY = y


        if(usePulse)
              then
                  drawPulse (fX, fY, true) |> results.AddRange
              else
                  mutiEndLine fX fbCellX fY |> results.Add

        //mutiEndLine fX (if(usePulse) then (fbCellX-3) else fbCellX) fY |> results.Add
      
        //if(usePulse) then
        //    drawPulse (fX, fY) |> results.AddRange

        let instance = 
            match fbName with
                     | "FF" ->
                                let instName = InstFun.getInst(insts, FF)
                                insts.Add(FF, instName)
                                sprintf "%s,VAR" instName
                     |_-> ","

        let c = coord (fbCellX + 1) y
        let FB_Param = sprintf "Param=\"%s\"" ((getFBXML(findName, fbName ,instance)))
        results.Add(elementFull (int ElementType.VertFBMode) c FB_Param "")
        
        let inCount, allCount = getFBInCount(findName)
        let mutable retY = fY + allCount + 2

        match fbName with
        | "TIME_DE" -> retY <- retY + 4;
        | "SWAP" 
        | "INC" 
        | "DEC" ->   InsertInArgs  ((newArgs |> Seq.toList), fbCellX, y) |> results.AddRange;
                     InsertOutArgs ((newArgs |> Seq.toList), fbCellX, y) |> results.AddRange;
        | "FF"  ->   InsertOutArgs ((newArgs |> Seq.toList), fbCellX, y-1) |> results.AddRange; retY <- fY + 2
        | "MCS" ->  InsertInArgs  ((newArgs |> Seq.take(1) |> Seq.map (fun f -> f.TrimStart('N')) |> Seq.toList), fbCellX, y)   |> results.AddRange
                    InsertOutArgs ((newArgs |> Seq.skip(1) |> Seq.toList), fbCellX, y-1) |> results.AddRange
        | "MCSCLR" -> 
                    InsertInArgs  ((newArgs |> Seq.map (fun f -> f.TrimStart('N')) |> Seq.toList), fbCellX, y)   |> results.AddRange
        |_->
            if(inCount <= args.length())
            then    
                let inItems = newArgs|> Seq.take(inCount) |> Seq.toList
                InsertInArgs (inItems, fbCellX, y ) |> results.AddRange
                let outItems = newArgs|> Seq.skip(inCount) |> Seq.toList
                InsertOutArgs (outItems, fbCellX, y ) |> results.AddRange

        retY, results

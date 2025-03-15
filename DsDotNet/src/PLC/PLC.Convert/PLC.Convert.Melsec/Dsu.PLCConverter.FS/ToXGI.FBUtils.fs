namespace Dsu.PLCConverter.FS

open Dsu.PLCConverter.FS.XgiSpecs.Config.POU.Program.LDRoutine
open System.Linq
open XgiBaseXML
open Dsu.PLCConverter.FS.XgiSpecs

[<AutoOpen>]
/// XGI로 FB 변환을 위한 기본 구조
module XgiFBUtils =


    let [<Literal>] MIXFB = "MIXFB"

    let drawFBTime ((func:string), (args:seq<string>), highTmr:bool, x, y) =
        let results = ResizeArray<string>()
        let fbCellX = fbCellX x
        let in1 = 1
        let in2 = 2

        match func with
        | "TMR"
        | "TON_UINT" ->
            mutiEndLine (x) (fbCellX - 1) y |> results.Add
            let c = coord (fbCellX) y
            let instance = args |> Seq.head
            let timeArgs = getNewArgs args func
            let time = timeArgs |> Seq.last
            let ton_unit = if highTmr then sprintf "%d" XgiOption.Config.TimerHighSpeed else sprintf "%d" XgiOption.Config.TimerLowSpeed

            let FB_Param = sprintf "Param=\"%s\"" (getFBXML(func, func, (instance + ",VAR"), getFBIndex func))
            results.Add(elementFull (int ElementType.VertFBMode) c FB_Param instance)

            let c = coord (fbCellX - 1) (y + in1)
            results.Add(elementFull (int ElementType.VariableMode) c "" time)

            if func = "TON_UINT" then
                let c = coord (fbCellX - 1) (y + in2)
                results.Add(elementFull (int ElementType.VariableMode) c "" ton_unit)

        | _ ->
            errorHistory.Add(sprintf "Unknown cmd [%s]" func)
            convertOk <- false

        y + 4, results

    let drawFBCount ((func:string), (args:seq<string>), x, y) =
        let results = ResizeArray<string>()
        let fbCellX = fbCellX x
        let in1 = 1
        let in2 = 2

        mutiEndLine (x) (fbCellX - 1) y |> results.Add
        let c = coord (fbCellX) y
        let instance = args |> Seq.head
        let cntArgs = getNewArgs args func
        let count = cntArgs |> Seq.last

        let FB_Param = sprintf "Param=\"%s\"" (getFBXML(func, func, (instance + ",VAR"), getFBIndex func))
        results.Add(elementFull (int ElementType.VertFBMode) c FB_Param instance)

        let c = coord (fbCellX - 1) (y + in1)
        results.Add(elementFull (int ElementType.VariableMode) c "" "false")

        let c = coord (fbCellX - 1) (y + in2)
        results.Add(elementFull (int ElementType.VariableMode) c "" count)

        y + 4, results

    let drawPulse (x, y, useFB:bool) =
        let results = ResizeArray<string>()
        let fbCellX = fbCellX x
        let c = coord (fbCellX - 2) y
        let instName = InstFun.getInst(insts, R_TRIG)
        insts.Add(R_TRIG, instName)

        let FB_Param = sprintf "Param=\"%s\"" (getFBXML(R_TRIG.ToText, R_TRIG.ToText, sprintf "%s,VAR" instName, getFBIndex R_TRIG.ToText))
        results.Add(elementFull (int ElementType.VertFBMode) c FB_Param "")
        mutiEndLine (x) (fbCellX - 3) y |> results.Add
        mutiEndLine (fbCellX - 1) (if useFB then fbCellX else coilCellX) y |> results.Add
        results

    let drawExCMD ((func:string), (args:seq<string>), x, y) =
        let results = ResizeArray<string>()
        let usePulse = func.Contains(";P")
        let func = func.Split(';').[0]
        let fbCellX = fbCellX x
        let mutable fX = x
        let mutable fY = y

        if usePulse then
            drawRising (fX, fY) |> results.AddRange
            mutiEndLine (fbCellX + 1) (coilCellX - 1) fY |> results.Add
        else
            mutiEndLine fX coilCellX fY |> results.Add

        let FB_Param =
            if args.length() > 0 then
                if not (ListExCMDHasPara.Contains(func)) && args.length() > 0 then
                    sprintf "변환시 자동 삭제 항목 : %s" (args |> String.concat " ") |> warningHistory.Add
                    sprintf "Param=\"%s\"" func
                else
                    let para = getNewArgs args func|> Seq.head
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

        let fbXmlHead = sprintf "Param=\"FNAME: %s&#xA;TYPE: function&#xA;INSTANCE: USER_FB,VAR&#xA;INDEX:0&#xA;COL_PROP: 1&#xA;SAFETY: 0&#xA;VAR_IN: EN, 0x00200001, , 0&#xA;" func
        let fbXmlBody = seq {
                            for n in [1.. (args.length())] do
                                yield sprintf "VAR_IN: IN%d, 0x00207fe0, , 0" n
                        } |> String.concat "&#xA;"
        let fbXmlTail = "VAR_OUT: ENO, 0x00000001,&#xA;VAR_OUT: OUT, 0x00007fe0, &#xA;\""
        let newArgs = getNewArgs args func

        InArgs (newArgs |> Seq.toList, fbCellX, y) |> results.AddRange
        mutiEndLine fX fbCellX fY |> results.Add
        let c = coord (fbCellX + 1) y
        let FB_Param = sprintf "%s%s%s" fbXmlHead fbXmlBody fbXmlTail
        results.Add(elementFull (int ElementType.VertFBMode) c FB_Param "")
        fY + args.length() + 3, results


    let getInstance fbName = 
        match fbName with
        | "FF" ->
            let instName = InstFun.getInst(insts, FF)
            insts.Add(FF, instName)
            sprintf "%s,VAR" instName
        | _ -> ","

    let isZRDevice (address:string) = 
        if address.StartsWith("ZR") then
            true
        else
            false   
            
    let isZDevice (address:string) = 
        if not(isZRDevice(address)) && address.StartsWith("Z") then
            true
        else
            false  
            
namespace Dsu.PLCConverter.FS

open Dual.Common
open Dsu.PLCConverter.FS.XgiSpecs.Config.POU.Program.LDRoutine
open Dsu.PLCConverter.FS.XgiSpecs
open ActivePattern


[<AutoOpen>]
///XgiBasic XGI로 프로그램 변환을 위한 기본  구조
module XgiBasic =
    let getContactMode (opOpt:OpOpt) =
        match  opOpt with
        | OpOpt.Normal -> ElementType.ContactMode
        | OpOpt.Neg -> ElementType.ClosedContactMode
        | OpOpt.Rising -> ElementType.PulseContactMode
        | OpOpt.Falling -> ElementType.NPulseContactMode
        | OpOpt.Inverter -> ElementType.InverterMode
        | OpOpt.Compare -> ElementType.VertFBMode
        | OpOpt.RisingEdge -> ElementType.VertFBMode
        | OpOpt.FallingEdge -> ElementType.VertFBMode
        
    let getCoilMode (cmd:string) =
        match  cmd with
        | "OUT" ->      ElementType.CoilMode
        | "OUTP" ->     ElementType.PulseCoilMode
        | "OUTN" ->     ElementType.NPulseCoilMode
        | "OUT NOT" ->  ElementType.ClosedCoilMode
        | "SET" ->      ElementType.SetCoilMode
        | "RST" ->      ElementType.ResetCoilMode
        | _ -> failwithlogf "Unknown cmd [%s]" cmd

    let mappingCSV =   XgiBaseXML.XgiOpt.PathCommandMapping // __SOURCE_DIRECTORY__ + @"/../../Data/CommandMapping.csv"
    let mappingTable  = MappingTableParser.createDictionary mappingCSV
    let mutable convertOk = true
    let errorHistory =  ResizeArray<string>()
    let warningHistory =  ResizeArray<string>()
    let insts =  ResizeArray<Inst>()

    let xgiLineConvertor line =
        let args = printArgumentOfLine line
        let cmdMelec = line.Instruction

        if(cmdMelec = "") 
        then 
            line, "RungComment", line.LineStatement
        else if(mappingTable.ContainsKey(cmdMelec)) 
        then
            let il = mappingTable.[cmdMelec].Xgi
            if(il = "X" && cmdMelec.EndsWith("P") && cmdMelec <> "MEP" && cmdMelec <> "EGP" && cmdMelec <> "JMP") //해당 XGI 대응 함수가 없으면 P를 제거하고 다시 검색
            then
                line, mappingTable.[cmdMelec.[0..cmdMelec.length() - 2]].Xgi + ";P", args
            else 
                match line.Instruction with
                |"EGP" ->  line, "EGP", args
                |"MEP" ->  line, "MEP", args
                |"EGF" ->  line, "EGF", args
                |"MEF" ->  line, "MEF", args
                |"FEND" ->  line, "FEND", args
                |"JMP" ->  line, "JMP", args
                |_-> line, il, args
                
        else
            assert(true) //사전에 없는 CMD 발견
            line, line.Instruction, args

    /// 좌표 반환 : 1, 4, 7, 11, ...
    /// 논리 좌표 x y 를 LS 산전 XGI 수치 좌표계로 반환
    let fbtxtfile = __SOURCE_DIRECTORY__ + @"\..\..\Data\IEC XGI FB\XG5000_IEC_FUNC.txt"
    let coord x y = x*3 + y*1024 + 1
    let elementFull type' coordi param tag = sprintf "\t\t<Element ElementType=\"%d\" Coordinate=\"%d\" %s>%s</Element>" type' coordi param tag
    //let xmlConvertor = createReader fbtxtfile
    //let dicIEC = createReader fbtxtfile
    let vline c = elementFull (int ElementType.VertLineMode) (c + 2) "" "" /// 좌표 c 에서 시작하는 수직 line
    let hline c = elementFull (int ElementType.HorzLineMode) (c) "" "" /// 좌표 c 에서 시작하는 수평 line
    let coilCellX = 31      // 산전 limit : 가로로 
    let minFBCellX = 9         // 산전 FB 위치 : 가로로 
    let fbCellX x:int = if(minFBCellX <= x+3) then (x+4) else minFBCellX
    let AutoType = [|"T"; "C"; R_TRIG.ToText;F_TRIG.ToText; "TON_UINT"|] |> System.Collections.Generic.HashSet
    
    let vlineDownTo x y n =
            seq {
                if x >= 0 then
                    for n in [0.. n-1] do
                        let c = coord x (y + n)
                        yield vline c
            }

    let hlineRightTo x y n =
        seq {
            if x >= 0 then
                for n in [0.. n-1] do
                    let c = coord (x + n) (y)
                    yield hline c
        }

    let mutiEndLine startX endX y =
        if endX > startX 
        then
            let lengthParam = sprintf "Param=\"%d\"" (3 * (endX-startX))
            let c = coord startX y
            elementFull (int ElementType.MultiHorzLineMode) c lengthParam ""
        else failwithlogf "endX startX [%d > %d]" endX startX

    let getNewArgs (args:seq<string>) = 
        let newAs = args
                    |> Seq.map (fun arg ->
                                    if(not (arg.StartsWith("%")) && arg.StartsWith("K") && arg.length() > 2 && XGI.IsAddress(arg.Substring(2, (arg.length()-2)))) then arg  //K4Y03F
                                    else 
                                        if(arg.StartsWith("K")) then sprintf "%d" (System.Convert.ToInt32(arg.TrimStart('K'), 10)) else 
                                        if(arg.StartsWith("H")) then sprintf "%d" (System.Convert.ToInt32(arg.TrimStart('H'), 16)) else
                                        if(arg.StartsWith("E")) then sprintf "%s" (arg.TrimStart('E')) else
                                        if(arg.StartsWith("T")) then arg + ".Q" else
                                        if(arg = "NOT" || arg = "MEP" || arg = "MEF") then ""
                                        else arg.Replace('"', ''')
                                    )   
        newAs


    //마지막 항목을 마지막에 추가한다 S1, S2  => S1, S2, S2
    let getAddLast (args:seq<string>) =    
        let extentArg = (args |> Seq.skip(args.length() - 1)) |> Seq.append args
        extentArg

    let getDoubleSizeArgs (args:seq<string>) = 
        let newAs = args
                    |> Seq.map (fun arg ->
                                        if(arg.StartsWith("%M") || arg.StartsWith("%R") || arg.StartsWith("%W")) 
                                        then arg.Replace("%MW", "%MD").Replace("%RW", "%RD").Replace("%WW", "%WD")
                                        else arg
                                    )
        newAs

        
    let newDoubleArgs ((dataType:string),(args:seq<string>))= 
        let temp = getNewArgs (args)
        match dataType with
        | "DWORD" 
        | "REAL" 
        | "DINT" -> getDoubleSizeArgs (temp)
        | _ -> temp

    
    let InsertInArgs ((items:List<string>), x, y) = 
        let results = ResizeArray<string>()
        for n in [1.. items.length()] do
            let c = coord (x) (y + n)
            results.Add(elementFull (int ElementType.VariableMode) c "" (items.[n-1]))
        results

    let InsertOutArgs ((items:List<string>), x, y) = 
        let results = ResizeArray<string>()
        for n in [1.. items.length()] do
            let c = coord (x + 2) (y + n)
            results.Add(elementFull (int ElementType.VariableMode) c "" (items.[n-1]))
        results

    let addPointX opComp =
            match opComp with
            |OpOpt.Compare -> 3
            |OpOpt.FallingEdge -> 3
            |OpOpt.RisingEdge -> 3
            |_ -> 1

    let addPointY opComp =
            match opComp with
            |OpOpt.Compare -> 4
            |OpOpt.FallingEdge -> 2
            |OpOpt.RisingEdge -> 2
            |_ -> 1

    
    let drawPoint (basePoint:LoadPoint) (runPoint:LoadPoint) (opOpt:OpOpt)  = 
        basePoint.SX, basePoint.SY, runPoint.EX, runPoint.EY, addPointX opOpt, addPointY opOpt

    let getNewPoint(loadPoint:LoadPoint, op, opOpt:OpOpt) = 
        let newPoint = loadPoint.Copy()
        match op with
        | Load ->
                newPoint.EX <- loadPoint.EX + addPointX opOpt
                newPoint.EY <- loadPoint.EY + addPointY opOpt
        | And  ->  
                let addY = loadPoint.SY + if((addPointY opOpt) - (loadPoint.SizeY) > 0) then addPointY opOpt else 0
                newPoint.EX <- loadPoint.EX + addPointX opOpt
                newPoint.EY <- max addY loadPoint.EY
        | Or ->  
                let addX = loadPoint.SX + if((addPointX opOpt) - (loadPoint.SizeX) > 0) then addPointX opOpt else 0
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
            | _ -> failwithlogf "Unknown loadOp [%s]" loadop.ToText
                     
        | Empty -> LoadPoint(0,0,0,0)
    
    let addBlankLine (leftP:LoadPoint, rightP:LoadPoint) =
        let results = ResizeArray<string>()
        //--------------
        //-------[ ... ]* <-vertex
        let vertexStartX = min leftP.EX rightP.EX    
        let vertexEndX =   max leftP.EX rightP.EX    

        let vertexStartY = if(leftP.EX > rightP.EX) then rightP.SY else leftP.SY
        let vertexEndY =   max leftP.EY rightP.EY    

        let countY = rightP.SY - leftP.SY
        let countX = vertexEndX - vertexStartX

        //vertical line
        // 좌측 vertical lines
        vlineDownTo (rightP.SX - 1) leftP.SY countY |> results.AddRange
        // 우측 vertical lines
        vlineDownTo (vertexEndX- 1) leftP.SY countY |> results.AddRange

        //horzLine line
        if(leftP.EX <> rightP.EX) then 
            hlineRightTo vertexStartX vertexStartY countX |> results.AddRange

        results

    let getCoord (str) =  
               match str with
               | ActivePattern.RegexPattern @"Coordinate=""([0-9]+)" [coord] -> coord |> int
               |_ -> failwithlogf "getCoord don't have coord [%s]" str


    


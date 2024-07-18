// Copyright (c) Dualsoft  All Rights Reserved.
namespace Engine.Import.Office

open System
open System.Linq
open Dual.Common.Core.FS
open Engine.Core
open System.IO
open System.Text
open System.Data
open System.Runtime.CompilerServices
open Engine.CodeGenCPU


[<AutoOpen>]
module ExportIOTable =


    let addRows rows (dt:DataTable) = 
        rows
        |> Seq.iter (fun row ->
            let rowTemp = dt.NewRow()
            rowTemp.ItemArray <- (row |> Seq.cast<obj> |> Seq.toArray)
            dt.Rows.Add(rowTemp) |> ignore)

    let emptyRow (cols:string array) (dt:DataTable) =
        let row = dt.NewRow()
        row.ItemArray <- cols.Select(fun f -> "" |> box).ToArray()
        row |> dt.Rows.Add |> ignore

 
    let  addIOColumn(dt:DataTable) =

        dt.Columns.Add($"{IOColumn.Case}", typeof<string>) |> ignore
        dt.Columns.Add($"{IOColumn.Flow}", typeof<string>) |> ignore
        dt.Columns.Add($"{IOColumn.Name}", typeof<string>) |> ignore
        dt.Columns.Add($"{IOColumn.DataType}", typeof<string>) |> ignore
        dt.Columns[dt.Columns.Count-1].ColumnName <- "DataType \n(In:Out)"
        dt.Columns.Add($"{IOColumn.Input}", typeof<string>) |> ignore
        dt.Columns.Add($"{IOColumn.Output}", typeof<string>) |> ignore
        dt.Columns.Add($"{IOColumn.InSymbol}", typeof<string>)  |> ignore
        dt.Columns[dt.Columns.Count-1].ColumnName <- "Symbol\nIn"
        dt.Columns.Add($"{IOColumn.OutSymbol}", typeof<string>)  |> ignore
        dt.Columns[dt.Columns.Count-1].ColumnName <- "Symbol\nOut"

    let emptyLine (dt:DataTable) = emptyRow (Enum.GetNames(typedefof<IOColumn>)) dt

    let toTextPPTFunc (x:DevParam) =
        match x.DevValue, x.DevTime with 
        | Some(v), Some(t) -> $"{v}:{t}ms"
        | Some(v), None    -> $"{v}"
        | None, Some(v)       -> $"{v}ms"
        | None, None          -> $""

    let ToPanelIOTable(sys: DsSystem) (selectFlows:Flow seq) (containSys:bool) target : DataTable =

        let dt = new System.Data.DataTable($"{sys.Name} Panel IO LIST")
        addIOColumn dt

        let toBtnText (btns: ButtonDef seq, xlsCase: ExcelCase) =
            for btn in btns do
                if containSys then
                    let inSym =  toTextPPTFunc btn.InParam 
                    let outSym =  toTextPPTFunc btn.OutParam
                    updateHwAddress (btn) (btn.InAddress, btn.OutAddress) Util.runtimeTarget
                    let dType = getPPTHwDevDataTypeText btn
                    
                    dt.Rows.Add(xlsCase.ToText(), "ALL", btn.Name, dType,  btn.InAddress, btn.OutAddress ,inSym, outSym)
                    |> ignore

        let toLampText (lamps: LampDef seq, xlsCase: ExcelCase) =
            for lamp in lamps do
                if containSys then
                    let inSym =  toTextPPTFunc lamp.InParam 
                    let outSym =  toTextPPTFunc lamp.OutParam
                    updateHwAddress (lamp) (lamp.InAddress, lamp.OutAddress) Util.runtimeTarget
                    let dType = getPPTHwDevDataTypeText lamp
                    dt.Rows.Add(xlsCase.ToText(), "ALL", lamp.Name, dType,  lamp.InAddress, lamp.OutAddress ,inSym, outSym)
                    |> ignore


        toBtnText (sys.AutoHWButtons, ExcelCase.XlsAutoBTN)
        toBtnText (sys.ManualHWButtons, ExcelCase.XlsManualBTN)
        toBtnText (sys.DriveHWButtons, ExcelCase.XlsDriveBTN)
        toBtnText (sys.PauseHWButtons, ExcelCase.XlsPauseBTN)
        toBtnText (sys.ClearHWButtons, ExcelCase.XlsClearBTN)
        toBtnText (sys.EmergencyHWButtons, ExcelCase.XlsEmergencyBTN)
        toBtnText (sys.HomeHWButtons, ExcelCase.XlsHomeBTN)
        toBtnText (sys.TestHWButtons, ExcelCase.XlsTestBTN)
        toBtnText (sys.ReadyHWButtons, ExcelCase.XlsReadyBTN)
        toLampText (sys.AutoHWLamps, ExcelCase.XlsAutoLamp)
        toLampText (sys.ManualHWLamps, ExcelCase.XlsManualLamp)
        toLampText (sys.IdleHWLamps, ExcelCase.XlsIdleLamp)
        toLampText (sys.ErrorHWLamps, ExcelCase.XlsErrorLamp)
        toLampText (sys.OriginHWLamps, ExcelCase.XlsHomingLamp)
        toLampText (sys.ReadyHWLamps, ExcelCase.XlsReadyLamp)
        toLampText (sys.DriveHWLamps, ExcelCase.XlsDriveLamp)
        toLampText (sys.TestHWLamps, ExcelCase.XlsTestLamp)


        dt

    let splitNameForRow(name:string) = 
        let head = name.Split(TextDeviceSplit)[0];   
        let tail = name[(head.Length+TextDeviceSplit.length())..]
        head, tail 

    let rowIOItems (dev: TaskDev, job: Job) target =
        let inSym  =  dev.GetInParam(job).Name
        let outSym =  dev.GetOutParam(job).Name
        let inSkip, outSkip = dev.GetSkipInfo(job)

        let flow, name = splitNameForRow $"{dev.DeviceName}.{dev.ApiItem.Name}"
        [   
            TextXlsAddress
            flow
            name
            getPPTTDevDataTypeText (dev)
            getValidAddress(dev.InAddress,  dev.InDataType,  dev.QualifiedName, inSkip,  IOType.In,  target )
            getValidAddress(dev.OutAddress, dev.OutDataType, dev.QualifiedName, outSkip, IOType.Out, target )
            inSym
            outSym
        ]

    let IOchunkBySize = 22

    let ToDeviceIOTables  (sys: DsSystem) (selectFlows:Flow seq) (containSys:bool) target : DataTable seq =
        
        let totalRows =
            seq {

          

                let mutable extCnt = 0
                let devsCall =  sys.GetDevicesSkipEmptyAddress()

                for (dev, call) in  devsCall do
                    //외부입력 전용 확인하여 출력 생성하지 않는다.
                    if  dev.IsRootOnlyDevice
                    then
                        dev.OutAddress <- (TextSkip)
                        if dev.InAddress = TextAddrEmpty
                        then
                            dev.InAddress  <-  getExternalTempMemory (target, extCnt)
                            extCnt <- extCnt+1

                    yield rowIOItems (dev, call.TargetJob) target
        }

        let dts = 
            totalRows 
            |> Seq.chunkBySize(IOchunkBySize)
            |> Seq.mapi(fun i rows->
                let dt = new System.Data.DataTable($"{sys.Name} Device IO LIST {i+1}")
                addIOColumn dt 
                addRows rows dt
                dt
            )

        dts

    type Statement with
        member x.ToConditionText() =
            match x with
            | DuAssign (_condition, expr, _) -> $"{expr.ToText()}" 
            | DuVarDecl (expr, _) -> $"{expr.ToText()}"
            | DuTimer timerStatement ->
                let ts, t = timerStatement, timerStatement.Timer
                let functionName = ts.FunctionName  // e.g "createTON"
                let args = [    // [preset; rung-in-condition; (reset-condition)]
                    sprintf "%A" t.PRE.Value
                    match ts.RungInCondition with | Some c -> c.ToText() | None -> ()
                    match ts.ResetCondition  with | Some c -> c.ToText() | None -> () ]
                let args = String.Join(", ", args)
                $"{functionName}({args})"

            | DuCounter counterStatement ->
                let cs, c = counterStatement, counterStatement.Counter
                let functionName = cs.FunctionName  // e.g "createCTU"
                let args = [    // [preset; up-condition; (down-condition;) (reset-condition;) (accum;)]
                    sprintf "%A" c.PRE.Value
                    match cs.UpCondition    with | Some c -> c.ToText() | None -> ()
                    match cs.DownCondition  with | Some c -> c.ToText() | None -> ()
                    match cs.ResetCondition with | Some c -> c.ToText() | None -> ()
                    if c.ACC.Value <> 0u then
                        sprintf "%A" c.ACC.Value ]
                let args = String.Join(", ", args)
                $"{functionName}({args})"
            | DuAction (DuCopy (condition, _, _)) -> $"{condition.ToText()}"
            | DuAction (DuCopyUdt {Condition=condition}) -> $"{condition.ToText()}"
            | DuPLCFunction _ ->
                failwithlog "ERROR"
            | (DuUdtDecl _ | DuUdtDef _) -> failwith "Unsupported.  Should not be called for these statements"
            | (DuLambdaDecl _ | DuProcDecl _ | DuProcCall _) ->
                failwith "ERROR: Not yet implemented"       // 추후 subroutine 사용시, 필요에 따라 세부 구현

    let ToFuncVariTables  (sys: DsSystem) (selectFlows:Flow seq) (containSys:bool) target: DataTable seq =

        let getConditionDefListRows (conds: ConditionDef seq) =
            conds |> Seq.map(fun cond ->
            
                updateHwAddress (cond) (cond.InAddress, cond.OutAddress) Util.runtimeTarget
                [
                    ExcelCase.XlsConditionReady.ToText()
                    ""
                    cond.Name
                    getPPTHwDevDataTypeText cond
                    cond.InAddress
                    cond.OutAddress
                    cond.InParam.Name 
                    cond.OutParam.Name 
                ]
            )
        
        let funcText (xs:Statement seq)=    String.Join(";", xs.Select(fun s->s.ToText()))
        let funcOperatorText (xs:Statement seq)=    String.Join(";", xs.Select(fun s->s.ToConditionText()))
        
        let operatorRows =
            
            sys.Functions
                .OfType<OperatorFunction>()
                                    .Map(fun func->
                                    let flow, name = splitNameForRow func.Name
                                    let funcText =    funcOperatorText func.Statements


                                    [ TextXlsOperator
                                      flow
                                      name
                                      TextSkip
                                      funcText
                                      TextSkip
                                      TextSkip
                                      TextSkip
                                      ]
                                      )   

        let commandRows =
            sys.Functions
                .OfType<CommandFunction>()
                                    .Map(fun func->
                                    let flow, name = splitNameForRow func.Name
                                    let funcText =    funcText func.Statements
         

                                    [ TextXlsCommand
                                      flow
                                      name
                                      TextSkip
                                      TextSkip
                                      funcText
                                      TextSkip
                                      TextSkip
                                      ]
                                      )

        let variRows = sys.Variables.Map(fun vari->
                [ 
                  TextXlsVariable
                  TextXlsAllFlow
                  vari.Name
                  vari.Type.ToText()
                  if vari.VariableType = Mutable then  TextSkip else vari.InitValue 
                  TextSkip
                  TextSkip
                  TextSkip
                  ]
                  )
                  

        let sampleOperatorRows =  if operatorRows.any() then [] else  [[TextXlsOperator;"-";"";"-";"";"-";"-";"-"]]
        let sampleCommandRows =  if commandRows.any() then [] else  [[TextXlsCommand;"-";"";"-";"-";"";"-";"-"]]
        let sampleConstRows=  if variRows.any() then [] else  [[TextXlsConst;"-";"";"";"";"-";"-";"-"]]
        let sampleVariRows  =  if variRows.any() then [] else  [[TextXlsVariable;"-";"";"";"-";"-";"-";"-"]]
        let dts = 
            getConditionDefListRows (sys.ReadyConditions)  
            @ commandRows 
            @ operatorRows
            @ variRows
            @ sampleOperatorRows
            @ sampleCommandRows
            @ sampleVariRows
            @ sampleConstRows
            |> Seq.chunkBySize(IOchunkBySize)
            |> Seq.map(fun rows->
                let dt = new System.Data.DataTable($"{sys.Name} 외부신호 IO LIST")
                addIOColumn dt
                addRows rows dt
                dt
            )
     
        dts

    let getErrorRows(sys:DsSystem) =
        
        let mutable no = 0
        let rowItems (name:string, address :string) =
            no <- no+1
            [ 
              $"Alarm{no}"
              name
              address
               ]

        let rows =
            let calls = sys.GetAlarmCalls()
                        
            seq {
                //1. call, real 부터
                for call in calls |> Seq.sortBy (fun c -> c.Name) do
                    yield rowItems ($"{call.Name}_센서쇼트이상", call.ErrorSensorOn.Address)
                    yield rowItems ($"{call.Name}_센서단락이상", call.ErrorSensorOff.Address)
                    yield rowItems ($"{call.Name}_감지시간초과이상", call.ErrorOnTimeOver.Address)
                    yield rowItems ($"{call.Name}_감지시간부족이상", call.ErrorOnTimeShortage.Address)
                    yield rowItems ($"{call.Name}_해지시간초과이상", call.ErrorOffTimeOver.Address)
                    yield rowItems ($"{call.Name}_해지시간부족이상", call.ErrorOffTimeShortage.Address)

                for real in sys.GetRealVertices() |> Seq.sortBy (fun r -> r.Name) do
                    yield rowItems ($"{real.Name}_작업원위치이상", real.ErrGoingOrigin.Address)

                //2. emg step
                for emg in sys.HWButtons.Where(fun f-> f.ButtonType = DuEmergencyBTN) do
                    yield rowItems ($"{emg.Name}_버튼눌림", emg.ErrorEmergency.Address)

                //3 . HWConditions step
                for condi in sys.HWConditions do
                    yield rowItems ($"{condi.Name}_조건이상", condi.ErrorCondition.Address)
            }
        rows

    let ToErrorTable (sys: DsSystem)  : DataTable =
        let dt = new System.Data.DataTable($"AlarmTable")
        dt.Columns.Add($"{ErrorColumn.No}", typeof<string>) |> ignore
        dt.Columns.Add($"{ErrorColumn.Name}", typeof<string>) |> ignore
        dt.Columns.Add($"{ErrorColumn.ErrorAddress}", typeof<string>) |> ignore

        let rows = getErrorRows(sys)

        addRows rows dt

        let emptyLine () = emptyRow (Enum.GetNames(typedefof<ErrorColumn>)) dt
     
        emptyLine ()
        dt

    let ToAlarmTable (sys: DsSystem)  : DataTable =

        let dt = new System.Data.DataTable($"알람 리스트")
        dt.Columns.Add($"{TextColumn.Name}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.Empty1}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.Empty2}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.Empty3}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.Color}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.Ltalic}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.UnderLine}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.StrikeOut}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.Bold}", typeof<string>) |> ignore
      
        let rowItems (name: string) =
            [ 
              name
              ""
              ""
              ""
              "0"
              "Off"
              "Off"
              "Off"
              "On"
               ]


        let alarmList = getErrorRows(sys)
        let rows= 
            alarmList
            |> Seq.map (fun err ->
                                rowItems (err[ErrorColumn.Name|>int]) 
                        )

        addRows rows dt
        let emptyLine () = emptyRow (Enum.GetNames(typedefof<TextColumn>)) dt
     
        emptyLine ()
        dt


        

    let getLabelTable(name:string) = 
        let dt=  new System.Data.DataTable($"{name}")
        dt.Columns.Add($"{TextColumn.Name}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.Empty1}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.Empty2}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.Empty3}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.Color}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.Ltalic}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.UnderLine}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.StrikeOut}", typeof<string>) |> ignore
        dt.Columns.Add($"{TextColumn.Bold}", typeof<string>) |> ignore
        dt

    let rowDeviceItems (dev: string) (hasSafety:bool)=
            [ 
              dev
              ""
              ""
              ""
              if hasSafety then "8388736" else "0"
              "Off"
              "Off"
              "Off"
              "On"
               ]

    let ToDevicesApiTable (sys: DsSystem)  : DataTable =
        let dt = getLabelTable "액션이름"

        let rows =
            let devs =  sys.GetDevicesForHMI()
            devs.Select(fun (dev, _)-> 
                let text = 
                    if dev.MaunualAddress = TextSkip then
                        ""//"·" 
                    else
                        "□" 

                rowDeviceItems text false

                )

        addRows rows dt
        let emptyLine () = emptyRow (Enum.GetNames(typedefof<TextColumn>)) dt
     
        emptyLine ()
        dt


    let ToFlowNamesTable (sys: DsSystem)  : DataTable =

        let dt = getLabelTable "Flow이름"
      
        let rows =
            sys.GetFlowsOrderByName()
                .Select(fun flow -> rowDeviceItems flow.Name false)

        addRows rows dt
        let emptyLine () = emptyRow (Enum.GetNames(typedefof<TextColumn>)) dt
     
        emptyLine ()
        dt


    let ToWorkNamesTable (sys: DsSystem)  : DataTable =

        let dt = getLabelTable "Work이름"
      
        let rows =
                  sys.GetVerticesOfRealOrderByName()
                     .Select(fun r -> rowDeviceItems $"{r.Flow.Name}.{r.Name}" false)

        addRows rows dt
        let emptyLine () = emptyRow (Enum.GetNames(typedefof<TextColumn>)) dt
     
        emptyLine ()
        dt


    let ToDevicesTable (sys: DsSystem)  : DataTable =

        let dt = getLabelTable "디바이스이름"
      
        let rows =
            let devCallSet =  sys.GetDevicesForHMI()
            devCallSet.Select(fun (dev,call)-> 
                    
                    let hasSafety = call.SafetyConditions.Count > 0   
                    rowDeviceItems dev.ApiStgName hasSafety)

        addRows rows dt
        let emptyLine () = emptyRow (Enum.GetNames(typedefof<TextColumn>)) dt
     
        emptyLine ()
        dt



    let ToAutoWorkTable (sys: DsSystem) target: DataTable =
        let dt = new System.Data.DataTable("Work자동조작")
        dt.Columns.Add($"{AutoColumn.Name}", typeof<string>) |> ignore
        dt.Columns.Add($"{AutoColumn.DataType}", typeof<string>) |> ignore
        dt.Columns.Add($"{AutoColumn.Address}", typeof<string>) |> ignore


        let rowItems (name: string, addr:string) =
            [ name;"bit";addr ]

        let rows =
            sys.GetVerticesOfRealOrderByName()
            |> Seq.collect (fun real ->
                let name = $"{real.Flow.Name}_{real.Name}"  
                [   
                    yield rowItems ($"{name}_SET", real.V.ON.Address)    
                    yield rowItems ($"{name}_RESET", real.V.RF.Address)    
                    yield rowItems ($"{name}_START", real.V.SF.Address)    
                    yield rowItems ($"{name}_ORIGIN", real.V.OB.Address)    
                    yield rowItems ($"{name}_ERROR", real.V.ErrTRX.Address)    
                    yield rowItems ($"{name}_STATE_R", real.V.R.Address)    
                    yield rowItems ($"{name}_STATE_G", real.V.G.Address)    
                    yield rowItems ($"{name}_STATE_F", real.V.F.Address)    
                    yield rowItems ($"{name}_STATE_H", real.V.H.Address)    
                ]
            )

        addRows rows dt
        let emptyLine () = emptyRow (Enum.GetNames(typedefof<ManualColumn>)) dt
     
        emptyLine ()
        dt


    let ToAutoFlowTable (sys: DsSystem)  target: DataTable =
        let dt = new System.Data.DataTable("Flow자동조작")
        dt.Columns.Add($"{AutoColumn.Name}", typeof<string>) |> ignore
        dt.Columns.Add($"{AutoColumn.DataType}", typeof<string>) |> ignore
        dt.Columns.Add($"{AutoColumn.Address}", typeof<string>) |> ignore

        let rowItems (name: string, addr:string) =
            [ name;"bit";addr ]

        let rows =
            sys.GetFlowsOrderByName()
            |> Seq.collect (fun flow ->
                [   
                    yield rowItems ($"{flow.Name}_FlowAutoSelect", flow.auto_btn.Address)
                    yield rowItems ($"{flow.Name}_FlowAutoLamp", flow.aop.Address)
                    yield rowItems ($"{flow.Name}_FlowManualSelect", flow.manual_btn.Address)
                    yield rowItems ($"{flow.Name}_FlowManualLamp", flow.mop.Address)
                    yield rowItems ($"{flow.Name}_FlowDriveBtn", flow.drive_btn.Address)
                    yield rowItems ($"{flow.Name}_FlowDriveLamp", flow.d_st.Address)
                    yield rowItems ($"{flow.Name}_FlowPauseBtn", flow.pause_btn.Address)
                    yield rowItems ($"{flow.Name}_FlowPauseLamp", flow.p_st.Address)
                ]
            )

        addRows rows dt
        let emptyLine () = emptyRow (Enum.GetNames(typedefof<ManualColumn>)) dt
     
        emptyLine ()
        dt


    let ToManualTable (sys: DsSystem) (iomType:IOType) : DataTable =
        let tableName =
            match iomType with
            | IOType.Memory  -> "액션(M)"
            | IOType.In      -> "센서(I)"
            | IOType.Out     -> "출력(Q)"
            | _ -> failwith "Invalid action tag"

        let dt = new System.Data.DataTable(tableName)
        dt.Columns.Add($"{ManualColumn.Name}", typeof<string>) |> ignore
        dt.Columns.Add($"{ManualColumn.DataType}", typeof<string>) |> ignore
        dt.Columns.Add($"{ManualColumn.Address}", typeof<string>) |> ignore

        let rowItems (dev: TaskDev, addr:string) =
            [ 
              dev.ApiName
              dev.InDataType.ToPLCText()
              addr
               ]

        let rows =
            
            let devs = sys.GetDevicesForHMI()
            devs
            |> Seq.collect (fun (dev,_) ->
                 [   
                   
                    match iomType with
                    | IOType.Memory ->
                        yield rowItems (dev, if dev.IsMaunualAddressSkipOrEmpty then HMITempManualAction else dev.MaunualAddress)
                    | IOType.In->
                        yield rowItems (dev, if dev.IsInAddressSkipOrEmpty then HMITempMemory else dev.InAddress)
                    | IOType.Out ->                            
                        yield rowItems (dev, if dev.IsOutAddressSkipOrEmpty then HMITempMemory else dev.OutAddress)

                    | _ -> failwith "Invalid action tag"
                 ]
            ) 

        addRows rows dt
        let emptyLine () = emptyRow (Enum.GetNames(typedefof<ManualColumn>)) dt

     
        emptyLine ()
        dt



    let ToManualTable_BtnLamp (sys: DsSystem)  : DataTable =

        let dt = new System.Data.DataTable($"조작반(M)")
        dt.Columns.Add($"{ManualColumn_ControlPanel.Name}", typeof<string>) |> ignore
        dt.Columns.Add($"{ManualColumn_ControlPanel.DataType}", typeof<string>) |> ignore
        dt.Columns.Add($"{ManualColumn_ControlPanel.Manual}", typeof<string>) |> ignore
      
        let rowItems (name  : string, address :string) =
            [ 
              name
              "bit"
              address
            ]

        let rows =
            let hws = sys.HwSystemDefs.Where(fun f->f :? ButtonDef || f :? LampDef )
                                      .Select(fun f-> f.Name, if f :? ButtonDef  then f.InAddress else f.OutAddress)
                                      .OrderBy(fun (name, addr) -> addr)
            hws
                |> Seq.map (fun (name, addr) -> 
                    rowItems (name, addr)
                    )


        addRows rows dt

        addRows [[ "SimulationLamp"; "bool"; RuntimeDS.EmulationAddress ]] dt

        let emptyLine () = emptyRow (Enum.GetNames(typedefof<ManualColumn>)) dt
     
        emptyLine ()
        dt


    let ToIOListDataTables (system: DsSystem) target = 
        let tableDeviceIOs = ToDeviceIOTables system system.Flows true target
        let tablePanelIO = ToPanelIOTable system system.Flows true target
        let tabletableFuncVariExternal = ToFuncVariTables system system.Flows true target
        
        let tables = tableDeviceIOs  @ [tablePanelIO ] @ tabletableFuncVariExternal
     
        tables
    
    
    let toDataTablesToCSV (dataTables: seq<DataTable>) (fileName:string) =
        let csvContent = new StringBuilder()

        // 각 DataTable을 순회
        for dataTable in dataTables do
            // 컬럼 헤더 추가
            let columnNames = 
                dataTable.Columns 
                |> Seq.cast<DataColumn> 
                |> Seq.map (fun col -> "\""+col.ColumnName+"\"")
                |> Seq.toArray
                |> (fun array -> String.Join("\t", array))

            csvContent.AppendLine(columnNames) |> ignore

            // 각 행의 데이터 추가
            dataTable.Rows
            |> Seq.cast<DataRow>
            |> Seq.iter (fun row ->
                let fieldValues =
                    row.ItemArray
                    |> Seq.map (fun obj ->
                        let field = obj.ToString()
                        //field.Replace("\t", "\\t") // 탭 문자 처리
                        "\""+field.Replace("\t", "\\t")+"\"" // 탭 문자 처리
                    )
                    |> Seq.toArray
                    |> (fun array -> String.Join("\t", array))

                csvContent.AppendLine(fieldValues) |> ignore)

            // DataTable 간 구분을 위해 빈 줄 추가 (필요한 경우)
            csvContent.AppendLine() |> ignore

        // 지정된 파일 이름으로 CSV 내용을 파일에 씀
        let filePath = Path.Combine(Path.GetTempPath(), fileName + ".csv")
        File.WriteAllText(filePath, csvContent.ToString(), Encoding.UTF8)

        filePath


    [<Extension>]
    type OfficeExcelExt =
        [<Extension>]
        static member ExportIOListToExcel (system: DsSystem) (filePath: string) target=
            let dataTables =  ToIOListDataTables system  target@  [|ToErrorTable system|]
            createSpreadsheet filePath dataTables 25.0 true

        [<Extension>]
        static member ExportIOListToPDF (system: DsSystem) (filePath: string) target=
            let dataTables =  ToIOListDataTables system  target@  [|ToErrorTable system|]
            convertDataSetToPdf filePath dataTables 

            
        [<Extension>]
        static member ExportHMITableToExcel (sys: DsSystem) (filePath: string) target=
            let dataTables = [|

                ToManualTable sys IOType.Memory    
                ToManualTable sys IOType.In
                ToManualTable sys IOType.Out
                ToManualTable_BtnLamp sys

                ToAutoFlowTable sys target
                ToAutoWorkTable sys target

                ToFlowNamesTable sys 
                ToWorkNamesTable sys
                ToDevicesTable sys
                ToDevicesApiTable sys
                ToAlarmTable sys

                                |]
            createSpreadsheet filePath dataTables 25.0 false

        [<Extension>]
        static member ToDataCSVFlows  (system: DsSystem) (flowNames:string seq) (conatinSys:bool) target =
            let dataTables = ToIOListDataTables system target
            toDataTablesToCSV dataTables "IOTABLE"

        [<Extension>]
        static member ToDataCSVLayouts (xs: Flow seq) =
            let dataTable = ToLayoutTable xs 
            toDataTablesToCSV [dataTable] "LAYOUT"
      
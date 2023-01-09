namespace Engine.Core

open Engine.Common.FS

[<AutoOpen>]
module CounterStatementModule =

    type (*internal*) CounterCreateParams = {
        Type: CounterType
        Name: string
        Preset: CountUnitType
        CountUpCondition: IExpression<bool> option
        CountDownCondition: IExpression<bool> option
        ResetCondition: IExpression<bool> option
        LoadCondition: IExpression<bool> option
        FunctionName:string
    }

    let (*private*) createCounterStatement (storages:Storages) {
        Type=typ; Name=name; Preset=preset; FunctionName=functionName
        CountUpCondition=countUpCondition; CountDownCondition=countDownCondition;
        ResetCondition=resetCondition; LoadCondition=loadCondition
    } : Statement =
        let accum = 0us
        let cs =    // counter structure
            match typ with
            | CTU  -> CTUStruct.Create(typ, storages, name, preset, accum) :> CounterBaseStruct
            | CTD  -> CTDStruct.Create(typ, storages, name, preset, accum)
            | CTUD -> CTUDStruct.Create(typ, storages, name, preset, accum)
            | CTR  -> CTRStruct.Create(typ, storages, name, preset, accum)
        let counter = new Counter   (typ, cs)

        let statements = ResizeArray<Statement>()
        match countUpCondition with
        | Some up->
            let statement = DuAssign (up, cs.CU)
            statement.Do()
            statements.Add statement
        | None -> ()
        match countDownCondition with
        | Some down ->
            let statement = DuAssign (down, cs.CD)
            statement.Do()
            statements.Add statement
        | None -> ()

        if not <| isItNull cs.RES then
            match resetCondition  with
            | Some reset ->
                let statement = DuAssign (reset, cs.RES)
                statement.Do()
                statements.Add statement
            | None -> ()

        match loadCondition with
        | Some load ->
            let statement = DuAssign (load, cs.LD)
            statement.Do()
            statements.Add statement
        | None -> ()



        counter.InputEvaluateStatements <- statements.ToFSharpList()
        let counterStatement:CounterStatement =
            {   Counter=counter; FunctionName=functionName;
                UpCondition=countUpCondition; DownCondition=countDownCondition;
                ResetCondition=resetCondition; LoadCondition=loadCondition;  }
        DuCounter counterStatement

    let defaultCounterCreateParam = {
        Type=CTU
        Name=""
        Preset=0us
        CountUpCondition=None
        CountDownCondition=None
        ResetCondition=None
        LoadCondition=None
        FunctionName=""
    }

    type CounterStatement =
        static member CreateCTU(tcParams:TCConstructionParams) =
            let {Storages=storages; Name=name; Preset=preset; RungInCondition=rungInCondition; FunctionName=functionName} = tcParams

            ({ defaultCounterCreateParam with
                Type=CTU; Name=name; Preset=preset; FunctionName=functionName
                CountUpCondition=Some rungInCondition; } :CounterCreateParams)
            |> createCounterStatement storages

        static member CreateCTD(tcParams:TCConstructionParams) =
            let {Storages=storages; Name=name; Preset=preset; RungInCondition=rungInCondition; FunctionName=functionName} = tcParams
            { defaultCounterCreateParam with
                Type=CTD; Name=name; Preset=preset; FunctionName=functionName
                CountDownCondition=Some rungInCondition; }
            |> createCounterStatement storages

        //static member CreateCTUD(tcParams:TCConstructionParams, countDownCondition, accum) =
        //    let {Storages=storages; Name=name; Preset=preset; RungInCondition=countUpCondition; FunctionName=functionName} = tcParams
        //    {   Type=CTUD; Name=name; Preset=preset;
        //        CountUpCondition=Some countUpCondition;
        //        CountDownCondition=Some countDownCondition;
        //        ResetCondition=None; FunctionName=functionName }
        //    |> createCounterStatement storages

        static member CreateCTUD(tcParams:TCConstructionParams, countDownCondition, reset) =
            let {Storages=storages; Name=name; Preset=preset; RungInCondition=countUpCondition; FunctionName=functionName} = tcParams
            { defaultCounterCreateParam with
                Type=CTUD; Name=name; Preset=preset; FunctionName=functionName
                CountUpCondition   = Some countUpCondition;
                CountDownCondition = Some countDownCondition;
                ResetCondition     = Some reset;  }
            |> createCounterStatement storages

        static member CreateXgiCTUD(tcParams:TCConstructionParams, countDownCondition, reset, ldCondition) =
            let {Storages=storages; Name=name; Preset=preset; RungInCondition=countUpCondition; FunctionName=functionName} = tcParams
            { defaultCounterCreateParam with
                Type=CTUD; Name=name; Preset=preset; FunctionName=functionName
                CountUpCondition   = Some countUpCondition
                CountDownCondition = Some countDownCondition
                LoadCondition      = Some ldCondition
                ResetCondition     = Some reset  }
            |> createCounterStatement storages


        static member CreateCTR(tcParams:TCConstructionParams) =
            let {Storages=storages; Name=name; Preset=preset; RungInCondition=rungInCondition; FunctionName=functionName} = tcParams
            { defaultCounterCreateParam with
                Type=CTR; Name=name; Preset=preset; FunctionName=functionName
                CountUpCondition=Some rungInCondition; }
            |> createCounterStatement storages


        static member CreateCTU(tcParams:TCConstructionParams, reset) =
            let {Storages=storages; Name=name; Preset=preset; RungInCondition=rungInCondition; FunctionName=functionName} = tcParams
            { defaultCounterCreateParam with
                Type=CTU; Name=name; Preset=preset; FunctionName=functionName
                CountUpCondition = Some rungInCondition;
                ResetCondition   = Some reset; }
            |> createCounterStatement storages

        static member CreateXgiCTD(tcParams:TCConstructionParams, load) =
            let {Storages=storages; Name=name; Preset=preset; RungInCondition=rungInCondition; FunctionName=functionName} = tcParams
            { defaultCounterCreateParam with
                Type=CTD; Name=name; Preset=preset; FunctionName=functionName
                CountDownCondition = Some rungInCondition;
                LoadCondition     = Some load; }
            |> createCounterStatement storages

        static member CreateXgiCTR(tcParams:TCConstructionParams, reset) =
            let {Storages=storages; Name=name; Preset=preset; RungInCondition=rungInCondition; FunctionName=functionName} = tcParams
            { defaultCounterCreateParam with
                Type=CTR; Name=name; Preset=preset; FunctionName=functionName
                CountDownCondition = Some rungInCondition;
                ResetCondition   = Some reset; }
            |> createCounterStatement storages


namespace Engine.Core

open Engine.Common.FS

[<AutoOpen>]
module CounterStatementModule =

    type (*internal*) CounterCreateParams = {
        Type: CounterType
        Name: string
        Preset: CountUnitType
        Accumulator: CountUnitType option
        CountUpCondition: IExpression<bool> option
        CountDownCondition: IExpression<bool> option
        ResetCondition: IExpression<bool> option
    }

    let (*private*) createCounterStatement (storages:Storages) {
        Type=typ; Name=name; Preset=preset;
        CountUpCondition=countUpCondition; CountDownCondition=countDownCondition;
        ResetCondition=resetCondition; Accumulator=accum
    } : Statement =
        let accum = accum |? 0us
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
        match resetCondition with
        | Some reset ->
            let statement = DuAssign (reset, cs.RES)
            statement.Do()
            statements.Add statement
        | None -> ()



        counter.InputEvaluateStatements <- statements.ToFSharpList()
        DuCounter { Counter=counter; UpCondition=countUpCondition; DownCondition=countDownCondition; ResetCondition=resetCondition }

    type CounterStatement =
        static member CreateCTU(tcParams:TCConstructionParams) =
            let {Storages=storages; Name=name; Preset=preset; RungInCondition=rungInCondition} = tcParams

            {   Type=CTU; Name=name; Preset=preset;
                CountUpCondition=Some rungInCondition;
                CountDownCondition=None; ResetCondition=None; Accumulator=None }
            |> createCounterStatement storages

        static member CreateCTD(tcParams:TCConstructionParams, accum) =
            let {Storages=storages; Name=name; Preset=preset; RungInCondition=rungInCondition} = tcParams
            {   Type=CTD; Name=name; Preset=preset;
                CountUpCondition=None;
                CountDownCondition=Some rungInCondition;
                ResetCondition=None; Accumulator=Some accum }
            |> createCounterStatement storages

        static member CreateCTUD(tcParams:TCConstructionParams, countDownCondition, accum) =
            let {Storages=storages; Name=name; Preset=preset; RungInCondition=countUpCondition} = tcParams
            {   Type=CTUD; Name=name; Preset=preset;
                CountUpCondition=Some countUpCondition;
                CountDownCondition=Some countDownCondition;
                ResetCondition=None; Accumulator=Some accum }
            |> createCounterStatement storages

        static member CreateCTR(tcParams:TCConstructionParams) =
            let {Storages=storages; Name=name; Preset=preset; RungInCondition=rungInCondition} = tcParams
            {   Type=CTR; Name=name; Preset=preset;
                CountUpCondition=Some rungInCondition;
                CountDownCondition=None; ResetCondition=None; Accumulator=None }
            |> createCounterStatement storages


        static member CreateCTU(tcParams:TCConstructionParams, reset) =
            let {Storages=storages; Name=name; Preset=preset; RungInCondition=rungInCondition} = tcParams
            {   Type=CTU; Name=name; Preset=preset;
                CountUpCondition=Some rungInCondition;
                CountDownCondition=None;
                ResetCondition=Some reset; Accumulator=None }
            |> createCounterStatement storages

        static member CreateCTD(tcParams:TCConstructionParams, reset, accum) =
            let {Storages=storages; Name=name; Preset=preset; RungInCondition=rungInCondition} = tcParams
            {   Type=CTD; Name=name; Preset=preset;
                CountUpCondition=None;
                CountDownCondition=Some rungInCondition;
                ResetCondition=Some reset; Accumulator=Some accum }
            |> createCounterStatement storages

        static member CreateCTUD(tcParams:TCConstructionParams, countDownCondition, reset, accum) =
            let {Storages=storages; Name=name; Preset=preset; RungInCondition=countUpCondition} = tcParams
            {   Type=CTUD; Name=name; Preset=preset;
                CountUpCondition=Some countUpCondition;
                CountDownCondition=Some countDownCondition;
                ResetCondition=Some reset; Accumulator=Some accum }
            |> createCounterStatement storages

        static member CreateCTR(tcParams:TCConstructionParams, reset) =
            let {Storages=storages; Name=name; Preset=preset; RungInCondition=rungInCondition} = tcParams
            {   Type=CTR; Name=name; Preset=preset;
                CountUpCondition=Some rungInCondition;
                CountDownCondition=None;
                ResetCondition=Some reset; Accumulator=None }
            |> createCounterStatement storages


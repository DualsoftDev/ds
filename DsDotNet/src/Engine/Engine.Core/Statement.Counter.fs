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
            | CTU  -> new CTUStruct(typ, storages, name, preset, accum) :> CounterBaseStruct
            | CTD  -> new CTDStruct(typ, storages, name, preset, accum)
            | CTUD -> new CTUDStruct(typ, storages, name, preset, accum)
            | CTR  -> new CTRStruct(typ, storages, name, preset, accum)
        let counter = new Counter(typ, cs)

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
        static member CreateCTU(storages:Storages, name, preset, rungConditionIn) =
            {   Type=CTU; Name=name; Preset=preset;
                CountUpCondition=Some rungConditionIn;
                CountDownCondition=None; ResetCondition=None; Accumulator=None }
            |> createCounterStatement storages

        static member CreateCTD(storages:Storages, name, preset, rungConditionIn, accum) =
            {   Type=CTD; Name=name; Preset=preset;
                CountUpCondition=None;
                CountDownCondition=Some rungConditionIn;
                ResetCondition=None; Accumulator=Some accum }
            |> createCounterStatement storages

        static member CreateCTUD(storages:Storages, name, preset, countUpCondition, countDownCondition, accum) =
            {   Type=CTUD; Name=name; Preset=preset;
                CountUpCondition=Some countUpCondition;
                CountDownCondition=Some countDownCondition;
                ResetCondition=None; Accumulator=Some accum }
            |> createCounterStatement storages

        static member CreateCTR(storages:Storages, name, preset, rungConditionIn) =
            {   Type=CTR; Name=name; Preset=preset;
                CountUpCondition=Some rungConditionIn;
                CountDownCondition=None; ResetCondition=None; Accumulator=None }
            |> createCounterStatement storages


        static member CreateCTU(storages:Storages, name, preset, rungConditionIn, reset) =
            {   Type=CTU; Name=name; Preset=preset;
                CountUpCondition=Some rungConditionIn;
                CountDownCondition=None;
                ResetCondition=Some reset; Accumulator=None }
            |> createCounterStatement storages

        static member CreateCTD(storages:Storages, name, preset, rungConditionIn, reset, accum) =
            {   Type=CTD; Name=name; Preset=preset;
                CountUpCondition=None;
                CountDownCondition=Some rungConditionIn;
                ResetCondition=Some reset; Accumulator=Some accum }
            |> createCounterStatement storages

        static member CreateCTUD(storages:Storages, name, preset, countUpCondition, countDownCondition, reset, accum) =
            {   Type=CTUD; Name=name; Preset=preset;
                CountUpCondition=Some countUpCondition;
                CountDownCondition=Some countDownCondition;
                ResetCondition=Some reset; Accumulator=Some accum }
            |> createCounterStatement storages

        static member CreateCTR(storages:Storages, name, preset, rungConditionIn, reset) =
            {   Type=CTR; Name=name; Preset=preset;
                CountUpCondition=Some rungConditionIn;
                CountDownCondition=None;
                ResetCondition=Some reset; Accumulator=None }
            |> createCounterStatement storages


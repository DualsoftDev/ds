namespace Engine.Core

open Engine.Common.FS

[<AutoOpen>]
module CounterStatementModule =

    type internal CounterCreateParams = {
        Type: CounterType
        Name: string
        Preset: CountUnitType
        Accumulator: CountUnitType option
        CountUpCondition: IExpression option
        CountDownCondition: IExpression option
        ResetCondition: IExpression option
    }

    let private createCounter {
        Type=typ; Name=name; Preset=preset;
        CountUpCondition=countUpCondition; CountDownCondition=countDownCondition;
        ResetCondition=resetCondition; Accumulator=accum
    } : Counter =
        let accum = accum |? 0us
        let cs =    // counter structure
            match typ with
            | CTU -> new CTUStruct(name, preset, accum) :> CounterBaseStruct
            | CTD -> new CTDStruct(name, preset, accum)
            | CTUD -> new CTUDStruct(name, preset, accum)
        let counter = new Counter(typ, cs)

        let statements = ResizeArray<Statement>()
        match countUpCondition with
        | Some up->
            let statement = Assign (up, cs.CU)
            statement.Do()
            statements.Add statement
        | None -> ()
        match countDownCondition with
        | Some down ->
            let statement = Assign (down, cs.CD)
            statement.Do()
            statements.Add statement
        | None -> ()
        match resetCondition with
        | Some reset ->
            let statement = Assign (reset, cs.RES)
            statement.Do()
            statements.Add statement
        | None -> ()



        counter.InputEvaluateStatements <- statements.ToFSharpList()
        counter

    type Counter =
        static member CreateCTU(name, preset, rungConditionIn) =
            {   Type=CTU; Name=name; Preset=preset;
                CountUpCondition=Some rungConditionIn;
                CountDownCondition=None; ResetCondition=None; Accumulator=None }
            |> createCounter

        static member CreateCTD(name, preset, rungConditionIn, accum) =
            {   Type=CTD; Name=name; Preset=preset;
                CountUpCondition=None;
                CountDownCondition=Some rungConditionIn;
                ResetCondition=None; Accumulator=Some accum }
            |> createCounter

        static member CreateCTUD(name, preset, countUpCondition, countDownCondition, accum) =
            {   Type=CTUD; Name=name; Preset=preset;
                CountUpCondition=Some countUpCondition;
                CountDownCondition=Some countDownCondition;
                ResetCondition=None; Accumulator=Some accum }
            |> createCounter


        static member CreateCTU(name, preset, rungConditionIn, reset) =
            {   Type=CTU; Name=name; Preset=preset;
                CountUpCondition=Some rungConditionIn;
                CountDownCondition=None;
                ResetCondition=Some reset; Accumulator=None }
            |> createCounter

        static member CreateCTD(name, preset, rungConditionIn, reset, accum) =
            {   Type=CTD; Name=name; Preset=preset;
                CountUpCondition=None;
                CountDownCondition=Some rungConditionIn;
                ResetCondition=Some reset; Accumulator=Some accum }
            |> createCounter

        static member CreateCTUD(name, preset, countUpCondition, countDownCondition, reset, accum) =
            {   Type=CTUD; Name=name; Preset=preset;
                CountUpCondition=Some countUpCondition;
                CountDownCondition=Some countDownCondition;
                ResetCondition=Some reset; Accumulator=Some accum }
            |> createCounter



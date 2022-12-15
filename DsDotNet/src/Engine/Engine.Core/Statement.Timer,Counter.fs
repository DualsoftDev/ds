namespace Engine.Core

open Engine.Common.FS

[<AutoOpen>]
module TimerStatementModule =
    type internal TimerCreateParams = {
        Type: TimerType
        Name: string
        Preset: uint16
        RungConditionIn: IExpression option
        ResetCondition: IExpression option
    }

    let private createTimer { Type=typ; Name=name; Preset=preset; RungConditionIn=rungConditionIn; ResetCondition=resetCondition;} =
        let ts = TimerStruct(name, preset)
        let timer = new Timer(typ, ts)

        let statements = ResizeArray<Statement>()
        match rungConditionIn with
        | Some cond ->
            let rungInStatement = Assign (cond, ts.EN)
            rungInStatement.Do()
            statements.Add rungInStatement
        | None -> ()

        match resetCondition with
        | Some cond ->
            let resetStatement = Assign (cond, ts.RES)
            resetStatement.Do()
            statements.Add resetStatement
        | None -> ()

        match resetCondition with
        | Some reset ->
            let statement = Assign (reset, ts.RES)
            statement.Do()
            statements.Add statement
        | None -> ()

        timer.InputEvaluateStatements <- statements.ToFSharpList()
        timer

    type Timer =
        static member CreateTON(name, rungConditionIn, preset) =
            {   Type=TON; Name=name; Preset=preset;
                RungConditionIn=Some rungConditionIn;
                ResetCondition=None; }
            |> createTimer

        static member CreateTOF(name, rungConditionIn, preset) =
            {   Type=TOF; Name=name; Preset=preset;
                RungConditionIn=Some rungConditionIn;
                ResetCondition=None; }
            |> createTimer

        static member CreateRTO(name, rungConditionIn, preset) =
            {   Type=RTO; Name=name; Preset=preset;
                RungConditionIn=Some rungConditionIn;
                ResetCondition=None; }
            |> createTimer

        static member CreateTON(name, rungConditionIn, resetCondition, preset) =
            {   Type=TON; Name=name; Preset=preset;
                RungConditionIn=Some rungConditionIn;
                ResetCondition=Some resetCondition; }
            |> createTimer

        static member CreateTOF(name, rungConditionIn, resetCondition, preset) =
            {   Type=TOF; Name=name; Preset=preset;
                RungConditionIn=Some rungConditionIn;
                ResetCondition=Some resetCondition; }
            |> createTimer
        static member CreateRTO(name, rungConditionIn, resetCondition, preset) =
            {   Type=RTO; Name=name; Preset=preset;
                RungConditionIn=Some rungConditionIn;
                ResetCondition=Some resetCondition; }
            |> createTimer



[<AutoOpen>]
module CounterStatementModule =

    type internal CounterCreateParams = {
        Type: CounterType
        Name: string
        Preset: uint16
        CountUpCondition: IExpression option
        CountDownCondition: IExpression option
        ResetCondition: IExpression option
    }

    let private createCounter {Type=typ; Name=name; Preset=preset; CountUpCondition=countUpCondition; CountDownCondition=countDownCondition; ResetCondition=resetCondition} =
        let cs =    // counter structure
            match typ with
            | CTU -> new CTUStruct(name, preset) :> CounterBaseStruct
            | CTD -> new CTDStruct(name, preset)
            | CTUD -> new CTUDStruct(name, preset)
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
        static member CreateCTU(name, rungConditionIn, preset) =
            {   Type=CTU; Name=name; Preset=preset;
                CountUpCondition=Some rungConditionIn;
                CountDownCondition=None; ResetCondition=None; }
            |> createCounter

        static member CreateCTD(name, rungConditionIn, preset) =
            {   Type=CTD; Name=name; Preset=preset;
                CountUpCondition=None;
                CountDownCondition=Some rungConditionIn;
                ResetCondition=None; }
            |> createCounter

        static member CreateCTUD(name, countUpCondition, countDownCondition, preset) =
            {   Type=CTUD; Name=name; Preset=preset;
                CountUpCondition=Some countUpCondition;
                CountDownCondition=Some countDownCondition;
                ResetCondition=None; }
            |> createCounter


        static member CreateCTU(name, rungConditionIn, reset, preset) =
            {   Type=CTU; Name=name; Preset=preset;
                CountUpCondition=Some rungConditionIn;
                CountDownCondition=None;
                ResetCondition=Some reset; }
            |> createCounter

        static member CreateCTD(name, rungConditionIn, reset, preset) =
            {   Type=CTD; Name=name; Preset=preset;
                CountUpCondition=None;
                CountDownCondition=Some rungConditionIn;
                ResetCondition=Some reset; }
            |> createCounter

        static member CreateCTUD(name, countUpCondition, countDownCondition, reset, preset) =
            {   Type=CTUD; Name=name; Preset=preset;
                CountUpCondition=Some countUpCondition;
                CountDownCondition=Some countDownCondition;
                ResetCondition=Some reset; }
            |> createCounter

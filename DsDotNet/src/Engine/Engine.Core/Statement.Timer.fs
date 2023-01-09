namespace Engine.Core

open Engine.Common.FS

[<AutoOpen>]
module TimerStatementModule =
    type internal TimerCreateParams = {
        Type: TimerType
        Name: string
        Preset: CountUnitType
        RungConditionIn: IExpression<bool> option
        ResetCondition: IExpression<bool> option
        FunctionName:string
    }

    let private createTimerStatement (storages:Storages) {
        Type=typ; Name=name; Preset=preset;
        RungConditionIn=rungInCondition; ResetCondition=resetCondition; FunctionName=functionName
    } : Statement =
        if preset < MinTickInterval then
            failwith <| sprintf "Timer Resolution Error: Preset value should be larger than %A" MinTickInterval

        let ts = TimerStruct.Create(typ, storages, name, preset, 0us)
        let timer = new Timer(typ, ts)

        let statements = ResizeArray<Statement>()
        match rungInCondition with
        | Some cond ->
            let rungInStatement = DuAssign (cond, ts.EN)
            rungInStatement.Do()
            statements.Add rungInStatement
        | None -> ()

        match resetCondition with
        | Some cond ->
            let resetStatement = DuAssign (cond, ts.RES)
            resetStatement.Do()
            statements.Add resetStatement
        | None -> ()

        timer.InputEvaluateStatements <- statements.ToFSharpList()
        DuTimer ({ Timer=timer; RungInCondition = rungInCondition; ResetCondition = resetCondition; FunctionName=functionName }:TimerStatement)


    /// Timer & Counter construction parameters
    type TCConstructionParams = {
        Storages:Storages
        /// timer/counter structure name
        Name: string
        Preset: CountUnitType
        RungInCondition:IExpression<bool>
        /// e.g 'createXgiCTU'
        FunctionName:string
    }

    type TimerStatement =
        static member CreateTON(tcParams:TCConstructionParams) =
            let {Storages=storages; Name=name; Preset=preset; RungInCondition=rungInCondition; FunctionName=functionName} = tcParams
            ({   Type=TON; Name=name; Preset=preset;
                RungConditionIn=Some rungInCondition;
                ResetCondition=None; FunctionName=functionName}:TimerCreateParams)
            |> createTimerStatement storages

        static member CreateTOF(tcParams:TCConstructionParams) =
            let {Storages=storages; Name=name; Preset=preset; RungInCondition=rungInCondition; FunctionName=functionName} = tcParams
            ({   Type=TOF; Name=name; Preset=preset;
                RungConditionIn=Some rungInCondition;
                ResetCondition=None; FunctionName=functionName}:TimerCreateParams)
            |> createTimerStatement storages

        static member CreateRTO(tcParams:TCConstructionParams) =
            let {Storages=storages; Name=name; Preset=preset; RungInCondition=rungInCondition; FunctionName=functionName} = tcParams
            ({   Type=RTO; Name=name; Preset=preset;
                RungConditionIn=Some rungInCondition;
                ResetCondition=None; FunctionName=functionName }:TimerCreateParams)
            |> createTimerStatement storages

        //static member CreateTON(tcParams:TCConstructionParams, resetCondition) =
        //    let {Storages=storages; Name=name; Preset=preset; RungInCondition=rungInCondition} = tcParams
        //    {   Type=TON; Name=name; Preset=preset;
        //        RungConditionIn=Some rungInCondition;
        //        ResetCondition=Some resetCondition; }
        //    |> createTimerStatement storages

        //static member CreateTOF(tcParams:TCConstructionParams, resetCondition) =
        //    let {Storages=storages; Name=name; Preset=preset; RungInCondition=rungInCondition} = tcParams
        //    {   Type=TOF; Name=name; Preset=preset;
        //        RungConditionIn=Some rungInCondition;
        //        ResetCondition=Some resetCondition; }
        //    |> createTimerStatement storages

        static member CreateRTO(tcParams:TCConstructionParams, resetCondition) =
            let {Storages=storages; Name=name; Preset=preset; RungInCondition=rungInCondition; FunctionName=functionName} = tcParams
            ({   Type=RTO; Name=name; Preset=preset;
                RungConditionIn=Some rungInCondition;
                ResetCondition=Some resetCondition; FunctionName=functionName}:TimerCreateParams)
            |> createTimerStatement storages




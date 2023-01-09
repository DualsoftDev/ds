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

    
    let private generateTimerStatement (ts :TimerStruct, tParams:TimerCreateParams) = 

        if ts.PRE.Value < MinTickInterval then
            failwith <| sprintf "Timer Resolution Error: Preset value should be larger than %A" MinTickInterval

        let timer = new Timer(ts.Type, ts)
        let statements = ResizeArray<Statement>()
        match tParams.RungConditionIn with
        | Some cond ->
            let rungInStatement = DuAssign (cond, ts.EN)
            rungInStatement.Do()
            statements.Add rungInStatement
        | None -> ()

        match tParams.ResetCondition with
        | Some cond ->
            let resetStatement = DuAssign (cond, ts.RES)
            resetStatement.Do()
            statements.Add resetStatement
        | None -> ()
        //RungConditionIn,  ResetCondition 동시 입력 대비 ?? <kwak>
        match tParams.ResetCondition with
        | Some reset ->
            let statement = DuAssign (reset, ts.RES)
            statement.Do()
            statements.Add statement
        | None -> ()

        timer.InputEvaluateStatements <- statements.ToFSharpList()
        DuTimer ({ Timer=timer; RungInCondition = tParams.RungConditionIn; ResetCondition = tParams.ResetCondition; FunctionName=tParams.FunctionName }:TimerStatement)


    let private createTONStatement (ts :TimerStruct, rungInCondition, resetCondition)  : Statement =
      
        let tParams ={ Type=ts.Type; Name=ts.Name; Preset=ts.PRE.Value;
                       RungConditionIn=rungInCondition; ResetCondition=resetCondition; FunctionName="createWinTON"} 
    
        generateTimerStatement (ts, tParams)

    let private createTimerStatement (storages:Storages) (tParams:TimerCreateParams)   : Statement =
        let ts = TimerStruct.Create(tParams.Type, storages, tParams.Name, tParams.Preset, 0us)
        generateTimerStatement (ts, tParams)


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

        static member CreateTONUsingTag(ts: TimerStruct, rungInCondition, resetCondition) =
            createTONStatement (ts, rungInCondition, resetCondition)

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




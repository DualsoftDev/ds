namespace Engine.Core
open System
open System.Reactive.Linq

[<AutoOpen>]
module TimerStatementModule =

    let CreateTON(name, condition, target20msCounter) =
        let timer = new Timer(TON, name, condition, target20msCounter)
        let ts = timer.Struct
        let statement = Assign (condition, ts.EN)
        //let fire() = ts.DN.Value <- true
        //timer.Firere.Fire <- Some fire
        //let onConditionChanged newCondition =
        //    assert(timer.CachedConditionValue <> newCondition)
        //    ts.EN.Value <- newCondition
        //    ts.TT.Value <- newCondition
        //    if newCondition then    // rising
        //        assert(not ts.DN.Value)
        //        timer.Firere.Start()
        //    else
        //        ()
        //timer.OptOnConditionChanged <- Some onConditionChanged

        timer.ConditionCheckStatement <- Some statement
        statement, timer

    let CreateTOF(name, condition, target20msCounter) =
        let timer = new Timer(TOF, name, condition, target20msCounter)
        let ts = timer.Struct
        let statement = Assign (fLogicalNot [condition], ts.EN) // The TOF instruction is a non-retentive timer that accumulates time when the instruction is enabled (rung-condition-in is false
        timer.ConditionCheckStatement <- Some statement

        statement, timer
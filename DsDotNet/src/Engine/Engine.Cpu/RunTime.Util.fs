namespace Engine.Cpu

open Engine.Core
open System.Linq
open Engine.CodeGenCPU

[<AutoOpen>]
module internal RunTimeUtil =
    /// Notify PreExcute(주의 !! 연산에 관련된건만 이벤트 처리)
    let notifyPreExcute ( x:IStorage) =
        x.GetTagInfo() 
        |> Option.iter(fun t -> t.OnChanged())


    ///HMI Reset
    let syncReset(system:DsSystem ) =
        let stgs = system.TagManager.Storages
        let skipList =  [SystemTag._ON
                         SystemTag._OFF
                         SystemTag._T20MS
                         SystemTag._T100MS
                         SystemTag._T200MS
                         SystemTag._T1S
                         SystemTag._T2S
                         SystemTag.timeout
                         ] |> Seq.cast<int>

        let stgs = stgs.Where(fun w-> not(skipList.Contains(w.Value.TagKind)))
        for tag in stgs do
            let stg = tag.Value
            match stg with
            | :? TimerCounterBaseStruct as tc ->
                tc.ResetStruct()  // 타이머 카운터 리셋
            | _ ->
                stg.BoxedValue <- textToDataType(stg.DataType.Name).DefaultValue()

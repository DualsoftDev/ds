namespace Dual.PLC.Common.FS

open System
open System.Runtime.Serialization

type Rung = {
    Title: string
    Items: PlcTerminal array
}

module RungLogic =

    /// Coil 판별 함수
    let isCoil (t: PlcTerminal) = t.TerminalType.IsCoilType()
    
    /// Contact 판별 함수
    let isContact (t: PlcTerminal) = t.TerminalType.IsContactType() 

    /// 유효한 Rung에서 Coil만 추출 (Coil + Contact 모두 존재 시)
    let getCoils (rungs: Rung array) : seq<PlcTerminal> =
        rungs
        |> Seq.choose (fun rung ->
            if Array.exists isCoil rung.Items && Array.exists isContact rung.Items then
                Array.tryFind isCoil rung.Items
            else
                None)

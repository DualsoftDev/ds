namespace Engine.Core

open System

[<AutoOpen>]
module TimeElements =
    
    type DsTime() =
        member val AGV = 1.0f with get, set //  Average msec
        member val STD = 0.1f with get, set //  Standard Deviation msec
        member val TON = 0.0f with get, set //  On Delay msec
        member val Script:string option= None with get, set //EXTERNAL SCRIPT
        member val Path3D:string option= None with get, set //3D PATH

    type TimeParam = {
        Average: float
        StdDev: float  // Standard Deviation
        USL: float  // Upper Specification Limit
        LSL: float  // Lower Specification Limit
    }
    with
        member x.CPK = 
            let cpu = (x.USL - x.Average) / (3.0 * x.StdDev)
            let cpl = (x.Average - x.LSL) / (3.0 * x.StdDev)
            Math.Min(cpu, cpl)

        member x.ToText() = 
            $"Mean: {x.Average}, StdDev: {x.StdDev}, USL: {x.USL}, LSL: {x.LSL}, CPK: {x.CPK}"
  
    let createTimeParamUsingMeanStd average stdDev =
        // 상한과 하한을 평균 기준으로 ±3σ(6σ)로 설정합니다.
        let upsl = average + 3.0 * stdDev
        let losl = average - 3.0 * stdDev
        { Average = average; StdDev = stdDev; USL = upsl; LSL = losl }

    let createTimeParamUsingMean average =
        // 평균의 10%를 기본 표준편차로 설정합니다.
        let stdDev = average * 0.1 
        createTimeParamUsingMeanStd average stdDev 
        
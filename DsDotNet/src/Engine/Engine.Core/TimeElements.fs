namespace Engine.Core

open System

[<AutoOpen>]
module TimeElements =
    
    type DsTime() = //최소 입력단위 0.01초(10msec)
        member val AVG: float option = None with get, set //  Average  sec
        member val STD: float option = None with get, set //  Standard Deviation  sec
        member val TON: float option = None with get, set //  On Delay sec (default 0)
        member val Script:string option = None with get, set //EXTERNAL SCRIPT
        member val Motion:string option = None with get, set //3D PATH

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


    let validateDecimalPlaces name (valueSec:float)=
        let decimalPart = valueSec.ToString().Split('.').[1]
        if decimalPart.Length > 2 then
            failwithf $"Invalid time {valueSec}sec ({name}) \r\nResolution {(MinTickInterval|>float)/1000.0}sec"
    
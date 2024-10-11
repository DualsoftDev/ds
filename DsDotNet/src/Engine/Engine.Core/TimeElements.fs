namespace Engine.Core

open System

[<AutoOpen>]
module TimeElements =

    ///Real Going Time (시뮬레이션 및 CPK 계산용)    
    type DsTime() = //최소 입력단위 0.01초(10msec)
        member val AVG: float option = None with get, set //  Average  sec
        member val STD: float option = None with get, set //  Standard Deviation  sec
            //TON은  ApiTime()  CHK으로 이동
        //member val TON: float option = None with get, set //  On Delay sec (default 0)

    /// 인터페이스 에러체크용 시간(사용자 입력 or Api.Tx~Rx AVG, STD 이용하여 CPK로 계산)
    type ApiTime() = // 최소 입력단위 0.01초(10msec)
        member val MAX: float option = None with get, set // 동작시간 에러초과 sec
        member val MIN: float option = None with get, set // 동작시간 에러미달 sec
        member val CHK: float option = None with get, set // 센서고장체크 딜레이 sec

        member x.TimeOver = x.MAX |> Option.defaultValue 15.0 // 입력없으면 15초
        member x.TimeUnder = x.MIN |> Option.defaultValue 0.0 // 입력없으면 0.0초는 TimeUnder 체크안함
        member x.TimeSensorCheckDelay = x.CHK |> Option.defaultValue 0.0 // 입력없으면 0.0초는 센서 즉시 체크

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
        let decimalPart = valueSec.ToString().Split('.')
        if decimalPart.Length > 2 then
            failwithf $"Invalid time {valueSec}sec ({name}) \r\nResolution {(MinTickInterval|>float)/1000.0}sec"

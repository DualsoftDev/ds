namespace T
open NUnit.Framework
open Dual.Common.Core.FS
open Dual.UnitTest.Common.FS
open Engine.Info
open System
open Xunit
open MathNet.Numerics.Distributions

[<AutoOpen>]
module ConfidenceIntervalTestModule =
    type ConfidenceIntervalTest() =
        inherit EngineTestBaseClass()

        /// Summary 객체를 생성합니다.
        let createSummary (samples:float seq) =
            let logSet = getNull<LogSet>()
            let storageKey = getNull<StorageKey>()
            Summary(logSet, storageKey, samples)

        let compute (summary:Summary, nSigma:float): unit =
            // 신뢰 구간을 계산합니다.
            let zHalfSigma = 1.96 // 95% 신뢰 구간에 해당하는 Z-값.  Summary.ComputeZScoreFromConfidenceInterval(0.95)
            let l, u = summary.CalculateZScoreLimits(zHalfSigma)


            // 신뢰 구간 내의 실제 신뢰도를 계산합니다.
            let confidenceInterval = summary.CalculateConfidenceInterval(l, u)

            // 기대 결과와 비교합니다. 0.95 (95%)에 가까운지 확인합니다.
            Assert.True(abs(confidenceInterval - 0.95) < 0.01, $"Expected ~0.95, but got {confidenceInterval}" )

            let cpk = summary.CalculateCpk(0, 100)
            let ll, ul = summary.CalculateSpecLimitsUsingCpk(cpk, nSigma)


            tracefn $".μ={summary.μ}, σ={summary.σ}, L={l}, U={u}"
            tracefn $"Confidence Interval: {confidenceInterval}"
            tracefn $"Cpk: {cpk}, Lsl={ll}, Usl={ul}"

        [<Test>]
        member __.``Basic Test`` () =
            abs(Summary.ComputeZScoreFromConfidenceInterval(0.95) - 1.96) < 0.0001 === true

        [<Test>]
        member __.``Random Sample Test`` () =
            // 랜덤 샘플 데이터를 생성합니다.
            let random = Random()
            let sampleData = [for _ in 0 .. 999 -> random.NextDouble() * 100.0]

            let summary = createSummary(sampleData)
            compute (summary, 3.0)

        [<Test>]
        member __.``정규분포 Sample Test`` () =
            // 랜덤 샘플 데이터를 생성합니다.
            // 평균 50, 표준편차 10을 가지는 정규 분포에서 샘플 데이터를 생성합니다.
            let sampleData =
                let mean = 50.0
                let stdDev = 10.0
                let normalDist = Normal(mean, stdDev)
                [for _ in 0 .. 999 -> normalDist.Sample()]

            let summary = createSummary(sampleData)
            compute (summary, 3.0)

            compute (summary, 6.0)

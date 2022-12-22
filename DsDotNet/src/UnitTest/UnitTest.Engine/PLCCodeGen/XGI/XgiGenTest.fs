namespace UnitTest.Engine.PLC

open System.IO
open System.Reflection

open NUnit.Framework

open UnitTest.Engine
open Engine.Parser.FS
open Engine.Core
open Engine.Common.FS
open PLC.CodeGen.Common.QGraph
open PLC.CodeGen.LSXGI


[<AutoOpen>]
module XgiGenerationTestModule =

    type XgiGenerationTest() =
        do Fixtures.SetUpTest()

        let plcCodeGenerationOption =
            let n (v:IVertex) = v :?> INamed |> name
            /// 테스트 용으로 출력 신호를 따로 생성함.  e.g "Ap" --> "QAp"
            let coilGenerator          = fun (v:IVertex) -> $"O_{n v}"
            let SensorGenerator        = fun (v:IVertex) -> $"I_{n v}"
            let runningGenerator       = fun (v:IVertex) -> $"{n v}_G"
            let finishGenerator        = fun (v:IVertex) -> $"{n v}_F"
            let resetGenerator         = fun (v:IVertex) -> $"{n v}_RST"
            let readyStateGenerator    = fun (v:IVertex) -> $"{n v}_R"
            let HomingStateGenerator   = fun (v:IVertex) -> $"{n v}_H"
            let OriginStateGenerator   = fun (v:IVertex) -> $"{n v}_O"
            let goinglockNameGenerator = fun (v:IVertex) (i:int) -> $"{n v}_GL{id}"

            { createDefaultCodeGenerationOption() with
                CoilTagGenerator            = Some coilGenerator
                SensorTagGenerator          = Some SensorGenerator
                GoingStateNameGenerator     = Some runningGenerator
                StandbyStateNameGenerator   = Some readyStateGenerator
                HomingStateNameGenerator    = Some HomingStateGenerator
                OriginStateNameGenerator    = Some OriginStateGenerator
                ResetLockRelayNameGenerator = Some goinglockNameGenerator
                ResetNameGenerator          = Some resetGenerator
                RelayGenerator              = relayGenerator "RR" 1// XGI 고려한 모델 : Relay 이름 R 대신 RR
                //FinishStateNameGenerator  = Some finishGenerator
            }
        let generatePLCByModel models =
            //let delta =
            //    let p = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)
            //    Path.Combine(p, @"..\..\..\..")
            //tracefn "Delta location: %s" delta




            //let procInfos =
            //    models |> Seq.map(fun model ->
            //        let procInfo, expr =
            //            PreProcessor.PreProcessModel model
            //            |> processModelWithOption  (Some opt) []

            //        procInfo
            //    )

            //let ladderInfo =
            //    let rungs = procInfos |> Seq.collect(fun (p) -> p.LadderInfo.Rungs) |> List.ofSeq
            //    let comments = procInfos |> Seq.collect(fun (p) -> p.LadderInfo.PrologComments) |> List.ofSeq
            //    { Rungs = rungs; PrologComments = comments}

            //let status = []//processStatus model procInfos opt

            //let xml = LsXGI.generateXGIXmlFromLadderInfoAndStatus opt ladderInfo status sempty sempty None
            //// @"F:\Git\dual\soft\Delta\UnitTest\output.xml"
            //let output = Path.Combine(delta, @"UnitTest\inner.xml")
            //File.WriteAllText(output, xml)

            //let tags = ladderInfo.Rungs |> List.collect(fun r -> rungInfoToExpr r |> collectTerminals |> List.ofSeq) |> List.where(fun t -> t :? PLCTag)
            //let dtags = tags |> List.distinct

            //dtags |> List.iter(fun t -> tracefn "%A" t)
            ()



        [<Test>]
        member __.``XGI Generation test`` () =
            //generatePLCByModel
            let storages = Storages()
            let code = """
                bool myBit0 = createTag("%IX0.0.0", false);
                bool myBit1 = createTag("%IX0.0.1", false);
                bool myBit2 = createTag("%IX0.0.2", false);

                bool myBit7 = createTag("%IX0.0.7", false);

                $myBit7 := ($myBit0 || $myBit1) && $myBit2;
"""
            let statements = parseCode storages code
            storages.Count === 4
            statements.Length === 1      // createTag 는 statement 에 포함되지 않는다.   (한번 생성하고 끝나므로 storages 에 tag 만 추가 된다.)

            let xml = LsXGI.generateXml plcCodeGenerationOption storages (map withNoComment statements)
            ()



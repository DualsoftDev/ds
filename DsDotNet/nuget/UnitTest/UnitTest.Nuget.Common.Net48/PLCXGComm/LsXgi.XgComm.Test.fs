namespace T

open NUnit.Framework

open Dual.Common.Core
open Dual.Common.Base.CS
open Dual.PLC.TagParser.FS
open DsXgComm
open System
open System.Linq
open Dual.Common.Core.FS

[<AutoOpen>]
module LsXgiXgCommTestModule =

    [<TestFixture>]
    type LsXgiXgCommTest() =

        let ranges = 
            [ LsXgiTagParserModule.dataRangeDic["X"], LsXgiTagParserModule.dataUtypeRangeDic["X"]
              LsXgiTagParserModule.dataRangeDic["B"], LsXgiTagParserModule.dataUtypeRangeDic["B"]
              LsXgiTagParserModule.dataRangeDic["W"], LsXgiTagParserModule.dataUtypeRangeDic["W"]
              LsXgiTagParserModule.dataRangeDic["D"], LsXgiTagParserModule.dataUtypeRangeDic["D"]
              LsXgiTagParserModule.dataRangeDic["L"], LsXgiTagParserModule.dataUtypeRangeDic["L"] ]

        let createTags prefix range utypeRange =
            [ 0..127 ]
            |> List.collect (fun d -> [
                $"%%I{prefix}{d}"
                $"%%I{prefix}{d/range/16}.{d/range%16}.{d%range}"
                $"%%Q{prefix}{d}"
                $"%%Q{prefix}{d/range/16}.{d/range%16}.{d%range}"
                $"%%U{prefix}{d/utypeRange/16}.{d/utypeRange%16}.{d%utypeRange}"
                $"%%M{prefix}{d}"
                $"%%L{prefix}{d}"
                $"%%N{prefix}{d}"
                $"%%K{prefix}{d}"
                $"%%R{prefix}{d}"
                $"%%A{prefix}{d}"
                $"%%W{prefix}{d}"
                $"%%F{prefix}{d}"
            ])

        let runTest prefix range utypeRange calculateValue expectedValues =
            if isXgCommAvailable then
                let tags = createTags prefix range utypeRange |> startScan
                let tags = tags.Values
                tags 
                |> Seq.filter (fun t -> not (t.TagName.StartsWith("%F")))
                |> Seq.iter (fun t -> t.SetWriteValue(calculateValue t.BitOffset range))
                waitReadWriteTags tags

                expectedValues
                |> List.iter (fun (tagName:string, expectedValue) -> 
                    let tag = tags.First(fun t -> t.TagName.StartsWith(tagName))
                    Assert.That(tag.Value, Is.EqualTo(expectedValue))
                    )

            else ()

        [<Test>]
        member _.``Test XGI Bit Tag``() =
            runTest "X" (fst (List.head ranges)) (snd (List.head ranges)) (fun offset _ -> offset % 2) 
                [ "%MX0", false; "%MX1", true; "%MX2", false ]

        [<Test>]
        member _.``Test XGI Byte Tag``() =
            runTest "B" (fst (List.item 1 ranges)) (snd (List.item 1 ranges)) (fun offset range -> byte (offset / (64 / range))) 
                [ "%MB1", 1uy; "%MB2", 2uy ]

        [<Test>]
        member _.``Test XGI Word Tag``() =
            runTest "W" (fst (List.item 2 ranges)) (snd (List.item 2 ranges)) (fun offset range -> uint16 (offset / (64 / range))) 
                [ "%MW1", 1us; "%MW2", 2us ]

        [<Test>]
        member _.``Test XGI DWord Tag``() =
            runTest "D" (fst (List.item 3 ranges)) (snd (List.item 3 ranges)) (fun offset range -> uint32 (offset / (64 / range))) 
                [ "%MD1", 1u; "%MD2", 2u ]

        [<Test>]
        member _.``Test XGI LWord Tag``() =
            runTest "L" (fst (List.item 4 ranges)) (snd (List.item 4 ranges)) (fun offset range -> uint64 (offset / (64 / range))) 
                [ "%ML1", 1UL; "%ML2", 2UL; "%RL1", 1UL; "%RL2", 2UL ]

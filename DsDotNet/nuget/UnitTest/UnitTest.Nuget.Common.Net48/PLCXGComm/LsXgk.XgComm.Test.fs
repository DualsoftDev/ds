namespace T

open NUnit.Framework

open Dual.Common.Core
open Dual.Common.Base.CS
open Dual.PLC.TagParser.FS
open DsXgComm
open System
open System.Linq

[<AutoOpen>]
module LsXgkXgCommTestModule =

    [<TestFixture>]
    type LsXgkXgCommTest() =
        let tagWordXGKs = 
            [0..127]
            |> List.collect (fun i -> 
                [
                    $"M{i:D4}"
                    $"P{i:D4}"
                    $"K{i:D4}"
                    $"F{i:D4}"
                    $"L{i:D5}"
                    $"T{i}"
                    $"C{i}"
                    $"Z{i}"
                    $"N{i}"
                    $"D{i}"
                    $"R{i}"
                    $"ZR{i}"
                ])

        let tagBitXGKs = 
            [0..127]
            |> List.collect (fun i -> 
                let wordPart = i / 16
                let bitPart = i % 16
                [
                    $"M{wordPart:D4}{bitPart:X}"
                    $"P{wordPart:D4}{bitPart:X}"
                    $"K{wordPart:D4}{bitPart:X}"
                    $"F{wordPart:D4}{bitPart:X}"
                    $"L{wordPart:D5}{bitPart:X}"
                    $"N{wordPart}.{bitPart:X}"
                    $"D{wordPart}.{bitPart:X}"
                    $"R{wordPart}.{bitPart:X}"
                    $"ZR{wordPart}.{bitPart:X}"
                ])


        [<Test>]
        member _.``Test XGK Word Tag``() =
            if isXgCommAvailable  
            then 
                let tags = startScan tagWordXGKs
                let tags = tags.Values
                tags |> Seq.iter (fun t -> t.SetWriteValue(t.BitOffset/16)) 
                waitReadWriteTags tags

                let m1 = tags.First(fun t->t.TagName.StartsWith("M0001"))
                let m2 = tags.First(fun t->t.TagName.StartsWith("M0002"))
                Assert.That(m1.Value, Is.EqualTo(1us))
                Assert.That(m2.Value, Is.EqualTo(2us))
            else 
                ()


        [<Test>]
        member _.``Test XGK Bit Tag``() =
            if isXgCommAvailable  
            then 
                let tags = startScan tagBitXGKs
                let tags = tags.Values
                tags |> Seq.iter (fun t -> t.SetWriteValue(t.BitOffset%2)) 
                waitReadWriteTags tags

                let m0 = tags.First(fun t->t.TagName.StartsWith("M00000"))
                let m1 = tags.First(fun t->t.TagName.StartsWith("M00001"))
                let m2 = tags.First(fun t->t.TagName.StartsWith("M00002"))
                Assert.That(m0.Value, Is.EqualTo(false))
                Assert.That(m1.Value, Is.EqualTo(true))
                Assert.That(m2.Value, Is.EqualTo(false))
            else 
                ()
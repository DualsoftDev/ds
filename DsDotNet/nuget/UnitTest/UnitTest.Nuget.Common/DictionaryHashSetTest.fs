namespace T

open NUnit.Framework

open Dual.Common.Core.FS
open Dual.Common.UnitTest.FS
open System.Collections.Generic


[<AutoOpen>]
module DictionaryHashSetTestModule =

    [<TestFixture>]
    type HashSetTest() =
        [<Test>]
        member _.``Dupliate insert Test``() =
            let hash = HashSet([1..10])
            hash.Add(11) === true
            hash.Add(1) === false

            ()
    [<TestFixture>]
    type DynamicDictionaryTest() =
        [<Test>]
        member _.``DynamicDictionary Test``() =
            let dd = DynamicDictionary()

            dd.Set<Person>(p1)
            dd.TryGet<Person>() === Some p1

            dd.Set<Person[]>([| p1; p2 |])
            dd.TryGet<Person[]>() === Some [| p1; p2 |]
            ()

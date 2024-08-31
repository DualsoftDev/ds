namespace T

open Dual.Common.Core.FS
open Dual.Common.UnitTest.FS
open NUnit.Framework
open System

[<AutoOpen>]
module DiscriminatedUnionTestModule =
    [<Flags>]
    type Dog =
        | Bulldog = 1
        | Shepherd = 2
        | Husky = 3
        | Poodle = 4
        | Dalmatian = 5
        | Terrier = 6
        | Retriever = 7
    [<Flags>]
    type Cat =
        | Siamese = 11        // 시암
        | Persian = 12        // 페르시안
        | MaineCoon = 13      // 메인쿤
        | Ragdoll = 14        // 래그돌
        | Bengal = 15         // 벵갈
        | Sphynx = 16         // 스핑크스
        | BritishShorthair = 17 // 브리티시 쇼트헤어


    //let tryGetEnumValue<'T when 'T: (new: unit -> 'T) and 'T: struct and 'T :> ValueType> (value:int) =
    //    try
    //        if typeof<'T>.GetEnumValues() |> Seq.cast<int> |> Seq.contains(value) then
    //            Enum.ToObject(typeof<'T>, value) :?> 'T |> Some
    //        else
    //            None
    //    with ex ->
    //        None
    
    [<TestFixture>]
    type DiscriminatedUnionTest() =

        [<Test>]
        member _.TestTryGetEnumValue() =
            DU.tryGetEnumValue<Dog>(1) === Some Dog.Bulldog
            DU.tryGetEnumValue<Dog>(7) === Some Dog.Retriever
            DU.tryGetEnumValue<Dog>(99) === None
            DU.tryGetEnumValue<Cat>(99) === None

            DU.tryGetEnumValue<Cat>(11) === Some Cat.Siamese
            DU.tryGetEnumValue<Cat>(12) === Some Cat.Persian

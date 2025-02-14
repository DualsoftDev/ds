namespace T

open NUnit.Framework

open Dual.Common.Core.FS
open Dual.Common.UnitTest.FS


[<AutoOpen>]
module ExtensionTestModule =

    type IFruit = abstract member Name:string

    type Fruit(name) =
        interface IFruit with
            member x.Name = name

    type Apple(name) = inherit Fruit(name)
    type Banana(name) = inherit Fruit(name)

    [<TestFixture>]
    type SomeCoreTest() =
        [<Test>]
        member _.``Option cast Test``() =
            let apple: Apple option = Some (Apple("홍옥"))
            let fruit: IFruit option = apple.Cast<IFruit>()
            fruit.Value.Name === "홍옥"

            let banana: Banana option = Some (Banana("바나나"))
            let fruit: IFruit option = banana.Cast<IFruit>()
            fruit.Value.Name === "바나나"

            let n = Some 1
            let d = n.Cast<double>()
            d.Value.GetType().Name === "Double"
            d.Value === 1.0

            ()

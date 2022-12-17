namespace UnitTest.Engine.Expression

open NUnit.Framework

open Engine.Parser.FS
open UnitTest.Engine
open Engine.Core

[<AutoOpen>]
module DeclarationTestModule =

    type DeclarationTest() =
        do Fixtures.SetUpTest()

        [<Test>]
        member __.``Variable Declaration test`` () =
            let storages = Storages()
            let t1 = PlcTag("my_counter_control_tag", "%M1.1", false)
            let tag1 = PlcTag("tag1", "%M1.1", false)
            let tag2 = PlcTag("tag2", "%M1.1", false)
            let tag3 = PlcTag("tag3", "%M1.1", false)
            do
                for t in [ t1 :> IStorage; tag1; tag2; tag3 ] do
                    storages.Add(t.Name, t)

            let percent = "%"
            let statementTexts = [
                "ctu myCtu1 = createCTU(100us, false)"
                $"int8 myByte = createTag({dq}{percent}M9.9{dq}, 123y)"
                "ton myton1 = createTON(1000us, $tag1 || $tag2)"
                "ton myton2 = createTON(1000us, $tag1 || $tag2, $tag3)"
            ]
            for s in statementTexts do
                let statement = parseStatement storages s
                match statement with
                | Some stmt -> stmt.ToText() === s
                | None -> ()

            storages.ContainsKey("myByte") === true
            storages["myByte"].Value === 123y
            storages["myByte"].DataType === typedefof<int8>
            let myByte = storages["myByte"] :?> PlcTag<int8>
            myByte.Address === "%M9.9"
            ()



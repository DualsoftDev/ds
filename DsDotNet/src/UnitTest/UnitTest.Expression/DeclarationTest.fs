namespace T.Expression
open Dual.Common.UnitTest.FS

open NUnit.Framework

open Engine.Parser.FS
open T
open Engine.Core

//[<AutoOpen>]
//module DeclarationTestModule =

    type DeclarationTest() =
        inherit ExpressionTestBaseClass()

        [<Test>]
        member __.``Variable Declaration test`` () =
            use _ = setRuntimeTarget AB
            let storages = Storages()
            let t1 = createTag("my_counter_control_tag", "%M1.1", false)
            let tag1 = createTag("tag1", "%M1.1", false)
            let tag2 = createTag("tag2", "%M1.1", false)
            let tag3 = createTag("tag3", "%M1.1", false)
            do
                for t in [ t1 :> IStorage; tag1; tag2; tag3 ] do
                    storages.Add(t.Name, t)

            let percent = "%"
            let statementTexts = [
                "ctu myCtu1 = createAbCTU(100u, false);"
                $"int8 myByte = createTag({dq}{percent}M9.9{dq}, 123y);"
                "ton myton1 = createAbTON(1000u, $tag1 || $tag2);"
                //"ton myton2 = createWinTON(1000us, $tag1 || $tag2, $tag3)"
            ]
            for s in statementTexts do
                let statement = tryParseStatement4UnitTest AB storages s
                match statement with
                | Some stmt -> stmt.ToText() === s
                | None -> ()

            storages.ContainsKey("myByte") === true
            storages["myByte"].BoxedValue === 123y
            storages["myByte"].DataType === typedefof<int8>
            let myByte = storages["myByte"] :?> Tag<int8>
            myByte.Address === "%M9.9"
            ()



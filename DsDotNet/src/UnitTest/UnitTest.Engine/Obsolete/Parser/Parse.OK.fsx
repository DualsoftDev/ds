namespace T


open Engine
open NUnit.Framework

[<AutoOpen>]
module ParseOKTest =
    type OK() =
        inherit EngineTestBaseClass()

        let checkParseOK(text:string, activeCpu:string) =
            ParserTest.Test(text, activeCpu)


        [<Test>] member __.``Safety`` () = checkParseOK(ParserTest.Safety, "Cpu")

        [<Test>] member __.``StrongCausal``  () = checkParseOK(ParserTest.StrongCausal, "Cpu")
        [<Test>] member __.``Buttons``       () = checkParseOK(ParserTest.Buttons, "Cpu")
        [<Test>] member __.``Dup``           () = checkParseOK(ParserTest.Dup, "Cpu")
        [<Test>] member __.``Ppt``           () = checkParseOK(ParserTest.Ppt, "Cpu")
        [<Test>] member __.``QualifiedName`` () = checkParseOK(ParserTest.QualifiedName, null)
        [<Test>] member __.``Aliases``       () = checkParseOK(ParserTest.Aliases, null)
        [<Test>] member __.``Serialize``     () = checkParseOK(ParserTest.Serialize, "Cpu")
        [<Test>] member __.``ExternalSegmentCall`` () = checkParseOK(ParserTest.ExternalSegmentCall, null)
        [<Test>] member __.``ExternalSegmentCallConfusing`` () = checkParseOK(ParserTest.ExternalSegmentCallConfusing, null)
        [<Test>] member __.``Error``         () = checkParseOK(ParserTest.Error, "Cpu")

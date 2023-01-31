namespace T


open Xunit
open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Engine.Common.FS
open PLC.CodeGen.LSXGI


type XgiExistingXmlProjectAnalTest() =
    inherit XgiTestBaseClass()


    [<Test>]
    member __.``Xml project read global variable test`` () =
        let xmlPrj = $"{__SOURCE_DIRECTORY__}/../../PLC/PLC.CodeGen.LSXGI/Documents/multiProgramSample.xml"

        analyzeXmlProject xmlPrj

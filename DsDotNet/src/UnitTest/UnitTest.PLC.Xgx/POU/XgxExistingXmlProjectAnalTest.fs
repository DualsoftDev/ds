namespace T.POU
open T

open Dual.Common.UnitTest.FS

open NUnit.Framework

open Engine.Core
open Dual.Common.Core.FS
open PLC.CodeGen.LS


type XgxExistingXmlProjectAnalTest() =
    inherit XgxTestBaseClass(XGI)


    [<Test>]
    member __.``Xml project read global variable test`` () =
        let xmlPrj = $"{__SOURCE_DIRECTORY__}/../../../PLC/PLC.CodeGen.LS/Documents/XmlSamples/multiProgramSample.xml"

        let usedMAddresses =
            DualXmlDocument.loadFromFile xmlPrj
            |> (fun xdoc -> xdoc.SelectMultipleNodes "//Configurations/Configuration/GlobalVariables/GlobalVariable/Symbols/Symbol") |> List.ofSeq
            |> map xmlSymbolNodeToSymbolInfo
            |> map address
            |> filter notNullAny
            |> filter (fun addr -> addr.StartsWith("%M"))

        usedMAddresses |> SeqEq [ "%MX0"; "%MX1"; "%MX8"; "%MX33"; "%MB2"; "%MB17"; "%ML1" ]

        // used Memory Indices :
        usedMAddresses
        |> collectByteIndices XGI
        |> SeqEq [ 0; 1; 2; 4; 8; 9; 10; 11; 12; 13; 14; 15; 17; ]

        ()

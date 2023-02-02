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

        let xdoc = XmlDocument.loadFromFile xmlPrj
        let xnGlobalSymbols = xdoc.SelectMultipleNodes "//Configurations/Configuration/GlobalVariables/GlobalVariable/Symbols/Symbol" |> List.ofSeq
        let globalsWithAddress = xnGlobalSymbols |> map xmlSymbolNodeToSymbolInfo |> filter (fun symbolInfo -> symbolInfo.Address.NonNullAny())
        let globalsWithMAreaAddress = globalsWithAddress |> filter (fun symbolInfo -> symbolInfo.Address.StartsWith("%M"))
        let usedMAddresses = globalsWithMAreaAddress |> map (fun symbolInfo -> symbolInfo.Address)

        usedMAddresses |> SeqEq [ "%MX0"; "%MX1"; "%MX8"; "%MX33"; "%MB2"; "%MB17"; "%ML1" ]

        let usedMemoryIndices = usedMAddresses |> collectByteIndices
        usedMemoryIndices |> SeqEq [ 0; 1; 2; 4; 8; 9; 10; 11; 12; 13; 14; 15; 17; ]

        ()

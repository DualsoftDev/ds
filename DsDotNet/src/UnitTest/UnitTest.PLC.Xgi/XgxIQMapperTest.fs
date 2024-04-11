namespace T

open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Dual.Common.Core.FS
open PLC.CodeGen.LS
open PLC.CodeGen.Common


type IQMapperTest() =
    inherit XgxTestBaseClass()


    [<Test>]
    member __.``Dummy IQ Map test`` () =
        let globalStorages = Storages()
        let pouIQMap =
            let code = """
                bool qAvante = false;
                bool qSonata = false;
                bool QCar = createTag("%QX1", false);
                $QCar := $qAvante || $qSonata;

                bool iSonata = false;
                bool iAvante = false;
                bool ICar = createTag("%IX1", false);
                $iAvante := $ICar;
                $iSonata := $ICar;
    """
            let statements = parseCode globalStorages code |> map withNoComment
            {
                TaskName = "Scan Program"
                POUName = "POU1"
                Comment = "POU1"
                LocalStorages = Storages()
                GlobalStorages = globalStorages
                CommentedStatements = statements
            }

        let prjParam = {
            defaultXgxProjectParams with
                TargetType = TestRuntimeTargetType
                ProjectName = "Dummy IQ Map test"
                GlobalStorages = globalStorages
                POUs = [pouIQMap]
                RungCounter = counterGenerator 0 |> Some
        }

        let xml = prjParam.GenerateXmlString()
        let f = getFuncName()
        saveTestResult f xml

namespace T

open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Dual.Common.Core.FS
open PLC.CodeGen.LS
open PLC.CodeGen.Common


type IQMapperTest(xgx:PlatformTarget) =
    inherit XgxTestBaseClass(xgx)

    member x.``Dummy IQ Map test`` () =
        let globalStorages = Storages()
        let pouIQMap =
            let codes =
                let code = """
                bool qAvante = false;
                bool qSonata = false;

                $QCar = $qAvante || $qSonata;

                bool iSonata = false;
                bool iAvante = false;

                $iAvante = $ICar;
                $iSonata = $ICar;
                """
                let iq = if xgx = XGI then """bool QCar = createTag("%QX1.0.1", false);bool ICar = createTag("%IX1.0.1", false);"""
                          elif xgx = XGK then """bool QCar = createTag("P0000F", false);bool ICar = createTag("P0010F", false);"""
                          else failwithf $"not support {xgx}"

                iq+code

            let statements = parseCodeForTarget globalStorages codes xgx|> map withNoComment
            {
                TaskName = "Scan Program"
                POUName = "POU1"
                Comment = "POU1"
                LocalStorages = Storages()
                GlobalStorages = globalStorages
                CommentedStatements = statements
            }

        let prjParam = {
            getXgxProjectParams xgx (getFuncName()) with
                GlobalStorages = globalStorages
                EnableXmlComment = true
                POUs = [pouIQMap]
        }

        let xml = prjParam.GenerateXmlString()
        let f = getFuncName()
        x.saveTestResult f xml


type XgiIQMapperTest() =
    inherit IQMapperTest(XGI)
    [<Test>] member __.``Dummy IQ Map test`` () = base.``Dummy IQ Map test``()


type XgkIQMapperTest() =
    inherit IQMapperTest(XGK)
    [<Test>] member __.``Dummy IQ Map test`` () = base.``Dummy IQ Map test``()

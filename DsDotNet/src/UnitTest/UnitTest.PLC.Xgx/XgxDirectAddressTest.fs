namespace T

open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Dual.Common.Core.FS
open Engine.CodeGenPLC
open Dual.Common.UnitTest.FS


type XgxDirectAddressTest(xgx:PlatformTarget) =
    inherit XgxTestBaseClass(xgx)
    //ADV, RET 주소 같음
    let code (addBoolIn, addInt16Out) =
        $"""
            [sys] HelloDS = {{
                [flow] f1 = {{
                    Work1 = {{
                        Device1.ADV(true : 500us) > Device1.RET(true : 500us);
                    }}
                }}
                [jobs] = {{
                    f1.Device1.ADV = {{"f1-Device1".ADV ({addBoolIn}, {addInt16Out};UInt16);}}  
                    f1.Device1.RET = {{"f1-Device1".RET ({addBoolIn}, {addInt16Out};UInt16);}}  
                }}
                [prop] = {{
                    [layouts] = {{
                        "f1-Device1" = (979, 460, 220, 80);
                    }}
                }}
                [device file="./dsLib/Cylinder/DoubleCylinder.ds"] "f1-Device1";
                [versions] = {{
                    DS-Langugage-Version = 1.0.0.1;
                    DS-Engine-Version = 0.9.9.6;
                }}
            }}
        """

    member x.``Xgx DirectAddress  plc gen`` () =
        let systemRepo = ShareableSystemRepository()
        let libPath = @$"{__SOURCE_DIRECTORY__}/../../UnitTest.Model/UnitTestExample/dsSimple"
        let code = if xgx = XGI then code("%IX0.0.0", "%IW0") else code("P0000F", "P0001")

        let sys = ModelParser.ParseFromString( code,
            ParserOptions.Create4Simulation(systemRepo, libPath, "ActiveCpuNA.me", None, DuNone)
            )
        let param =  XgxGenerationParameters.Default()
        param.PlatformTarget <- xgx
            
        exportXMLforLSPLC (sys, "XXXXXXXXX", param)

type XgiDirectAddressTest() =
    inherit XgxDirectAddressTest(XGI)
    [<Test>] member __.``Xgi DirectAddress  plc gen`` () = base.``Xgx DirectAddress  plc gen``()

type XgkDirectAddressTest() =
    inherit XgxDirectAddressTest(XGK)
    [<Test>] member __.``Xgk DirectAddress  plc gen`` () = base.``Xgx DirectAddress  plc gen``()

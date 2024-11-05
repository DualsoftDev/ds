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
                        Device1."ADV(INTrue_OUT300)" > Device1."RET(INTrue_OUT500)";
                    }}
                }}
                [jobs] = {{
                    f1.Device1."ADV(INTrue_OUT300)" = ({addBoolIn}:Boolean:True, {addInt16Out}:Int16:300s);
                    f1.Device1."RET(INTrue_OUT500)" = ({addBoolIn}:Boolean:True, {addInt16Out}:Int16:500s);
                }}
                [prop] = {{
                    [layouts] = {{
                        f1__Device1 = (979, 460, 220, 80);
                    }}
                }}
                [device file="./dsLib/Cylinder/DoubleCylinder.ds"] f1__Device1;
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

        exportXMLforLSPLC(xgx, sys, "XXXXXXXXX", null,  0, 0, true, 0)

type XgiDirectAddressTest() =
    inherit XgxDirectAddressTest(XGI)
    [<Test>] member __.``Xgi DirectAddress  plc gen`` () = base.``Xgx DirectAddress  plc gen``()

type XgkDirectAddressTest() =
    inherit XgxDirectAddressTest(XGK)
    [<Test>] member __.``Xgk DirectAddress  plc gen`` () = base.``Xgx DirectAddress  plc gen``()

namespace T

open NUnit.Framework
open Dual.Common.Core.FS
open Engine.Core
open Engine.Parser.FS

type XgxExtendedTypesTest(xgx:PlatformTarget) =
    inherit XgxTestBaseClass(xgx)

    let span width = width*3



    member x.``Local vars type test`` () =
        let storages = Storages()
        let code = """
            bool    mybool   = false;
            single  mysingle = 0.1f;
            double  mypi = 3.14;
            double  myeuler = 2.718;
            sbyte   mysbyte  = 1y;
            char    mychar   = 'a';
            byte    mybyte   = 2uy;
            int16   myint16  = 16s;
            uint16  myuint16 = 16us;
            int32   myint32  = 32;
            uint32  myuint32 = 32u;
            int64   myint64  = 64L;
            uint64  myuint64 = 64UL;

            double myDoubleSum1 = 0.0;
            double myDoubleSum2 = 0.0;
            double myDoubleSum3 = $mypi + $myeuler;
            double myDoubleSum4 = 3.14 + 2.718;
            $myDoubleSum1 := $myDoubleSum2;
            $myDoubleSum2 := $myDoubleSum1 + $myDoubleSum3;
"""
        let statements = parseCodeForWindows storages code
        let f = getFuncName()
        let xml = x.generateXmlForTest f storages (map withNoComment statements)
        x.saveTestResult f xml



type XgiExtendedTypesTest() =
    inherit XgxExtendedTypesTest(XGI)
    [<Test>] member x.``Local vars type test``() = base.``Local vars type test``()

type XgkExtendedTypesTest() =
    inherit XgxExtendedTypesTest(XGK)
    [<Test>] member x.``Local vars type test``() = base.``Local vars type test``()


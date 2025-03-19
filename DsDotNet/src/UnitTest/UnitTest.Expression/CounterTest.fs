namespace T.Statement
open Dual.Common.UnitTest.FS

open NUnit.Framework

open T
open Engine.Core
open T.Expression
open Dual.Common.Core.FS
open Engine.Parser.FS
open PLC.CodeGen.LS


//[<AutoOpen>]
//module CounterTestModule =

    type CounterTest() =
        inherit ExpressionTestBaseClass()
        do
            let ``강제 reference 추가용`` = XGITag.createSymbolInfo
            ()


        let evaluateRungInputs (counter:Counter) =
            for s in counter.InputEvaluateStatements do
                s.Do()

        [<Test>]
        member __.``CTU creation test`` () =
            use _ = setRuntimeTarget WINDOWS
            let storages = Storages()
            let t1 = createTag("my_counter_control_tag", "%M1.1", false)
            let condition = var2expr t1
            let tcParam = {Storages=storages; Name="myCTU"; Preset=100u; RungInCondition=condition; FunctionName="createWinCTU"}
            let ctu = CounterStatement.CreateAbCTU(tcParam) ExpressionFixtures.runtimeTarget|> toCounter
            ctu.OV.Value === false
            ctu.UN.Value === false
            ctu.DN.Value === false
            ctu.PRE.Value === 100u
            ctu.ACC.Value === 0u


            (* Counter struct 의 내부 tag 들이 생성되고, 등록되었는지 확인 *)
            //let internalTags =
            //    [
            //        // CTU 및 CTD 에서는 .CU 와 .CD tag 는 internal 로 숨겨져 있다.
            //        ctu.OV :> IStorage
            //        ctu.UN
            //        ctu.DN
            //        ctu.PRE
            //        ctu.ACC
            //        ctu.RES
            //    ]

            //storages.ContainsKey("myCTU") === true
            //for t in internalTags do
            //    storages.ContainsKey(t.Name) === true


            for i in [1..50] do
                t1.Value <- true
                evaluateRungInputs ctu
                ctu.ACC.Value === uint32 i
                t1.Value <- false
                evaluateRungInputs ctu
                ctu.DN.Value === false

            ctu.ACC.Value === 50u
            ctu.DN.Value === false
            for i in [51..100] do
                t1.Value <- true
                evaluateRungInputs ctu
                ctu.ACC.Value === uint32 i
                t1.Value <- false
                evaluateRungInputs ctu
            ctu.ACC.Value === 100u
            ctu.DN.Value === true

     

        [<Test>]
        member __.``CTU with reset creation test`` () =
            use _ = setRuntimeTarget WINDOWS
            let storages = Storages()
            let t1 = createTag("my_counter_control_tag", "%M1.1", false)
            let resetTag = createTag("my_counter_reset_tag", "%M1.1", false)
            let condition = var2expr t1
            let reset = var2expr resetTag
            let tcParam = {Storages=storages; Name="myCTU"; Preset=100u; RungInCondition=condition; FunctionName="createWinCTU"}
            let ctu = CounterStatement.CreateCTU(tcParam, reset) ExpressionFixtures.runtimeTarget|> toCounter
            ctu.OV.Value === false
            ctu.UN.Value === false
            ctu.DN.Value === false
            ctu.RES.Value === false
            ctu.PRE.Value === 100u
            ctu.ACC.Value === 0u


            for i in [1..50] do
                t1.Value <- true
                evaluateRungInputs ctu
                ctu.ACC.Value === uint32 i
                t1.Value <- false
                evaluateRungInputs ctu
                ctu.DN.Value === false

            ctu.ACC.Value === 50u
            ctu.DN.Value === false

            // counter reset
            resetTag.Value <- true
            evaluateRungInputs ctu
            ctu.OV.Value === false
            ctu.UN.Value === false
            ctu.DN.Value === false
            ctu.RES.Value === true
            ctu.PRE.Value === 100u
            ctu.ACC.Value === 0u


        [<Test>]
        member __.``CTR with reset creation test`` () =
            use _ = setRuntimeTarget WINDOWS
            let storages = Storages()
            let t1 = createTag("my_counter_control_tag", "%M1.1", false)
            let resetTag = createTag("my_counter_reset_tag", "%M1.1", false)
            let condition = var2expr t1
            let reset = var2expr resetTag
            let tcParam = {Storages=storages; Name="myCTR"; Preset=100u; RungInCondition=condition; FunctionName="createWinCTR"}
            let ctr = CounterStatement.CreateXgiCTR(tcParam, reset) ExpressionFixtures.runtimeTarget |> toCounter
            ctr.OV.Value === false
            ctr.UN.Value === false
            ctr.DN.Value === false
            ctr.RES.Value === false
            ctr.PRE.Value === 100u
            ctr.ACC.Value === 0u


            for i in [1..50] do
                t1.Value <- true
                evaluateRungInputs ctr
                ctr.ACC.Value === uint32 i
                t1.Value <- false
                evaluateRungInputs ctr
                ctr.DN.Value === false
            ctr.ACC.Value === 50u
            ctr.DN.Value === false

            for i in [51..99] do
                t1.Value <- true
                evaluateRungInputs ctr
                ctr.ACC.Value === uint32 i
                t1.Value <- false
                evaluateRungInputs ctr
                ctr.DN.Value === false

            ctr.ACC.Value === 99u
            ctr.DN.Value === false

            t1.Value <- true        // last straw that broken ...
            evaluateRungInputs ctr
            ctr.ACC.Value === 100u
            ctr.DN.Value === true

            // counter preset + 1 : ring counter : auto reset
            t1.Value <- false
            evaluateRungInputs ctr
            t1.Value <- true
            evaluateRungInputs ctr
            ctr.ACC.Value === 1u
            ctr.DN.Value === false




            // force counter reset
            resetTag.Value <- true
            evaluateRungInputs ctr
            ctr.OV.Value === false
            ctr.UN.Value === false
            ctr.DN.Value === false
            ctr.RES.Value === true
            ctr.PRE.Value === 100u
            ctr.ACC.Value === 0u





        [<Test>]
        member x.``CTU on XGI platform test`` () =
            use _ = setRuntimeTarget XGI
            let storages = Storages()
            let code = """
                bool cu = createTag("%MX0", false);
                bool r  = createTag("%MX1", false);
                ctu myCTU = createXgiCTU(2000u, $cu, $r);
"""

            let statement = parseCodeForTarget storages code XGI
            [ "CU"; "Q"; "PV"; "CV"; "R"; ] |> iter (fun n -> storages.ContainsKey($"myCTU.{n}") === true)
            [ "DN"; "PRE"; "ACC"; ] |> iter (fun n -> storages.ContainsKey($"myCTU.{n}") === false)

        [<Test>]
        member x.``CTD on WINDOWS platform test`` () =
            use _ = setRuntimeTarget WINDOWS
            let storages = Storages()
            let code = """
                bool cd = true;
                bool ld = false;
                ctd myCTD = createWinCTD(2000u, $cd, $ld);
"""

            let statement = parseCodeForWindows storages code
            [ "CD"; "LD"; "PV"; "Q"; "CV"; ] |> iter (fun n -> storages.ContainsKey($"myCTD.{n}") === true)
            [ "DN"; "OV"; "UN"; "PRE"; "ACC"; ] |> iter (fun n -> storages.ContainsKey($"myCTD.{n}") === false)

        [<Test>]
        member x.``CTD on WINDOWS, XGI platform test`` () =
            use _ = setRuntimeTarget XGI
            let storages = Storages()
            let code = """
                bool cd = createTag("%MX0", false);
                bool ld = createTag("%MX1", false);
                ctd myCTD = createXgiCTD(2000u, $cd, $ld);
"""

            let statement = parseCodeForTarget storages code XGI
            [ "CD"; "LD"; "PV"; "Q"; "CV"; ] |> iter (fun n -> storages.ContainsKey($"myCTD.{n}") === true)
            [ "DN"; "PRE"; "ACC"; ] |> iter (fun n -> storages.ContainsKey($"myCTD.{n}") === false)


        [<Test>]
        member x.``CTUD on WINDOWS, XGI platform test`` () =
            for platform in [WINDOWS; XGI] do
                use _ = setRuntimeTarget platform
                let storages = Storages()
                let code = """
                    bool cu = false;
                    bool cd = false;
                    bool r__ = false; // 'r'
                    bool ld = false;
                    ctud myCTUD = createWinCTUD(2000u, $cu, $cd, $r__, $ld);
    """

                let statement = parseCodeForWindows storages code
                [ "CU"; "CD"; "R"; "LD"; "PV"; "QU"; "QD"; "CV";] |> iter (fun n -> storages.ContainsKey($"myCTUD.{n}") === true)
                [ "DN"; "OV"; "UN"; "PRE"; "ACC"; "RES"; "PT"; "ET"; ] |> iter (fun n -> storages.ContainsKey($"myCTUD.{n}") === false)



        [<Test>]
        member x.``CTR on WINDOWS, XGI platform test`` () =
            for platform in [WINDOWS; XGI] do
                use _ = setRuntimeTarget platform
                let storages = Storages()
                let code = """
                    bool cd = createTag("%IX0.0.0", false);
                    bool rst = createTag("%QX0.0.1", false);
                    ctr myCTR = createXgiCTR(2000u, $cd, $rst);
    """

                let statement = parseCodeForTarget storages code XGI
                [ "CD"; "PV"; "RST"; "Q"; "CV"; ] |> iter (fun n -> storages.ContainsKey($"myCTR.{n}") === true)
                [ "CU"; "DN"; "LD"; "PRE"; "ACC"; ] |> iter (fun n -> storages.ContainsKey($"myCTR.{n}") === false)
namespace T.Statement

open NUnit.Framework

open T
open Engine.Core
open T.Expression
open Engine.Common.FS
open Engine.Parser.FS


//[<AutoOpen>]
//module CounterTestModule =

    type CounterTest() =
        do Fixtures.SetUpTest()

        let evaluateRungInputs (counter:Counter) =
            for s in counter.InputEvaluateStatements do
                s.Do()

        [<Test>]
        member __.``CTU creation test`` () =
            use _ = setRuntimeTarget AB
            let storages = Storages()
            let t1 = PlcTag("my_counter_control_tag", "%M1.1", false)
            let condition = var2expr t1
            let tcParam = {Storages=storages; Name="myCTU"; Preset=100us; RungInCondition=condition; FunctionName="createWinCTU"}
            let ctu = CounterStatement.CreateCTU(tcParam) |> toCounter
            ctu.OV.Value === false
            ctu.UN.Value === false
            ctu.DN.Value === false
            ctu.PRE.Value === 100us
            ctu.ACC.Value === 0us


            (* Counter struct 의 내부 tag 들이 생성되고, 등록되었는지 확인 *)
            let internalTags =
                [
                    // CTU 및 CTD 에서는 .CU 와 .CD tag 는 internal 로 숨겨져 있다.
                    ctu.OV :> IStorage
                    ctu.UN
                    ctu.DN
                    ctu.PRE
                    ctu.ACC
                    ctu.RES
                ]

            storages.ContainsKey("myCTU") === true
            for t in internalTags do
                storages.ContainsKey(t.Name) === true


            for i in [1..50] do
                t1.Value <- true
                evaluateRungInputs ctu
                ctu.ACC.Value === uint16 i
                t1.Value <- false
                evaluateRungInputs ctu
                ctu.DN.Value === false

            ctu.ACC.Value === 50us
            ctu.DN.Value === false
            for i in [51..100] do
                t1.Value <- true
                evaluateRungInputs ctu
                ctu.ACC.Value === uint16 i
                t1.Value <- false
                evaluateRungInputs ctu
            ctu.ACC.Value === 100us
            ctu.DN.Value === true

        [<Test>]
        member __.``CTUD creation test`` () =
            use _ = setRuntimeTarget AB
            let storages = Storages()
            let t1 = PlcTag("my_counter_up_tag", "%M1.1", false)
            let t2 = PlcTag("my_counter_down_tag", "%M1.1", false)
            let t3 = PlcTag("my_counter_reset_tag", "%M1.1", false)
            let upCondition = var2expr t1
            let downCondition = var2expr t2
            let resetCondition = var2expr t3

            let tcParam = {Storages=storages; Name="myCTU"; Preset=100us; RungInCondition=upCondition; FunctionName="createWinCTUD"}
            let ctu = CounterStatement.CreateCTUD(tcParam, downCondition, resetCondition) |> toCounter
            ctu.OV.Value === false
            ctu.UN.Value === false
            ctu.DN.Value === false
            ctu.PRE.Value === 100us
            ctu.ACC.Value === 0us


            (* Counter struct 의 내부 tag 들이 생성되고, 등록되었는지 확인 *)
            let internalTags =
                [
                    ctu.CU :> IStorage
                    ctu.CD
                    ctu.OV
                    ctu.UN
                    ctu.DN
                    ctu.PRE
                    ctu.ACC
                    ctu.RES
                ]

            storages.ContainsKey("myCTU") === true
            for t in internalTags do
                storages.ContainsKey(t.Name) === true

        [<Test>]
        member __.``CTU with reset creation test`` () =
            use _ = setRuntimeTarget WINDOWS
            let storages = Storages()
            let t1 = PlcTag("my_counter_control_tag", "%M1.1", false)
            let resetTag = PlcTag("my_counter_reset_tag", "%M1.1", false)
            let condition = var2expr t1
            let reset = var2expr resetTag
            let tcParam = {Storages=storages; Name="myCTU"; Preset=100us; RungInCondition=condition; FunctionName="createWinCTU"}
            let ctu = CounterStatement.CreateCTU(tcParam, reset) |> toCounter
            ctu.OV.Value === false
            ctu.UN.Value === false
            ctu.DN.Value === false
            ctu.RES.Value === false
            ctu.PRE.Value === 100us
            ctu.ACC.Value === 0us


            for i in [1..50] do
                t1.Value <- true
                evaluateRungInputs ctu
                ctu.ACC.Value === uint16 i
                t1.Value <- false
                evaluateRungInputs ctu
                ctu.DN.Value === false

            ctu.ACC.Value === 50us
            ctu.DN.Value === false

            // counter reset
            resetTag.Value <- true
            evaluateRungInputs ctu
            ctu.OV.Value === false
            ctu.UN.Value === false
            ctu.DN.Value === false
            ctu.RES.Value === true
            ctu.PRE.Value === 100us
            ctu.ACC.Value === 0us


        [<Test>]
        member __.``CTR with reset creation test`` () =
            use _ = setRuntimeTarget WINDOWS
            let storages = Storages()
            let t1 = PlcTag("my_counter_control_tag", "%M1.1", false)
            let resetTag = PlcTag("my_counter_reset_tag", "%M1.1", false)
            let condition = var2expr t1
            let reset = var2expr resetTag
            let tcParam = {Storages=storages; Name="myCTR"; Preset=100us; RungInCondition=condition; FunctionName="createWinCTR"}
            let ctr = CounterStatement.CreateXgiCTR(tcParam, reset) |> toCounter
            ctr.OV.Value === false
            ctr.UN.Value === false
            ctr.DN.Value === false
            ctr.RES.Value === false
            ctr.PRE.Value === 100us
            ctr.ACC.Value === 0us


            for i in [1..50] do
                t1.Value <- true
                evaluateRungInputs ctr
                ctr.ACC.Value === uint16 i
                t1.Value <- false
                evaluateRungInputs ctr
                ctr.DN.Value === false
            ctr.ACC.Value === 50us
            ctr.DN.Value === false

            for i in [51..99] do
                t1.Value <- true
                evaluateRungInputs ctr
                ctr.ACC.Value === uint16 i
                t1.Value <- false
                evaluateRungInputs ctr
                ctr.DN.Value === false

            ctr.ACC.Value === 99us
            ctr.DN.Value === false

            t1.Value <- true        // last straw that broken ...
            evaluateRungInputs ctr
            ctr.ACC.Value === 100us
            ctr.DN.Value === true

            // counter preset + 1 : ring counter : auto reset
            t1.Value <- false
            evaluateRungInputs ctr
            t1.Value <- true
            evaluateRungInputs ctr
            ctr.ACC.Value === 1us
            ctr.DN.Value === false




            // force counter reset
            resetTag.Value <- true
            evaluateRungInputs ctr
            ctr.OV.Value === false
            ctr.UN.Value === false
            ctr.DN.Value === false
            ctr.RES.Value === true
            ctr.PRE.Value === 100us
            ctr.ACC.Value === 0us





        [<Test>]
        member x.``CTU on WINDOWS platform test`` () =
            use _ = setRuntimeTarget WINDOWS
            let storages = Storages()
            let code = """
                bool x0 = createTag("%MX0.0.0", false);
                ctu myCTU = createWinCTU(2000us, $x0);
"""

            let statement = parseCode storages code
            [ "CU"; "DN"; "OV"; "UN"; "PRE"; "ACC"; "RES" ] |> iter (fun n -> storages.ContainsKey($"myCTU.{n}") === true)
            [ "CD"; "Q"; "PT"; "ET"; ] |> iter (fun n -> storages.ContainsKey($"myCTU.{n}") === false)

        [<Test>]
        member x.``CTU on XGI platform test`` () =
            use _ = setRuntimeTarget XGI
            let storages = Storages()
            let code = """
                bool cu = createTag("%MX0.0.0", false);
                bool r  = createTag("%MX0.0.1", false);
                ctu myCTU = createXgiCTU(2000us, $cu, $r);
"""

            let statement = parseCode storages code
            [ "CU"; "Q"; "PV"; "CV"; "R"; ] |> iter (fun n -> storages.ContainsKey($"myCTU.{n}") === true)
            [ "DN"; "PRE"; "ACC"; ] |> iter (fun n -> storages.ContainsKey($"myCTU.{n}") === false)

        [<Test>]
        member x.``CTD on WINDOWS platform test`` () =
            use _ = setRuntimeTarget WINDOWS
            let storages = Storages()
            let code = """
                bool x0 = createTag("%MX0.0.0", false);
                ctd myCTD = createWinCTD(2000us, $x0);
"""

            let statement = parseCode storages code
            [ "CD"; "DN"; "OV"; "UN"; "PRE"; "ACC"; "RES" ] |> iter (fun n -> storages.ContainsKey($"myCTD.{n}") === true)
            [ "CU"; "Q"; "PT"; "ET"; ] |> iter (fun n -> storages.ContainsKey($"myCTD.{n}") === false)

        [<Test>]
        member x.``CTD on XGI platform test`` () =
            use _ = setRuntimeTarget XGI
            let storages = Storages()
            let code = """
                bool cd = createTag("%MX0.0.0", false);
                bool ld = createTag("%MX0.0.1", false);
                ctd myCTD = createXgiCTD(2000us, $cd, $ld);
"""

            let statement = parseCode storages code
            [ "CD"; "LD"; "PV"; "Q"; "CV"; ] |> iter (fun n -> storages.ContainsKey($"myCTD.{n}") === true)
            [ "DN"; "PRE"; "ACC"; ] |> iter (fun n -> storages.ContainsKey($"myCTD.{n}") === false)


        [<Test>]
        member x.``CTUD on WINDOWS platform test`` () =
            use _ = setRuntimeTarget WINDOWS
            let storages = Storages()
            let code = """
                bool cu = createTag("%MX0.0.0", false);
                bool cd = createTag("%MX0.0.1", false);
                bool r  = createTag("%MX0.0.2", false);
                ctud myCTUD = createWinCTUD(2000us, $cu, $cd, $r);
"""

            let statement = parseCode storages code
            [ "CU"; "CD"; "DN"; "OV"; "UN"; "PRE"; "ACC"; "RES" ] |> iter (fun n -> storages.ContainsKey($"myCTUD.{n}") === true)
            [ "LD"; "Q"; "PT"; "ET"; ] |> iter (fun n -> storages.ContainsKey($"myCTUD.{n}") === false)

        [<Test>]
        member x.``CTUD on XGI platform test`` () =
            use _ = setRuntimeTarget XGI
            let storages = Storages()
            let code = """
                bool cu = createTag("%MX0.0.0", false);
                bool cd = createTag("%MX0.0.1", false);
                bool r  = createTag("%MX0.0.2", false);
                bool ld = createTag("%MX0.0.3", false);
                ctud myCTUD = createXgiCTUD(2000us, $cu, $cd, $r, $ld);
"""

            let statement = parseCode storages code
            [ "CU"; "CD"; "R"; "LD"; "PV"; "QU"; "QD"; "CV"; ] |> iter (fun n -> storages.ContainsKey($"myCTUD.{n}") === true)
            [ "DN"; "PRE"; "ACC"; ] |> iter (fun n -> storages.ContainsKey($"myCTUD.{n}") === false)




        [<Test>]
        member x.``CTR on WINDOWS platform test`` () =
            use _ = setRuntimeTarget WINDOWS
            let storages = Storages()
            let code = """
                bool x0 = createTag("%MX0.0.0", false);
                ctr myCTR = createWinCTR(2000us, $x0);
"""

            let statement = parseCode storages code
            [ "CD"; "DN"; "OV"; "UN"; "PRE"; "ACC"; "RES" ] |> iter (fun n -> storages.ContainsKey($"myCTR.{n}") === true)
            [ "CU"; "Q"; "PT"; "ET"; ] |> iter (fun n -> storages.ContainsKey($"myCTR.{n}") === false)

        [<Test>]
        member x.``CTR on XGI platform test`` () =
            use _ = setRuntimeTarget XGI
            let storages = Storages()
            let code = """
                bool cd = createTag("%MX0.0.0", false);
                bool rst = createTag("%MX0.0.1", false);
                ctr myCTR = createXgiCTR(2000us, $cd, $rst);
"""

            let statement = parseCode storages code
            [ "CD"; "PV"; "RST"; "Q"; "CV"; ] |> iter (fun n -> storages.ContainsKey($"myCTR.{n}") === true)
            [ "CU"; "DN"; "PRE"; "ACC"; ] |> iter (fun n -> storages.ContainsKey($"myCTR.{n}") === false)
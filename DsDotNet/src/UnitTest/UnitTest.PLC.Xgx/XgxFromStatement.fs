namespace T

open NUnit.Framework

open Engine.Core
open Dual.Common.Core.FS
open Dual.UnitTest.Common.FS
open Engine.Parser.FS
open PLC.CodeGen.LS
open Dual.Common.Core.FS.Reflection


type XgxFromStatementTest(xgx:PlatformTarget) =
    inherit XgxTestBaseClass(xgx)

    let prjParam = getXgxProjectParams xgx "UnitTestProject"
    let noComment:string = null
    member x.CreateVar name = 
        prjParam.CreateTypedAutoVariable(name, false, null)
        |> tee (fun x ->
            x.Address <- name   // 일단 test 용으로 name 을 그대로 address 로 할당.  XGK 에서는 address 가 반드시 필요.
            prjParam.GlobalStorages.Add (x.Name, x))

    member x.``DuAssign statement test`` () =

        let varName = "XX"
        let targetVarname = if xgx = XGI then $"_t1_{varName}" else varName
        let cmtStmts =
            let stmt = DuAssign(None, literal2expr false, x.CreateVar varName)
            CommentedStatements(noComment, [stmt])
        let xml = generateRungs prjParam noComment [cmtStmts]
        let answer = $"""	<Rung BlockMask="0">
		<Element ElementType="6" Coordinate="1" >_OFF</Element>
		<Element ElementType="2" Coordinate="4" Param="90"></Element>
		<Element ElementType="14" Coordinate="94" >{targetVarname}</Element>	</Rung>"""
        //tracefn "%s" xml
        //tracefn "%s" answer
        xml.StartsWith(answer) === true


        let targetVarname = if xgx = XGI then $"_t2_{varName}" else varName
        let cmtStmts =
            let stmt = DuAssign(None, literal2expr true, x.CreateVar "XX")
            CommentedStatements(noComment, [stmt])
        let xml = generateRungs prjParam noComment [cmtStmts]
        let answer = $"""	<Rung BlockMask="0">
		<Element ElementType="6" Coordinate="1" >_ON</Element>
		<Element ElementType="2" Coordinate="4" Param="90"></Element>
		<Element ElementType="14" Coordinate="94" >{targetVarname}</Element>	</Rung>"""
        //tracefn "%s" xml
        xml.StartsWith(answer) === true


    (*
        [DuAssign] 문장 변환:
            - condition 이 존재하는 경우
                - BOOL type 인 경우
                    - XGK : BAND, BOR 를 이용한 변환
                    - XGI : DuPLCFunction({FunctionName = XgiConstants.FunctionNameMove ...}) 이용 변환
                - 'T type 인 경우
                    - XGK : MOVE command 이용
                    - XGI : DuPLCFunction({FunctionName = XgiConstants.FunctionNameMove ...}) 이용 변환
            - condition 이 존재하지 않는 경우
                - BOOL type 인 경우
                    - DuAssign 그대로 사용 (normal ladder)
                - 'T type 인 경우
                    - ?? XGK : MOVE command 이용
                    - XGI : DuPLCFunction({FunctionName = XgiConstants.FunctionNameMove ...}) 이용 변환
     *)
    member x.``DuCopy bool with condition statement test`` () =
        let varName = "XX"

        let condition = true
        let cmtStmts =
            let c = literal2expr condition
            let source = literal2expr false
            let target = x.CreateVar varName |> tee (fun t -> t.Address <- "P0000A" )
            let stmt =
                let stmt = DuAction(DuCopy(c, source, target))
                if xgx = XGI then
                    let fParam = {FunctionName = XgiConstants.FunctionNameMove; Condition = Some c; Arguments = [c; source;]; Output = target; OriginalExpression = c}
                    DuPLCFunction(fParam)
                else
                    stmt
            CommentedStatements(noComment, [stmt])
        let xml = generateRungs prjParam noComment [cmtStmts]
        let answer =
            let conditionText = if condition then "_ON" else "_OFF"
            if xgx = XGK then
                $"""	<Rung BlockMask="0">
		<Element ElementType="6" Coordinate="1" >{conditionText}</Element>
		<Element ElementType="2" Coordinate="4" Param="75"></Element>
		<Element ElementType="33" Coordinate="94" Param="BAND,P00008,251,P00008,1"></Element>	</Rung>"""
            else
                $"""	<Rung BlockMask="0">
		<Element ElementType="6" Coordinate="1" >_ON</Element>
		<Element ElementType="102" Coordinate="4" Param="FNAME: MOVE&#xA;TYPE: function&#xA;INSTANCE: ,&#xA;INDEX: 118&#xA;COL_PROP: 1&#xA;SAFETY: 0&#xA;VAR_IN: EN, 0x00200001, , 0&#xA;VAR_IN: IN, 0x02afffff, ARRAY[0..-1] OF ANY, 0&#xA;VAR_OUT: ENO, 0x00000001,&#xA;VAR_OUT: OUT, 0x028fffff, ARRAY[0..-1] OF ANY &#xA;"></Element>
		<Element ElementType="70" Coordinate="1025" >False</Element>
		<Element ElementType="70" Coordinate="1031" >_t1_XX</Element>	</Rung>"""

        xml.StartsWith(answer) === true

    member x.MyTestCode (funcName:string, statements:Statement list, storages:Storages) =
        let xml = x.generateXmlForTest funcName storages (map withNoComment statements)
        x.saveTestResult funcName xml
        storages, statements

    member x.``DuCopy int with condition statement test`` () =
        let varName = "XX"
        let condition = literal2expr true
        let source = literal2expr 3
        let target =
            prjParam.CreateTypedAutoVariable(varName, 0, null)
            |> tee (fun x ->
                if prjParam.TargetType = XGK then
                    x.Address <- "P0000"   // 일단 test 용으로 name 을 그대로 address 로 할당.  XGK 에서는 address 가 반드시 필요.
                prjParam.GlobalStorages.Add (x.Name, x))


        let statements = [DuAssign(Some condition, source, target)]
        x.MyTestCode(getFuncName(), statements, prjParam.GlobalStorages) |> ignore
        ()

    member x.``DuCopy int add with condition statement test`` () =
        let varName = "XX"
        let condition = literal2expr true
        let source = 
            createBinaryExpression (literal2expr 3) "+" (literal2expr 5)
        let target =
            prjParam.CreateTypedAutoVariable(varName, 0, null)
            |> tee (fun x ->
                if prjParam.TargetType = XGK then
                    x.Address <- "P0000"   // 일단 test 용으로 name 을 그대로 address 로 할당.  XGK 에서는 address 가 반드시 필요.
                prjParam.GlobalStorages.Add (x.Name, x))


        let statements = [DuAssign(Some condition, source, target)]
        x.MyTestCode(getFuncName(), statements, prjParam.GlobalStorages) |> ignore
        ()


type XgiFromStatementTest() =
    inherit XgxFromStatementTest(XGI)
    [<Test>] member __.``DuAssign statement test`` () = base.``DuAssign statement test``()
    [<Test>] member __.``DuCopy bool with condition statement test`` () = base.``DuCopy bool with condition statement test``()
    [<Test>] member __.``DuCopy int with condition statement test`` () = base.``DuCopy int with condition statement test``()
    [<Test>] member __.``DuCopy int add with condition statement test`` () = base.``DuCopy int add with condition statement test``()

type XgkFromStatementTest() =
    inherit XgxFromStatementTest(XGK)
    [<Test>] member __.``DuAssign statement test`` () = base.``DuAssign statement test``()
    [<Test>] member __.``DuCopy bool with condition statement test`` () = base.``DuCopy bool with condition statement test``()
    [<Test>] member __.``DuCopy int with condition statement test`` () = base.``DuCopy int with condition statement test``()
    [<Test>] member __.``DuCopy int add with condition statement test`` () = base.``DuCopy int add with condition statement test``()

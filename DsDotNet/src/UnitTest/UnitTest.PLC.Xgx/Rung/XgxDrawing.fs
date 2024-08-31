namespace T.Rung
open T

open Dual.Common.UnitTest.FS
open NUnit.Framework
open Dual.Common.Core.FS
open PLC.CodeGen.LS
open PLC.CodeGen.LS.Config.POU.Program.LDRoutine.ElementType
open Engine.Core

type XgxDrawingTest(xgx:PlatformTarget) =
    inherit XgxTestBaseClass(xgx)

    let span width = width*3

    member __.``ADD function details test``() =
        (* Function/FunctionBlock 정보 :
            - function 의 가로 너비 : COL_PROP
            - function 의 세로 높이 : max(# VAR_IN, # VAR_OUT)
            - TYPE: {function or function_block}
            - I/O parameter : VAR_{IN, OUT} 을 참조
        *)

        let details = FB.getFunctionDeails "ADD"
        [
            //"#BEGIN_FUNC: ADD"
            "FNAME: ADD"
            "TYPE: function"
            "INSTANCE: INST,VAR"
            "INDEX: 71"
            "COL_PROP: 1"
            "SAFETY: 0"
            "VAR_IN: EN, 0x00200001, , 0"
            "VAR_OUT: ENO, 0x00000001,"
            "VAR_OUT: OUT, 0x00007fe0,"
            //"#END_FUNC"
        ] |> SeqEq details

        FB.decodeVarType "0x00200001" |> toString === "BOOL, CONSTANT"    // EN

        let details = FB.getFunctionDeails "ADD2_INT"
        [
            //"#BEGIN_FUNC: ADD2_INT"
            "FNAME: ADD2_INT"
            "TYPE: function"
            "INSTANCE: INST,VAR"
            "INDEX: 1686"
            "COL_PROP: 1"
            "SAFETY: 0"
            "VAR_IN: EN, 0x00200001, , 0"
            "VAR_IN: IN1, 0x00200040, , 0"
            "VAR_IN: IN2, 0x00200040, , 0"
            "VAR_OUT: ENO, 0x00000001,"
            "VAR_OUT: OUT, 0x00000040,"
            //"#END_FUNC"
        ] |> SeqEq details
        FB.decodeVarType "0x00200040" |> toString === "INT, CONSTANT"   // IN1, IN2, OUT




        let details = FB.getFunctionDeails "GT"
        [
            //"#BEGIN_FUNC: GT"
            "FNAME: GT"
            "TYPE: function"
            "INSTANCE: INST,VAR"
            "INDEX: 68"
            "COL_PROP: 1"
            "SAFETY: 0"
            "VAR_IN: EN, 0x00200001, , 0"
            "VAR_OUT: ENO, 0x00000001,"
            "VAR_OUT: OUT, 0x00000001,"
            //"#END_FUNC"
        ] |> SeqEq details


        ()

    member x.``ADD function drawing test``() =
        let { Coordinate = c; Xml = elementAddXml } = rxiFunctionAt ("ADD2_INT", "ADD") "" (3, 2)
        (* '&#xA' = '&#10' = '\n' 의 HTML encoding *)
        let originalElementAddXml_ = "FNAME: ADD&#xA;TYPE: function&#xA;INSTANCE: ,&#xA;INDEX: 71&#xA;COL_PROP: 1&#xA;SAFETY: 0&#xA;PROP_COLOR: 16777215&#xA;VAR_IN: EN, 0x00200001, , 0&#xA;VAR_IN: IN1, 0x00207fe0, , 0&#xA;VAR_IN: IN2, 0x00207fe0, , 0&#xA;VAR_OUT: ENO, 0x00000001, &#xA;VAR_OUT: OUT, 0x00007fe0, &#xA;"

        let rungsXml =
            [
                let x, y = 1, 1
                $"""
<Rung BlockMask="0">
	<Element ElementType="{ContactMode}"       Coordinate="{coord(0,  2)}">EN</Element>
	<Element ElementType="{MultiHorzLineMode}" Coordinate="{coord(1,  2)}" Param="3"></Element>
	{elementAddXml}
	<Element ElementType="{VariableMode}"      Coordinate="{coord(2,  3)}">IN1</Element>
	<Element ElementType="{VariableMode}"      Coordinate="{coord(4,  3)}">Q</Element>
	<Element ElementType="{VariableMode}"      Coordinate="{coord(2,  4)}">IN2</Element>
</Rung>
"""
            ] |> String.concat "\r\n"



        // Symbol 정의
        let symbolInfos = [
            let intInitValue:BoxedObjectHolder = {Object=0}
            let kind = (int Variable.Kind.VAR)
            XGITag.createSymbolInfo "EN"  "EN"  "BOOL" kind {Object=false}
            XGITag.createSymbolInfo "IN1" "IN1" "INT"  kind intInitValue
            XGITag.createSymbolInfo "IN2" "IN2" "INT"  kind intInitValue
            XGITag.createSymbolInfo "Q"   "Q"   "INT"  kind intInitValue
        ]

        let xml =
            let prjParam = getXgxProjectParams xgx (getFuncName())
            wrapWithXml prjParam rungsXml symbolInfos emptySymbolsGlobalXml None
        x.saveTestResult (getFuncName ()) xml

    member x.``Box drawing test``() =
        let rungsXml = $"""
<Rung BlockMask="0">
    <Element ElementType="{RungCommentMode}"   Coordinate="{coord(0, 0)}">Test boxes</Element>
	<Element ElementType="{MultiHorzLineMode}" Coordinate="{coord(0, 1)}" Param="{span 0}"></Element>
	<Element ElementType="{VertLineMode}"      Coordinate="{coord(0, 1) + 2}"></Element>
	<Element ElementType="{MultiHorzLineMode}" Coordinate="{coord(0, 2)}" Param="{span 0}"></Element>
</Rung>


<Rung BlockMask="0">
    <Element ElementType="{RungCommentMode}"   Coordinate="{coord(0, 3)}">Another boxes</Element>
	<Element ElementType="{VertLineMode}"      Coordinate="{coord(0, 4) + 2}"></Element>
	<Element ElementType="{MultiHorzLineMode}" Coordinate="{coord(1, 4)}" Param="{span 0}"></Element>
	<Element ElementType="{VertLineMode}"      Coordinate="{coord(1, 4) + 2}"></Element>
	<Element ElementType="{MultiHorzLineMode}" Coordinate="{coord(1, 5)}" Param="{span 0}"></Element>
</Rung>

<Rung BlockMask="0">
    <Element ElementType="{RungCommentMode}"   Coordinate="{coord(0, 6)}">Another 1x2 boxes</Element>
	<Element ElementType="{VertLineMode}"      Coordinate="{coord(0, 7) + 2}"></Element>
	<Element ElementType="{MultiHorzLineMode}" Coordinate="{coord(1, 7)}" Param="{span 1}"></Element>
	<Element ElementType="{VertLineMode}"      Coordinate="{coord(2, 7) + 2}"></Element>
	<Element ElementType="{MultiHorzLineMode}" Coordinate="{coord(1, 8)}" Param="{span 1}"></Element>
</Rung>

<Rung BlockMask="0">
    <Element ElementType="{RungCommentMode}"   Coordinate="{coord(0, 9)}">Another 1x3 boxes</Element>
	<Element ElementType="{VertLineMode}"      Coordinate="{coord(0, 10) + 2}"></Element>
	<Element ElementType="{MultiHorzLineMode}" Coordinate="{coord(1, 10)}" Param="{span 2}"></Element>
	<Element ElementType="{VertLineMode}"      Coordinate="{coord(3, 10) + 2}"></Element>
	<Element ElementType="{MultiHorzLineMode}" Coordinate="{coord(1, 11)}" Param="{span 2}"></Element>
</Rung>

<Rung BlockMask="0">
    <Element ElementType="{RungCommentMode}"   Coordinate="{coord(0, 12)}">Another 2x3 boxes</Element>
	<Element ElementType="{VertLineMode}"      Coordinate="{coord(0, 13) + 2}"></Element>
	<Element ElementType="{VertLineMode}"      Coordinate="{coord(0, 14) + 2}"></Element>
	<Element ElementType="{MultiHorzLineMode}" Coordinate="{coord(1, 13)}" Param="{span 2}"></Element>
	<Element ElementType="{VertLineMode}"      Coordinate="{coord(3, 13) + 2}"></Element>
	<Element ElementType="{VertLineMode}"      Coordinate="{coord(3, 14) + 2}"></Element>
	<Element ElementType="{MultiHorzLineMode}" Coordinate="{coord(1, 15)}" Param="{span 2}"></Element>
</Rung>

"""
        let xml =
            let prjParam = getXgxProjectParams xgx (getFuncName())
            wrapWithXml prjParam rungsXml [] emptySymbolsGlobalXml None
        x.saveTestResult (getFuncName ()) xml

    member x.``Vertical line drawing test``() =
        let rungsXml =
            [
                let x, y = 1, 1
                $"""
<Rung BlockMask="0">
    <Element ElementType="{RungCommentMode}"   Coordinate="{coord(0, y-1)}">({x}, {y}) 에서 시작하는 'ㄱ'</Element>
	<Element ElementType="{MultiHorzLineMode}" Coordinate="{coord(x, y)}" Param="0"></Element>
	<Element ElementType="{VertLineMode}"      Coordinate="{coord(x, y) + 2}"></Element>
</Rung>
"""

                let x, y = 4, 4
                $"""
<Rung BlockMask="0">
    <Element ElementType="{RungCommentMode}"   Coordinate="{coord(0, y-1)}">({x}-, {y}) 에서 시작하는 'ㄴ'</Element>
	<Element ElementType="{VertLineMode}"      Coordinate="{coord(x, y) - 1}"></Element>
	<Element ElementType="{MultiHorzLineMode}" Coordinate="{coord(x, y+1)}" Param="0"></Element>
</Rung>
"""

            ] |> String.concat "\r\n"


        let xml =
            let prjParam = getXgxProjectParams xgx (getFuncName())
            wrapWithXml prjParam rungsXml [] emptySymbolsGlobalXml None
        x.saveTestResult (getFuncName ()) xml



type XgiDrawingTest() =
    inherit XgxDrawingTest(XGI)
    [<Test>] member __.``ADD function details test``() = base.``ADD function details test``()
    [<Test>] member __.``ADD function drawing test``() = base.``ADD function drawing test``()
    [<Test>] member __.``Box drawing test``() = base.``Box drawing test``()
    [<Test>] member __.``Vertical line drawing test``() = base.``Vertical line drawing test``()




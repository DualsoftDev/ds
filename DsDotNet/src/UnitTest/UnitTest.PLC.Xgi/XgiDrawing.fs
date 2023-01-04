namespace T

open NUnit.Framework
open Engine.Common.FS
open PLC.CodeGen.LSXGI
open PLC.CodeGen.LSXGI.Config.POU.Program.LDRoutine.ElementType

type XgiDrawingTest() =
    inherit XgiTestClass()

    let span width = width*3

    [<Test>]
    member __.``Box drawing test``() =
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
        let xml = wrapWithXml rungsXml emptySymbolsLocalXml emptySymbolsGlobalXml None
        saveTestResult (get_current_function_name ()) xml


    [<Test>]
    member __.``Vertical line drawing test``() =
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


        let xml = wrapWithXml rungsXml emptySymbolsLocalXml emptySymbolsGlobalXml None
        saveTestResult (get_current_function_name ()) xml



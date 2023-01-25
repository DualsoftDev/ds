namespace T

open System.Linq
open NUnit.Framework
open Engine.Core
open Engine.Common.FS
open PLC.CodeGen.LSXGI
open PLC.CodeGen.LSXGI.Config.POU.Program.LDRoutine.ElementType
open Engine.Parser.FS

type XgiRungTest() =
    inherit XgiTestBaseClass()

    [<Test>]
    member __.``Prolog comment test``() =
        let rungsXml = $"""<Rung BlockMask="0"><Element ElementType="{RungCommentMode}" Coordinate="1">DS Logic for XGI</Element></Rung>"""
        let xml = wrapWithXml rungsXml emptySymbolsLocalXml emptySymbolsGlobalXml None
        saveTestResult (get_current_function_name ()) xml

    [<Test>]
    member __.``Generate simplest program test``() =
        let xml = wrapWithXml simplestProgramXml emptySymbolsLocalXml emptySymbolsGlobalXml None
        saveTestResult (get_current_function_name ()) xml


    [<Test>]
    member __.``Generate simplest with local variables test``() =
        let xml = wrapWithXml simplestProgramXml simpleSymbolsLocalXml emptySymbolsGlobalXml None
        saveTestResult (get_current_function_name ()) xml


    [<Test>]
    member __.``Generate simplest with local, global variables test``() =
        let symbolsGlobalXml =
            """
<GlobalVariable Version="Ver 1.0" Count="2">
<Symbols>
    <Symbol Name="myBit00" Kind="6" Type="BOOL" Comment="FAKECOMMENT" Device="I" Address="%IX0.0.0" State="0">
        <MemberAddresses/>
        <MemberRetains/>
        <MemberInitValues/>
        <MemberComments/>
    </Symbol>
    <Symbol Name="myBit01" Kind="6" Type="BOOL" Comment="FAKECOMMENT" Device="I" Address="%IX0.0.1" State="0">
        <MemberAddresses/>
        <MemberRetains/>
        <MemberInitValues/>
        <MemberComments/>
    </Symbol>
</Symbols>
<TempVar Count="0"></TempVar>
</GlobalVariable>
"""

        let xml = wrapWithXml simplestProgramXml simpleSymbolsLocalXml symbolsGlobalXml None
        saveTestResult (get_current_function_name ()) xml
        ()


    [<Test>]
    member __.``Generate local variables test``() =
        let t = Tag("myBit00", "%IX0.0.0", false)
        // name, comment, device, kind, address, plcType 를 받아서 SymbolInfo 를 생성한다.
        let symbolInfo: SymbolInfo =
            { defaultSymbolCreateParam with Name=t.Name; PLCType="BOOL"; Address=t.Address; Device="I"; }
            |> XGITag.createSymbolInfoWithDetail

        let symbolInfoXml = symbolInfo.GenerateXml()
        symbolInfoXml =~= """<Symbol Name="myBit00" Kind="1" Type="BOOL" Comment="Fake Comment" Device="I" Address="%IX0.0.0" State="0">
		<MemberAddresses/>
		<MemberRetains/>
		<MemberInitValues/>
		<MemberComments/>
	</Symbol>"""

        let symbolsLocalXml = XGITag.generateLocalSymbolsXml [ symbolInfo ]

        symbolsLocalXml =~= """<LocalVar Version="Ver 1.0" Count="1">
<Symbols>
	<Symbol Name="myBit00" Kind="1" Type="BOOL" Comment="Fake Comment" Device="I" Address="%IX0.0.0" State="0">
		<MemberAddresses/>
		<MemberRetains/>
		<MemberInitValues/>
		<MemberComments/>
	</Symbol>
</Symbols>
<TempVar Count="0"></TempVar>
</LocalVar>"""

    member __.PrepareWithSymbols(numTags:int) =
        let storages = Storages()
        let q = Tag("myQ0", "%QX0.1.0", false)
        let statements_ = parseCode storages codeForBits31
        let iTags = storages.Values.ToEnumerable<Tag<bool>>().Take(numTags).ToArray()
        let symbolInfos =
            let kindVar = int Variable.Kind.VAR
            let plcType = "BOOL"
            [   for t in iTags do
                    { defaultSymbolCreateParam with Name=t.Name; PLCType=plcType; Address=t.Address; Device="I"; Kind=kindVar; }
                    |> XGITag.createSymbolInfoWithDetail

                { defaultSymbolCreateParam with Name=q.Name; PLCType=plcType; Address=q.Address; Device="Q"; Kind=kindVar; }
                |> XGITag.createSymbolInfoWithDetail
            ]
        let localSymbolsXml = XGITag.generateLocalSymbolsXml symbolInfos
        iTags, q, localSymbolsXml


    [<Test>]
    member x.``Generate ANDsMax(=31) variables test``() =
        let iTags, q, localSymbolsXml = x.PrepareWithSymbols(31)

        let rungs =
            [
                $"""
<Rung BlockMask="0"><Element ElementType="{RungCommentMode}" Coordinate="1">DS Logic for XGI</Element></Rung>
<Rung BlockMask="0">
"""
                let y = 1
                let mutable x = 0
                for t in iTags do
                    contactAt t.Name x y     // <Element ElementType="{ContactMode}" Coordinate="1025">myBit00</Element>
                    x <- x + 1

                (* 꽉 채운 경우에는 HorzLineMode 및 MultiHorzLineMode 가 들어갈 공간이 없으므로 사용하지 않는다. *)
                //let xy = coord(x, y)
                //$""" <Element ElementType="{HorzLineMode}" Coordinate="{xy}"></Element>"""
                //x <- x + 1
                //let xy = coord(x, y)
                //$""" <Element ElementType="{MultiHorzLineMode}" Coordinate="{xy}" Param="84"></Element>"""

                coilAt q.Name 1

                $"""
</Rung>
<Rung BlockMask="0">
    <Element ElementType="{MultiHorzLineMode}" Coordinate="{coord(0, 2)}" Param="90"></Element>
    <Element ElementType="{FBMode}" Coordinate="{coord(31, 2)}" Param="END">END</Element>
</Rung>"""
            ] |> String.concat "\r\n"

        let xml = wrapWithXml rungs localSymbolsXml emptySymbolsGlobalXml None
        saveTestResult (get_current_function_name ()) xml


    [<Test>]
    member x.``Generate ANDs30 variables test``() =
        let iTags, q, localSymbolsXml = x.PrepareWithSymbols(30)

        let rungs =
            [
                $"""
<Rung BlockMask="0"><Element ElementType="{RungCommentMode}" Coordinate="{coord(0, 0)}">DS Logic for XGI</Element></Rung>
<Rung BlockMask="0">
"""
                let y = 1
                let mutable x = 0
                for t in iTags.Take(30) do
                    contactAt t.Name x y     // <Element ElementType="{ContactMode}" Coordinate="1025">myBit00</Element>
                    x <- x + 1

                (* 꽉 채우고, 한 칸 빌 경우에는 HorzLineMode 만 사용한다. *)
                let xy = coord(x, y)
                $""" <Element ElementType="{HorzLineMode}" Coordinate="{xy}"></Element>"""
                //x <- x + 1
                //let xy = coord(x, y)
                //$""" <Element ElementType="{MultiHorzLineMode}" Coordinate="{xy}" Param="84"></Element>"""

                coilAt q.Name 1

                $"""
</Rung>
<Rung BlockMask="0">
    <Element ElementType="{MultiHorzLineMode}" Coordinate="{coord(0, 2)}" Param="90"></Element>
    <Element ElementType="{FBMode}" Coordinate="2142" Param="END">END</Element>
</Rung>"""
            ] |> String.concat "\r\n"

        let xml = wrapWithXml rungs localSymbolsXml emptySymbolsGlobalXml None
        saveTestResult (get_current_function_name ()) xml


    [<Test>]
    member x.``Generate ANDs29 variables test``() =
        let iTags, q, localSymbolsXml = x.PrepareWithSymbols(29)

        let rungs =
            [
                $"""
<Rung BlockMask="0"><Element ElementType="{RungCommentMode}" Coordinate="{coord(0, 0)}">DS Logic for XGI</Element></Rung>
<Rung BlockMask="0">
"""
                let y = 1
                let mutable x = 0
                for t in iTags.Take(29) do
                    contactAt t.Name x y     // <Element ElementType="{ContactMode}" Coordinate="1025">myBit00</Element>
                    x <- x + 1

                (* 꽉 채우고, 두 칸 이상 빌 경우에는 HorzLineMode 및 MultiHorzLineMode 를 모두 사용한다. *)
                let xy = coord(x, y)
                $""" <Element ElementType="{HorzLineMode}" Coordinate="{xy}"></Element>"""
                x <- x + 1
                let xy = coord(x, y)
                $""" <Element ElementType="{MultiHorzLineMode}" Coordinate="{xy}" Param="84"></Element>"""

                coilAt q.Name 1

                $"""
</Rung>
<Rung BlockMask="0">
    <Element ElementType="{MultiHorzLineMode}" Coordinate="{coord(0, 2)}" Param="90"></Element>
    <Element ElementType="{FBMode}" Coordinate="{coord(31, 2)}" Param="END">END</Element>
</Rung>"""
            ] |> String.concat "\r\n"

        let xml = wrapWithXml rungs localSymbolsXml emptySymbolsGlobalXml None
        saveTestResult (get_current_function_name ()) xml



    [<Test>]
    member x.``Generate OR2 variables test``() =
        let iTags, q, localSymbolsXml = x.PrepareWithSymbols(2)

        let rungs =
            [
                $"""
<Rung BlockMask="0"><Element ElementType="{RungCommentMode}" Coordinate="{coord(0, 0)}">DS Logic for XGI</Element></Rung>
<Rung BlockMask="0">
"""
                let mutable y = 1
                for t in iTags do
                    contactAt t.Name 0 y     // <Element ElementType="{ContactMode}" Coordinate="1025">myBit00</Element>
                    if y = 1 then
                        (* VertLineMode 로 시작하면 HorzLineMode 없이, 바로 MultiHorzLineMode 가 와야 한다. *)
                        let xy = coord(1, 1) - 1      // 1027
                        $"""<Element ElementType="{VertLineMode}" Coordinate="{xy}" />"""
                        let xy = coord(1, 1)          // 1028
                        let width = (maxNumHorizontalContact - 2) * 3    // 87
                        //$""" <Element ElementType="{HorzLineMode}" Coordinate="{xy} Param={width}"></Element>"""
                        //let xy = coord 31 1
                        $""" <Element ElementType="{MultiHorzLineMode}" Coordinate="{xy}" Param="{width}"></Element>"""

                    y <- y + 1



                coilAt q.Name 1

                let xyEndS, xyEndE = coord(0, y), coord(maxNumHorizontalContact, y)
                $"""
</Rung>
<Rung BlockMask="0">
    <Element ElementType="{MultiHorzLineMode}" Coordinate="{xyEndS}" Param="90"></Element>
    <Element ElementType="{FBMode}" Coordinate="{xyEndE}" Param="END">END</Element>
</Rung>"""
            ] |> String.concat "\r\n"

        let xml = wrapWithXml rungs localSymbolsXml emptySymbolsGlobalXml None
        saveTestResult (get_current_function_name ()) xml

    [<Test>]
    member x.``Generate ORs variables test``() =
        let iTags, q, localSymbolsXml = x.PrepareWithSymbols(31)

        let rungs =
            [
                $"""
<Rung BlockMask="0"><Element ElementType="{RungCommentMode}" Coordinate="{coord(0, 0)}">DS Logic for XGI</Element></Rung>
<Rung BlockMask="0">
"""
                let mutable y = 1
                for t in iTags do
                    contactAt t.Name 0 y     // <Element ElementType="{ContactMode}" Coordinate="1025">myBit00</Element>
                    if y < 31 then
                        (* VertLineMode 로 시작하면 HorzLineMode 없이, 바로 MultiHorzLineMode 가 와야 한다. *)
                        let xy = coord(1, y) - 1      // 1027
                        $"""<Element ElementType="{VertLineMode}" Coordinate="{xy}" />"""
                    if y = 1 then
                        let xy = coord(1, 1)          // 1028
                        let width = (maxNumHorizontalContact - 2) * 3    // 87 = (31-2) * 3
                        $""" <Element ElementType="{MultiHorzLineMode}" Coordinate="{xy}" Param="{width}"></Element>"""

                    y <- y + 1

                coilAt q.Name 1

                let xyEndS, xyEndE = coord(0, y), coord(coilCellX, y)
                $"""
</Rung>
<Rung BlockMask="0">
    <Element ElementType="{MultiHorzLineMode}" Coordinate="{xyEndS}" Param="90"></Element>
    <Element ElementType="{FBMode}" Coordinate="{xyEndE}" Param="END">END</Element>
</Rung>"""
            ] |> String.concat "\r\n"

        let xml = wrapWithXml rungs localSymbolsXml emptySymbolsGlobalXml None
        saveTestResult (get_current_function_name ()) xml

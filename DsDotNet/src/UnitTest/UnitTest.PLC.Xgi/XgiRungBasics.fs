namespace T

open System.Linq
open NUnit.Framework
open Engine.Core
open Engine.Common.FS
open PLC.CodeGen.LSXGI
open PLC.CodeGen.LSXGI.Config.POU.Program.LDRoutine.ElementType
open Engine.Parser.FS

type XgiRungTest() =
    inherit XgiTestClass()

    let emptySymbolsLocalXml = """<LocalVar Version="Ver 1.0" Count="16"></LocalVar>"""

    let emptySymbolsGlobalXml =
        """<GlobalVariable Version="Ver 1.0" Count="16"></GlobalVariable>"""
    (*
        ElementType : HorzLineMode = 1, MultiHorzLineMode = 2, ContactMode = 6, CoilMode = 14
        Coordinate :
            - 가로 방향 : 1, 4, 7, ... (3x+1)
            - 세로 방향 : 1024*y
            - e.g :
                - (1, 0)  : 1024*1 + 3*0  + 1 = 1025
                - (1, 1)  : 1024*1 + 3*1  + 1 = 1028
                - (1, 2)  : 1024*1 + 3*2  + 1 = 1031
                - (1, 31) : 1024*1 + 3*31 + 1 = 1118
                - (2, 31) : 1024*2 + 3*31 + 1 = 2142
    *)
    let simplestProgramXml =
        $"""
        <Rung BlockMask="0"><Element ElementType="{RungCommentMode}" Coordinate="{coord 0 0}">DS Logic for XGI</Element></Rung>
        <Rung BlockMask="0">
            <Element ElementType="{ContactMode}" Coordinate="{coord 0 1}">myBit00</Element>
            <Element ElementType="{HorzLineMode}" Coordinate="{coord 1 1}"></Element>
            <Element ElementType="{MultiHorzLineMode}" Coordinate="{coord 2 1}" Param="84"></Element>
            <Element ElementType="{CoilMode}" Coordinate="{coord 31 1}">myBit01</Element>
        </Rung>
        <Rung BlockMask="0"><Element ElementType="{MultiHorzLineMode}" Coordinate="{coord 0 2}" Param="90"></Element>
            <Element ElementType="{FBMode}" Coordinate="{coord 31 2}" Param="END">END</Element>
        </Rung>
"""

    let simpleSymbolsLocalXml =
        """
<LocalVar Version="Ver 1.0" Count="2">
<Symbols>
    <Symbol Name="myBit00" Kind="1" Type="BOOL" Comment="FAKECOMMENT" Device="I" Address="%IX0.0.0" State="0">
        <MemberAddresses/>
        <MemberRetains/>
        <MemberInitValues/>
        <MemberComments/>
    </Symbol>
    <Symbol Name="myBit01" Kind="1" Type="BOOL" Comment="FAKECOMMENT" Device="I" Address="%IX0.0.1" State="0">
        <MemberAddresses/>
        <MemberRetains/>
        <MemberInitValues/>
        <MemberComments/>
    </Symbol>
</Symbols>
<TempVar Count="0"></TempVar>
</LocalVar>
"""


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
        let t = PlcTag("myBit00", "%IX0.0.0", false)
        // name, comment, device, kind, address, plcType 를 받아서 SymbolInfo 를 생성한다.
        let symbolInfo: SymbolInfo =
            let plcType = "BOOL"
            let kindVar = int Variable.Kind.VAR
            XGITag.createSymbol t.Name "Fake Comment" "I" kindVar t.Address plcType

        let symbolInfoXml = symbolInfo.GenerateXml()
        symbolInfoXml =~= """<Symbol Name="myBit00" Kind="1" Type="BOOL" Comment="Fake Comment" Device="I" Address="%IX0.0.0" State="0">
		<MemberAddresses/>
		<MemberRetains/>
		<MemberInitValues/>
		<MemberComments/>
	</Symbol>"""

        let symbolsLocalXml = XGITag.generateSymbolVars ([ symbolInfo ], false)

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
        let q = PlcTag("myQ0", "%QX0.1.0", false)
        let statements_ = parseCode storages codeForBits31
        let iTags = storages.Values.ToEnumerable<PlcTag<bool>>().Take(numTags).ToArray()
        let symbolInfos =
            let kindVar = int Variable.Kind.VAR
            let plcType = "BOOL"
            [   for t in iTags do
                    XGITag.createSymbol t.Name "Fake Comment" "I" kindVar t.Address plcType

                XGITag.createSymbol q.Name "Fake Comment" "Q" kindVar q.Address plcType
            ]
        let localSymbolsXml = XGITag.generateSymbolVars(symbolInfos, false)
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
                    contactAt t x y     // <Element ElementType="{ContactMode}" Coordinate="1025">myBit00</Element>
                    x <- x + 1

                (* 꽉 채운 경우에는 HorzLineMode 및 MultiHorzLineMode 가 들어갈 공간이 없으므로 사용하지 않는다. *)
                //let xy = coord x y
                //$""" <Element ElementType="{HorzLineMode}" Coordinate="{xy}"></Element>"""
                //x <- x + 1
                //let xy = coord x y
                //$""" <Element ElementType="{MultiHorzLineMode}" Coordinate="{xy}" Param="84"></Element>"""

                coilAt q 1

                $"""
</Rung>
<Rung BlockMask="0">
    <Element ElementType="{MultiHorzLineMode}" Coordinate="{coord 0 2}" Param="90"></Element>
    <Element ElementType="{FBMode}" Coordinate="2142" Param="END">END</Element>
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
<Rung BlockMask="0"><Element ElementType="{RungCommentMode}" Coordinate="{coord 0 0}">DS Logic for XGI</Element></Rung>
<Rung BlockMask="0">
"""
                let y = 1
                let mutable x = 0
                for t in iTags.Take(30) do
                    contactAt t x y     // <Element ElementType="{ContactMode}" Coordinate="1025">myBit00</Element>
                    x <- x + 1

                (* 꽉 채우고, 한 칸 빌 경우에는 HorzLineMode 만 사용한다. *)
                let xy = coord x y
                $""" <Element ElementType="{HorzLineMode}" Coordinate="{xy}"></Element>"""
                //x <- x + 1
                //let xy = coord x y
                //$""" <Element ElementType="{MultiHorzLineMode}" Coordinate="{xy}" Param="84"></Element>"""

                coilAt q 1

                $"""
</Rung>
<Rung BlockMask="0">
    <Element ElementType="{MultiHorzLineMode}" Coordinate="{coord 0 2}" Param="90"></Element>
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
<Rung BlockMask="0"><Element ElementType="{RungCommentMode}" Coordinate="{coord 0 0}">DS Logic for XGI</Element></Rung>
<Rung BlockMask="0">
"""
                let y = 1
                let mutable x = 0
                for t in iTags.Take(29) do
                    contactAt t x y     // <Element ElementType="{ContactMode}" Coordinate="1025">myBit00</Element>
                    x <- x + 1

                (* 꽉 채우고, 두 칸 이상 빌 경우에는 HorzLineMode 및 MultiHorzLineMode 를 모두 사용한다. *)
                let xy = coord x y
                $""" <Element ElementType="{HorzLineMode}" Coordinate="{xy}"></Element>"""
                x <- x + 1
                let xy = coord x y
                $""" <Element ElementType="{MultiHorzLineMode}" Coordinate="{xy}" Param="84"></Element>"""

                coilAt q 1

                $"""
</Rung>
<Rung BlockMask="0">
    <Element ElementType="{MultiHorzLineMode}" Coordinate="{coord 0 2}" Param="90"></Element>
    <Element ElementType="{FBMode}" Coordinate="{coord 31 2}" Param="END">END</Element>
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
<Rung BlockMask="0"><Element ElementType="{RungCommentMode}" Coordinate="{coord 0 0}">DS Logic for XGI</Element></Rung>
<Rung BlockMask="0">
"""
                let mutable y = 1
                for t in iTags do
                    contactAt t 0 y     // <Element ElementType="{ContactMode}" Coordinate="1025">myBit00</Element>
                    if y = 1 then
                        (* VertLineMode 로 시작하면 HorzLineMode 없이, 바로 MultiHorzLineMode 가 와야 한다. *)
                        let xy = coord 1 1 - 1      // 1027
                        $"""<Element ElementType="{VertLineMode}" Coordinate="{xy}" />"""
                        let xy = coord 1 1          // 1028
                        let width = (maxNumHorizontalContact - 2) * 3    // 87
                        //$""" <Element ElementType="{HorzLineMode}" Coordinate="{xy} Param={width}"></Element>"""
                        //let xy = coord 31 1
                        $""" <Element ElementType="{MultiHorzLineMode}" Coordinate="{xy}" Param="{width}"></Element>"""

                    y <- y + 1



                coilAt q 1

                let xyEndS, xyEndE = coord 0 y, coord maxNumHorizontalContact y
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
<Rung BlockMask="0"><Element ElementType="{RungCommentMode}" Coordinate="{coord 0 0}">DS Logic for XGI</Element></Rung>
<Rung BlockMask="0">
"""
                let mutable y = 1
                for t in iTags do
                    contactAt t 0 y     // <Element ElementType="{ContactMode}" Coordinate="1025">myBit00</Element>
                    if y < 31 then
                        (* VertLineMode 로 시작하면 HorzLineMode 없이, 바로 MultiHorzLineMode 가 와야 한다. *)
                        let xy = coord 1 y - 1      // 1027
                        $"""<Element ElementType="{VertLineMode}" Coordinate="{xy}" />"""
                    if y = 1 then
                        let xy = coord 1 1          // 1028
                        let width = (maxNumHorizontalContact - 2) * 3    // 87 = (31-2) * 3
                        $""" <Element ElementType="{MultiHorzLineMode}" Coordinate="{xy}" Param="{width}"></Element>"""

                    y <- y + 1

                coilAt q 1

                let xyEndS, xyEndE = coord 0 y, coord coilCellX y
                $"""
</Rung>
<Rung BlockMask="0">
    <Element ElementType="{MultiHorzLineMode}" Coordinate="{xyEndS}" Param="90"></Element>
    <Element ElementType="{FBMode}" Coordinate="{xyEndE}" Param="END">END</Element>
</Rung>"""
            ] |> String.concat "\r\n"

        let xml = wrapWithXml rungs localSymbolsXml emptySymbolsGlobalXml None
        saveTestResult (get_current_function_name ()) xml

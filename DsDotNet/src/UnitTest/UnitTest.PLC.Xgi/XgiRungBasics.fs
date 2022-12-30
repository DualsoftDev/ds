namespace T

open NUnit.Framework
open Engine.Core
open Engine.Common.FS
open PLC.CodeGen.LSXGI
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
        """
        <Rung BlockMask="0"><Element ElementType="63" Coordinate="1">DS Logic for XGI</Element></Rung>
        <Rung BlockMask="0">
            <Element ElementType="6" Coordinate="1025">myBit00</Element>
            <Element ElementType="1" Coordinate="1028"></Element>
            <Element ElementType="2" Coordinate="1031" Param="84"></Element>
            <Element ElementType="14" Coordinate="1118">myBit01</Element>
        </Rung>
        <Rung BlockMask="0"><Element ElementType="2" Coordinate="2049" Param="90"></Element>
            <Element ElementType="33" Coordinate="2142" Param="END">END</Element>
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
        let rungsXml = """<Rung BlockMask="0"><Element ElementType="63" Coordinate="1">DS Logic for XGI</Element></Rung>"""
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
            let kind = int Variable.Kind.VAR
            let plcType = "BOOL"
            XGITag.createSymbol t.Name "Fake Comment" "I" kind t.Address plcType

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

    [<Test>]
    member __.``Generate ANDs variables test``() =
        let storages = Storages()
        let q = PlcTag("myQ0", "%QX0.1.0", false)
        let statements_ = parseCode storages codeForBits31
        let iTags = storages.Values.ToEnumerable<PlcTag<bool>>().ToArray()
        let symbolInfos =
            let kind = int Variable.Kind.VAR
            let plcType = "BOOL"
            [   for t in iTags do
                    XGITag.createSymbol t.Name "Fake Comment" "I" kind t.Address plcType

                XGITag.createSymbol q.Name "Fake Comment" "Q" kind q.Address plcType
            ]
        let localSymbolsXml = XGITag.generateSymbolVars(symbolInfos, false)

        let rungs =
            [
                """
<Rung BlockMask="0"><Element ElementType="63" Coordinate="1">DS Logic for XGI</Element></Rung>
<Rung BlockMask="0">
"""
                let y = 1
                let mutable x = 0
                for t in iTags do
                    contactAt t x y     // <Element ElementType="6" Coordinate="1025">myBit00</Element>
                    x <- x + 1

                //let xy = coord x y
                //$""" <Element ElementType="1" Coordinate="{xy}"></Element>"""
                //x <- x + 1
                //let xy = coord x y
                //$""" <Element ElementType="2" Coordinate="{xy}" Param="84"></Element>"""
            
                coilAt q 1

                """
</Rung>
<Rung BlockMask="0">
    <Element ElementType="2" Coordinate="2049" Param="90"></Element>
    <Element ElementType="33" Coordinate="2142" Param="END">END</Element>
</Rung>"""
            ] |> String.concat "\r\n"

        let xml = wrapWithXml rungs localSymbolsXml emptySymbolsGlobalXml None
        saveTestResult (get_current_function_name ()) xml

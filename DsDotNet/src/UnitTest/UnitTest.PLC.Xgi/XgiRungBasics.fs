namespace T

open NUnit.Framework
open Engine.Common.FS
open PLC.CodeGen.LSXGI

type XgiRungTest() =
    inherit XgiTestClass()

    let emptySymbolsLocalXml = """<LocalVar Version="Ver 1.0" Count="16"></LocalVar>"""
    let emptySymbolsGlobalXml = """<GlobalVariable Version="Ver 1.0" Count="16"></GlobalVariable>"""
    let simplestProgramXml = """
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
    let simpleSymbolsLocalXml = """
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
    member __.``Prolog comment test`` () =
        let rungsXml = """<Rung BlockMask="0"><Element ElementType="63" Coordinate="1">DS Logic for XGI</Element></Rung>"""
        let xml = wrapWithXml rungsXml emptySymbolsLocalXml emptySymbolsGlobalXml None
        saveTestResult (get_current_function_name()) xml

    [<Test>]
    member __.``Generate simplest program test`` () =
        let xml = wrapWithXml simplestProgramXml emptySymbolsLocalXml emptySymbolsGlobalXml None
        saveTestResult (get_current_function_name()) xml


    [<Test>]
    member __.``Generate simplest with local variables test`` () =

        let xml = wrapWithXml simplestProgramXml simpleSymbolsLocalXml emptySymbolsGlobalXml None
        saveTestResult (get_current_function_name()) xml


    [<Test>]
    member __.``Generate simplest with local, global variables test`` () =
        let symbolsGlobalXml = """
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
        saveTestResult (get_current_function_name()) xml
        ()





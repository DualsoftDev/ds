namespace T

open PLC.CodeGen.LS
open PLC.CodeGen.LS.Config.POU.Program.LDRoutine.ElementType
open Engine.Core.ModelConfigModule


[<AutoOpen>]
module XgxTestCommonModule =
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
        <Rung BlockMask="0"><Element ElementType="{RungCommentMode}" Coordinate="{coord(0, 0)}">DS Logic for XGI</Element></Rung>
        <Rung BlockMask="0">
            <Element ElementType="{ContactMode}" Coordinate="{coord(0, 1)}">myBit00</Element>
            <Element ElementType="{HorzLineMode}" Coordinate="{coord(1, 1)}"></Element>
            <Element ElementType="{MultiHorzLineMode}" Coordinate="{coord(2, 1)}" Param="84"></Element>
            <Element ElementType="{CoilMode}" Coordinate="{coord(31, 1)}">myBit01</Element>
        </Rung>
        <Rung BlockMask="0"><Element ElementType="{MultiHorzLineMode}" Coordinate="{coord(0, 2)}" Param="90"></Element>
            <Element ElementType="{FBMode}" Coordinate="{coord(31, 2)}" Param="END">END</Element>
        </Rung>
"""
    
    let getSimpleLocalSymbolInfos(xgx:HwCPU): SymbolInfo list =
        let device =
            match xgx with
            | XGI -> "I"
            | XGK -> "P"
            | _ -> failwith "Not supported plc type"
        [
            { Name="myBit00"; Kind=1; Type="BOOL"; Comment="FAKECOMMENT"; Device=device; Address="%IX0.0.0"; AddressIEC="%IX0.0.0"; InitValue=null; State=0; DevicePos=0; AddressAlias = ResizeArray<string>() }
            { Name="myBit01"; Kind=1; Type="BOOL"; Comment="FAKECOMMENT"; Device=device; Address="%IX0.0.1"; AddressIEC="%IX0.0.1"; InitValue=null; State=0; DevicePos=1; AddressAlias = ResizeArray<string>() }
        ]


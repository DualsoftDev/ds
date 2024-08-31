namespace T.Rung
open T

open Dual.Common.UnitTest.FS
open System.Linq
open NUnit.Framework
open Engine.Core
open Dual.Common.Core.FS
open PLC.CodeGen.LS
open PLC.CodeGen.LS.Config.POU.Program.LDRoutine.ElementType
open Engine.Parser.FS

type XgxRungTest(xgx:PlatformTarget) =
    inherit XgxTestBaseClass(xgx)

    let getProjectParams4Test xgx funName = { getXgxProjectParams xgx funName with EnableXmlComment = true }


    member __.PrepareWithSymbols(numTags:int) =
        let storages = Storages()
        let qx, i, q =
            match xgx with
            | XGI -> "%QX0.1.0", "I", "Q"
            | XGK -> "P00001", "P", "P"
            | _ -> failwith "Not supported plc type"

        let output = createTag("myQ0", qx, false)
        let statements_ = parseCodeForWindows storages (generateBitTagVariableDeclarations xgx 0 32)
        let iTags = storages.Values.ToEnumerable<Tag<bool>>().Take(numTags).ToArray()
        let symbolInfos =
            let kindVar = int Variable.Kind.VAR
            let counter = counterGenerator 0
            let plcType = "BOOL"
            [   for t in iTags do
                    { defaultSymbolInfo with Name=t.Name; Type=plcType; Address=t.Address; Device=i; Kind=kindVar; DevicePos=counter() }

                { defaultSymbolInfo with Name=output.Name; Type=plcType; Address=output.Address; Device=q; Kind=kindVar; DevicePos=counter()}
            ]
        iTags, output, symbolInfos


    member x.``Generate ANDs30 variables test``() =
        let iTags, q, localSymbolInfos = x.PrepareWithSymbols(30)

        let rungs =
            [
                $"""
<Rung BlockMask="0"><Element ElementType="{RungCommentMode}" Coordinate="{coord(0, 0)}">DS Logic for XGI</Element></Rung>
<Rung BlockMask="0">
"""
                let y = 1
                let mutable x = 0
                for t in iTags.Take(30) do
                    contactAt t.Name (x, y)     // <Element ElementType="{ContactMode}" Coordinate="1025">myBit00</Element>
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

        let xml =
            let prjParam = getProjectParams4Test xgx (getFuncName())
            wrapWithXml prjParam rungs localSymbolInfos emptySymbolsGlobalXml None
        x.saveTestResult (getFuncName ()) xml

    member x.``Generate ANDs29 variables test``() =
        let iTags, q, localSymbolInfos = x.PrepareWithSymbols(29)

        let rungs =
            [
                $"""
<Rung BlockMask="0"><Element ElementType="{RungCommentMode}" Coordinate="{coord(0, 0)}">DS Logic for XGI</Element></Rung>
<Rung BlockMask="0">
"""
                let y = 1
                let mutable x = 0
                for t in iTags.Take(29) do
                    contactAt t.Name (x, y)     // <Element ElementType="{ContactMode}" Coordinate="1025">myBit00</Element>
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

        let xml =
            let prjParam = getProjectParams4Test xgx (getFuncName())
            wrapWithXml prjParam rungs localSymbolInfos emptySymbolsGlobalXml None
        x.saveTestResult (getFuncName ()) xml

    member x.``Generate ANDsMax(=31) variables test``() =
        let iTags, q, localSymbolInfos = x.PrepareWithSymbols(31)

        let rungs =
            [
                $"""
<Rung BlockMask="0"><Element ElementType="{RungCommentMode}" Coordinate="1">DS Logic for XGI</Element></Rung>
<Rung BlockMask="0">
"""
                let y = 1
                let mutable x = 0
                for t in iTags do
                    contactAt t.Name (x, y)     // <Element ElementType="{ContactMode}" Coordinate="1025">myBit00</Element>
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

        let xml =
            let prjParam = getProjectParams4Test xgx (getFuncName())
            wrapWithXml prjParam rungs localSymbolInfos emptySymbolsGlobalXml None
        x.saveTestResult (getFuncName ()) xml

    member x.``Generate local variables test``() =
        let devType, tAddress, devPos=
            if xgx = XGI then  "I", "%IX0.0.0", 0
            elif xgx = XGK then "P", "P0000A", 10
            else failwithf $"not support {xgx}"

        let t =  createTag("myBit00", tAddress, false)

        // name, comment, device, kind, address, plcType 를 받아서 SymbolInfo 를 생성한다.
        let symbolInfo: SymbolInfo =
            { defaultSymbolInfo with Name=t.Name; Type="BOOL"; Address=t.Address; DevicePos=devPos; Device=devType; }

        let symbolInfoXml =
            let prjParam = getProjectParams4Test xgx (getFuncName())
            symbolInfo.GenerateXml prjParam
        symbolInfoXml =~=

            if xgx = XGI then  """<Symbol Name="myBit00" Comment="" Device="I" Kind="1" Type="BOOL" Address="%IX0.0.0" State="0">
	</Symbol>"""
            elif xgx = XGK then """<Symbol Name="myBit00" Comment="" Device="P" DevicePos="10" Type="BIT">
	</Symbol>"""
            else failwithf $"not support {xgx}"


        let symbolsLocalXml =
            let prjParam = getProjectParams4Test xgx (getFuncName())
            XGITag.generateLocalSymbolsXml prjParam [ symbolInfo ]

        symbolsLocalXml =~=


            if xgx = XGI then  """<LocalVar Version="Ver 1.0" Count="1">
<Symbols>
	<Symbol Name="myBit00" Comment="" Device="I" Kind="1" Type="BOOL" Address="%IX0.0.0" State="0">
	</Symbol>
</Symbols>
<TempVar Count="0"></TempVar>
</LocalVar>"""
            elif xgx = XGK then """<LocalVar Version="Ver 1.0" Count="1">
<Symbols>
	<Symbol Name="myBit00" Comment="" Device="P" DevicePos="10" Type="BIT">
	</Symbol>
</Symbols>
<TempVar Count="0"></TempVar>
</LocalVar>"""
            else failwithf $"not support {xgx}"

    member x.``Generate OR2 variables test``() =
        let iTags, q, localSymbolInfos = x.PrepareWithSymbols(2)

        let rungs =
            [
                $"""
<Rung BlockMask="0"><Element ElementType="{RungCommentMode}" Coordinate="{coord(0, 0)}">DS Logic for XGI</Element></Rung>
<Rung BlockMask="0">
"""
                let mutable y = 1
                for t in iTags do
                    contactAt t.Name (0, y)     // <Element ElementType="{ContactMode}" Coordinate="1025">myBit00</Element>
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

        let xml =
            let prjParam = getProjectParams4Test xgx (getFuncName())
            wrapWithXml prjParam rungs localSymbolInfos emptySymbolsGlobalXml None
        x.saveTestResult (getFuncName ()) xml

    member x.``Generate ORs variables test``() =
        let iTags, q, localSymbolInfos = x.PrepareWithSymbols(31)

        let rungs =
            [
                $"""
<Rung BlockMask="0"><Element ElementType="{RungCommentMode}" Coordinate="{coord(0, 0)}">DS Logic for XGI</Element></Rung>
<Rung BlockMask="0">
"""
                let mutable y = 1
                for t in iTags do
                    contactAt t.Name (0, y)     // <Element ElementType="{ContactMode}" Coordinate="1025">myBit00</Element>
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

        let xml =
            let prjParam = getProjectParams4Test xgx (getFuncName())
            wrapWithXml prjParam rungs localSymbolInfos emptySymbolsGlobalXml None
        x.saveTestResult (getFuncName ()) xml

    member x.``Generate simplest with local variables test``() =
        let xml =
            let prjParam = getProjectParams4Test xgx (getFuncName())
            wrapWithXml prjParam simplestProgramXml (getSimpleLocalSymbolInfos(xgx)) emptySymbolsGlobalXml None
        x.saveTestResult (getFuncName ()) xml

    member x.``Generate simplest with local, global variables test``() =
        let symbolsGlobalXml =

            if xgx = XGI then
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
            elif xgx = XGK then
                """
    <GlobalVariable Version="Ver 1.0" Count="2">
    <Symbols>
        <Symbol Name="myBit00" Kind="6" Type="Bit" Comment="FAKECOMMENT" Device="P" DevicePos="10" State="0">
            <MemberAddresses/>
            <MemberRetains/>
            <MemberInitValues/>
            <MemberComments/>
        </Symbol>
        <Symbol Name="myBit01" Kind="6" Type="Bit" Comment="FAKECOMMENT" Device="P" DevicePos="11" State="0">
            <MemberAddresses/>
            <MemberRetains/>
            <MemberInitValues/>
            <MemberComments/>
        </Symbol>
    </Symbols>
    <TempVar Count="0"></TempVar>
    </GlobalVariable>
    """
            else failwithf $"not support {xgx}"



        let xml =
            let prjParam = getProjectParams4Test xgx (getFuncName())
            wrapWithXml prjParam simplestProgramXml (getSimpleLocalSymbolInfos(xgx)) symbolsGlobalXml None
        x.saveTestResult (getFuncName ()) xml
        ()

    member x.``Prolog comment test``() =
        let rungsXml = wrapWithRung $"""<Element ElementType="{RungCommentMode}" Coordinate="1">DS Logic for XGI</Element>"""
        let xml =
            let prjParam = getProjectParams4Test xgx (getFuncName())
            wrapWithXml prjParam rungsXml [] emptySymbolsGlobalXml None
        x.saveTestResult (getFuncName ()) xml





type XgiRungTest() =
    inherit XgxRungTest(XGI)
    [<Test>] member __.``Generate ANDs29 variables test``() = base.``Generate ANDs29 variables test``()
    [<Test>] member __.``Generate ANDs30 variables test``() = base.``Generate ANDs30 variables test``()
    [<Test>] member __.``Generate ANDsMax(=31) variables test``() = base.``Generate ANDsMax(=31) variables test``()
    [<Test>] member __.``Generate local variables test``() = base.``Generate local variables test``()
    [<Test>] member __.``Generate OR2 variables test``() = base.``Generate OR2 variables test``()
    [<Test>] member __.``Generate ORs variables test``() = base.``Generate ORs variables test``()
    [<Test>] member __.``Generate simplest with local variables test``() = base.``Generate simplest with local variables test``()
    [<Test>] member __.``Generate simplest with local, global variables test``() = base.``Generate simplest with local, global variables test``()
    [<Test>] member __.``Prolog comment test``() = base.``Prolog comment test``()


type XgkRungTest() =
    inherit XgxRungTest(XGK)
    [<Test>] member __.``Generate ANDs29 variables test``() = base.``Generate ANDs29 variables test``()
    [<Test>] member __.``Generate ANDs30 variables test``() = base.``Generate ANDs30 variables test``()
    [<Test>] member __.``Generate ANDsMax(=31) variables test``() = base.``Generate ANDsMax(=31) variables test``()
    [<Test>] member __.``Generate local variables test``() = base.``Generate local variables test``()
    [<Test>] member __.``Generate OR2 variables test``() = base.``Generate OR2 variables test``()
    [<Test>] member __.``Generate ORs variables test``() = base.``Generate ORs variables test``()
    [<Test>] member __.``Generate simplest with local variables test``() = base.``Generate simplest with local variables test``()
    [<Test>] member __.``Generate simplest with local, global variables test``() = base.``Generate simplest with local, global variables test``()
    [<Test>] member __.``Prolog comment test``() = base.``Prolog comment test``()

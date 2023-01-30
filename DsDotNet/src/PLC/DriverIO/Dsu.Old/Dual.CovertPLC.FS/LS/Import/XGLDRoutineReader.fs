namespace Old.Dual.ConvertPLC.FS.LsXGI

open FSharpPlus
open Old.Dual.Common
open System.Xml
open AddressConvert
open System.Collections.Generic
open System
open Config.POU.Program.LDRoutine

[<AutoOpen>]
module XGLDRoutineReader  =

    type XmlRung = {
           RungID : int     
           RungLens : seq<string>
           Elements  : seq<XmlRungElement>
           Program : string
       }
    and XmlRungElement = {
        ElementType : int     
        Coordinate : int
        Param : string
        Tag : string
        FBXGK  : seq<string>  // XGK FB에 사용된 parameter
        AddressFBXGI  : seq<string*string>  //XGI FB에 사용된 address addtype
        AddressSet : string*string  //접점 or 코일에 사용된 address addtype
    }


    ///비워있지 않고 숫자 시작 안되며 '_' 시작 안됨
    let ableAddress name = 
        if(name = "") then false
            else if(name.StartsWith("_")) then  false
            else if(Char.IsNumber(name.ToCharArray().[0])) then  false
            else true

    let getTagNType(tag, cpuSerise:CpuType, dicTotalVar:IDictionary<string, SymbolInfo>) = 
        if(dicTotalVar.ContainsKey(tag)) 
        then dicTotalVar.[tag].Address, dicTotalVar.[tag].Type
        else
            if(ableAddress tag) then
                match AddressConvert.tryParseTag(cpuSerise) tag  with
                | Some v -> v.Tag, if(cpuSerise.IsXGI()) then v.DataType.TotextXGI() else v.DataType.TotextXGK()
                | None -> tag, "" 
                else "", "" 

    /// XML에 존재하는 모든 Rung의 정보를 Element 구조 포함하여 추출한다.
    let getRungs  (xdoc:XmlDocument, cpuSerise:CpuType, dicMaxDevice:IDictionary<string, int>, dicTotalVar:IDictionary<string, SymbolInfo>)=
        let getElements (xmlNode:XmlNode) = 
                xmlNode.SelectNodes("Element")
                |> XmlExt.ToEnumerables
                |> Seq.map(fun e ->
                    let na = if(e.FirstChild <> null) then e.FirstChild.InnerText else ""
                    let el = e.Attributes.["ElementType"].InnerText |> int
                    let co = e.Attributes.["Coordinate"].InnerText |> int
                    let pa = if(e.Attributes.ItemOf("Param") <> null) then e.Attributes.["Param"].InnerText else ""
                    let fbMode = (el = (int)ElementType.FBMode || el = (int)ElementType.VariableMode)
                    let fbXGK = 
                        if(el = (int)ElementType.FBMode) //XGK
                        then pa.Split(',') |> Seq.map (fun tag ->  tag)
                        else Seq.empty

                    let addressFBXGI = 
                        if(el = (int)ElementType.VariableMode) //XGI
                        then getTagNType(na, cpuSerise, dicTotalVar)
                        else "", "" 

                    let address =
                        if(dicTotalVar.ContainsKey(na)) 
                        then dicTotalVar.[na].Address, dicTotalVar.[na].Type
                        else
                            if(ableAddress na)  then
                                match AddressConvert.tryParseTag(cpuSerise) na  with
                                | Some v -> v.Tag, if(cpuSerise.IsXGI()) then v.DataType.TotextXGI() else v.DataType.TotextXGK()
                                | None -> na,  ""
                            else "", ""                                             
                                
                    {ElementType = el;    Coordinate = co;   Param = pa;   Tag = na; AddressSet = address; FBXGK = fbXGK; AddressFBXGI = [|addressFBXGI|];})

        let getLens (elements:seq<XmlRungElement>) = 
            let startY = elements |> head |> fun f -> (f.Coordinate-1) / 1024
            elements
            |> Seq.map(fun  e -> 
                        let tagMarking = if( e.Tag <> "") then "0T0" else "0N0"  //Tag 위치 규격은 존재하면 0T0, 없으면 0N0 숫자문자조합으로 마킹
                        let posMarking = (e.Coordinate-1) - (1024 * startY)
                        (sprintf "%s‡%d‡%d" tagMarking posMarking e.ElementType))
        let mutable cntRung = -1;
        let rungs = 
            xdoc.SelectNodes(xmlCnfPath+"/POU/Programs/Program")
            |> XmlExt.ToEnumerables
            |> Seq.collect(fun xmlProgram -> 
                    let nameProgram = xmlProgram.FirstChild.InnerText
                    xmlProgram.SelectNodes("Body/LDRoutine/Rung")
                    |> XmlExt.ToEnumerables
                    |> Seq.filter(fun f -> f.ChildNodes.Count <> 0)
                    |> Seq.map(fun xmlRung ->   
                        cntRung <- cntRung+1
                        let elements = getElements(xmlRung)
                        let rungLens = getLens(elements)
                        {RungID = cntRung; RungLens = rungLens;  Program =  nameProgram;   Elements =elements }))
                         

        rungs

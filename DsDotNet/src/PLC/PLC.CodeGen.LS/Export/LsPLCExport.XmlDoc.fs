namespace PLC.CodeGen.LS

open Dual.Common.Core.FS
open Engine.Core
open System.Runtime.CompilerServices
open System.Xml
open System.Collections.Generic
open Dual.Common.Core.FS
open System.IO

[<AutoOpen>]
module internal XgxXmlExtensionImpl =
    let getXPathGlobalVariable (xgx:RuntimeTargetType) =
        let var =
            match xgx with
            | XGI -> "GlobalVariable"
            | XGK -> "VariableComment"
            | _ -> failwithlog "Not supported plc type"
        $"//Configurations/Configuration/GlobalVariables/{var}"
    let xPathLocalVar = "//POU/Programs/Program/LocalVar"

[<Extension>]
type XgxXmlExtension =
    /// XmlNode '//Configurations/Configuration/GlobalVariables/{GlobalVariable, VariableComment}' 반환
    [<Extension>]
    static member GetXmlNodeTheGlobalVariable (xdoc:XmlDocument, xgx:RuntimeTargetType) : XmlNode = getXPathGlobalVariable xgx |> xdoc.SelectSingleNode 

    /// XGI 기준으로 LocalVar 에 정의한 symbol 들을 XGK 인 경우에 한해, GlobalVariable 로 이동시킨다.
    [<Extension>]
    static member MovePOULocalSymbolsToGlobal (xdoc:XmlDocument, xgx:RuntimeTargetType) : unit =
        if xgx = XGI then
            failwith "XGI type can hold local symbols.  Do not call me."

        let xPathGlobalVar = getXPathGlobalVariable xgx
        let globalSymbols:Dictionary<string, XmlNode> =
            xdoc.GetXmlNodes($"{xPathGlobalVar}/Symbols/Symbol")
            |> map(fun x -> x.Attributes["Name"].Value, x)
            |> Tuple.toDictionary

        // LocalVar 에서만 정의된 symbols 
        let localOnlySymbolss:XmlNode[] =
            xdoc.GetXmlNodes($"{xPathLocalVar}/Symbols/Symbol")
            |> filter(fun x ->
                x.Attributes.["Name"].Value
                |> globalSymbols.ContainsKey
                |> not)
            |> toArray

        // LocalVar 의 Symbol 들을 GlobalVar 로 이동
        let xnGlobalVarsContainer = xdoc.GetXmlNode($"{xPathGlobalVar}/Symbols")
        localOnlySymbolss |> Array.iter(fun x -> xnGlobalVarsContainer.AdoptChild x |> ignore)

        // LocalVar 정의 삭제
        xdoc.GetXmlNodes(xPathLocalVar).Iter(fun x -> x.ParentNode.RemoveChild x |> ignore)

        xnGlobalVarsContainer.Attributes["Count"].Value <- globalSymbols.Count.ToString()
        ()

    /// Xml document 상에서 제대로 생성되었는지 검사한다.
    ///
    /// - Symbol 의 DevicePos 가 음수인 Symbol 이 있는지 확인한다.
    [<Extension>]
    static member Check(xdoc:XmlDocument, xgx:RuntimeTargetType) =
        let xPathGlobalVar = getXPathGlobalVariable xgx
        let globalSymbols:XmlNode[] = xdoc.GetXmlNodes($"{xPathGlobalVar}/Symbols/Symbol").ToArray()
        let localSymbolss:XmlNode[] = xdoc.GetXmlNodes($"{xPathLocalVar}/Symbols/Symbol").ToArray()

        for s in globalSymbols @ localSymbolss do
            let name = s.Attributes.["Name"].Value
            let devPos = s.Attributes["DevicePos"]
            if devPos <> null && devPos.Value.any() && int devPos.Value < 0  then
                failwithlog $"Symbol {name} has Invalid DevicePos attribute {devPos.Value}."
        xdoc


// https://codebeautify.org/xmlviewer : Online XML 구조 보기 (treeview)

namespace Old.Dual.ConvertPLC.FS.LsXGI

open System.Linq
open System.Xml
open System.Xml.Linq
open System.Runtime.CompilerServices
open FSharpPlus
open Old.Dual.Common

module XGIXml =
    let private globalVariable = "Project/Configurations/Configuration/GlobalVariables/GlobalVariable"
    let private getAttribute (xn:XmlNode) (attr:string) = xn.Attributes.[attr].Value

    let getGlobalSymbolXmlNodes (xmlDoc:XmlDocument) =
        xmlDoc.SelectNodes(globalVariable + "/Symbols/Symbol")
        // <Symbol Name="_0002_A1_RDY" Kind="6" Type="BOOL" State="12" Address="%UX0.2.0" Trigger="" InitValue="" Comment="위치결정 모듈: 1축 Ready" Device="U" DevicePos="1024" TotalSize="1" OrderIndex="-1" HMI="0" EIP="0" SturctureArrayOffset="0" ModuleInfo="SP:0:2:0" ArrayPointer="0"><MemberAddresses></MemberAddresses>
    let getDirectVarXmlNodes (xmlDoc:XmlDocument) =
        xmlDoc.SelectNodes(globalVariable + "/DirectVarComment/DirectVar")
        // <DirectVar Device="%IX0.0.0" Name="" Comment="RR 공급 감지 센서" Used="1"></DirectVar>

    let getGlobalAddresses (xmlDoc:XmlDocument) =
        ( getGlobalSymbolXmlNodes xmlDoc
          |> XmlExt.ToEnumerables
          |> Seq.map (fun xn -> getAttribute xn "Address"))
        @@
        ( getDirectVarXmlNodes xmlDoc
          |> XmlExt.ToEnumerables
          |> Seq.map (fun xn -> getAttribute xn "Device"))

    let createUsedVariableMap (xmlDoc:XmlDocument) =
        //xmlDoc.SelectNodes(globalVariable + "/DirectVarComment/DirectVar")
        xmlDoc.SelectNodes(globalVariable + "/Symbols/Symbol")        // Name, Address
        |> XmlExt.ToEnumerables
        |> Seq.map (fun xn -> getAttribute xn "Name", getAttribute xn "Address")
        |> Tuple.toDictionary
namespace Dsu.PLCConverter.FS

open Dual.Common
open ActivePattern
open Microsoft.FSharp.Collections
open Dsu.PLCConverter.FS.XgiSpecs
open System.Diagnostics

[<AutoOpen>]
/// 문자열 주소로부터 Address 생성
type Symbol (addressString:string) =
        // 문자열 주소로부터 Address 생성
        let head, d1, d2 = XGI.Parsing addressString
        let symbol = XGI.MakeXgiSymbolName (head, d1, d2)
        let address, varType = XGI.MakeXgiAddress (head, d1, d2)

        member x.Index = d1
        member x.Symbol = symbol
        member x.Address = address
        member x.HeadType = XGI.HeadType address
        member x.VarType = varType


[<AutoOpen>]
/// Xml Symbol tag 가 가지는 속성
module XgiSymbol =
    [<DebuggerDisplay("Name({Name},{Address})")>]
    type SymbolInfo (index, name, comment, deviceType, address, kind, typed, state, melsecAddress, sameAddCount)=
          let mutable sameAddCount = 0
          member x.Index  with get() =
                             match address with
                             | StartsWith "%A"   () -> index + 10000000  //자동타입은 Sorting을 위해서 뒷번호 배정
                             | _ -> index

          member x.Name = name:string
          member x.Address  with get() =
                            match address with
                            | StartsWith "%A"   () -> ""
                            | _ -> address

          member x.Comment with get() = System.Security.SecurityElement.Escape comment
          
          member x.SameAddress  with get() = sameAddCount and set(value) = sameAddCount  <- value
          member private x.deviceType = deviceType:string
          member private x._Kind = kind:int
          member private x._Type = typed:string
          member private x._State = state:int
          member x.OldAddress() = melsecAddress
          member x.DeviceType() = deviceType
          member x.Kind() = kind
          member x.Type() = typed
          member x.State() = state

       with
          /// Symbol 관련 XML tag attributes 생성
        member private x.GetXmlArgs() =
            seq {
                yield sprintf "Name=\"%s\"" x.Name
                yield sprintf "Kind=\"%d\"" (x.Kind())
                yield sprintf "Type=\"%s\"" (x.Type())
                yield sprintf "Comment=\"%s\"" x.Comment
                yield sprintf "Device=\"%s\"" (x.DeviceType())
                yield sprintf "Address=\"%s\"" x.Address
                yield sprintf "State=\"%d\"" (x.State())
            } |> String.concat " "

        member private x.GetDirectXmlArgs() =
            seq {
                yield sprintf "Device=\"%s\"" x.Address
                yield sprintf "Comment=\"%s\"" x.Comment
            } |> String.concat " "

        /// Symbol 관련 XML tag 생성
        member x.ToText() =
                sprintf "<Symbol %s/>" (x.GetXmlArgs())
                //Address="" Trigger="" InitValue="" Comment="" Device="" DevicePos="-1" TotalSize="0" OrderIndex="0" HMI="0" EIP="0" SturctureArrayOffset="0" ModuleInfo="" ArrayPointer="0"><MemberAddresses></MemberAddresses>

        member x.GenerateXml() =
            seq {
                yield sprintf "\t<Symbol %s>" (x.GetXmlArgs())
                // 사용되지 않지만, 필요한 XML children element 생성
                yield! ["Addresses"; "Retains"; "InitValues"; "Comments"]
                    |> Seq.map (fun k -> sprintf "\t\t<Member%s/>" k)
                yield "\t</Symbol>"
            } |> String.concat "\r\n"

        member x.GenerateDirectVarXml() =
            seq {
                    yield sprintf "\t<DirectVar %s>" (x.GetDirectXmlArgs())
                    yield "\t</DirectVar>"
                } |> String.concat "\r\n"

    /// 로컬 심볼 만들기
    let createLocalSymbol (s:SymbolInfo)
        = SymbolInfo (
            s.Index   
            ,s.Name
            ,s.Comment
            ,s.DeviceType()
            ,s.Address
            ,(int Variable.Kind.VAR_EXTERNAL)
            ,(s.Type())
            ,0
            ,(s.OldAddress())
            ,0
        )

    /// 문자열 주소로부터 Symbol 생성
    let makeSymbol addressString comment (kind:Variable.Kind) (varType: Config.VarType) =
        let symbol = Symbol(addressString)
        // let melsec = MelsecAddress (addressString, comment);
        let symbolInfo = SymbolInfo (
                            symbol.Index
                            ,symbol.Symbol
                            ,comment
                            ,symbol.HeadType
                            ,symbol.Address
                            ,int kind
                            ,if(varType <> VarType.NONE)then varType.ToString() else symbol.VarType.ToString()
                            ,0
                            ,addressString
                            ,0)

        symbolInfo

    let makeSymbolAsync addressString comment (kind:Variable.Kind)=
        async {
            let makeSymbolInfo = makeSymbol addressString comment kind VarType.NONE
            return makeSymbolInfo
        }

    let getGlobalSymbols (commentDic:CommentDictionary) =
        let newSymbol =
            seq{
            for kv in commentDic do
                let dev, comment = kv.Key, kv.Value
                if(not (dev.StartsWith("U"))) then  //test ahn UB.23 통신모듈 스킵
                    yield makeSymbolAsync dev comment Variable.Kind.VAR_GLOBAL
                    }
        newSymbol
        |> Async.Parallel
        |> Async.RunSynchronously
        |> Seq.toList




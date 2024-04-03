namespace PLC.CodeGen.LSXGI

open System.Diagnostics

open Dual.Common.Core.FS
open System.Security
open Engine.Core
open System

// IEC-61131 Addressing
// http://www.microshadow.com/ladderdip/html/basic_iec_addressing.htm
// https://deltamotion.com/support/webhelp/rmctools/Registers/Address_Formats/Addressing_IEC.htm
(*
% [Q | I ] [X] [file] . [element] . [bit]
% [Q | I ] [B|W|D] [file] . [element]
% MX [file] . [element] . [bit]
% M [B|W|D] [file] . [element]
*)


[<AutoOpen>]
module XGITag = //IEC61131Tag =
    type FE(f, e) =
        member x.File = f
        member x.Element = e

    type FEB(f, e, b) =
        inherit FE(f, e)
        member x.Bit = b

    //let analyzeAddress tag =



    ///// memory type(I/O/M) 에 따른 연속적인  device 를 생성하는 함수를 반환한다.
    //let AddressGenerator
    //    (memType:string)
    //    (nBaseBit:int, nMaxBit:int)
    //    (nBaseByte:int, nMaxByte:int)
    //    (nBaseWord:int, nMaxWord:int) (alreadyAllocatedAddresses:Set<string>)
    //  =
    //    let mutable startBit = nBaseBit
    //    let mutable startByte = nBaseByte
    //    let mutable startWord = nBaseWord
    //    let rec generate() =
    //        //let x = startBit % 16
    //        //let n = startBit / 16
    //        let errMsg = $"Device generator for {memType} bit exceeds max limit!"

    //        if startBit >= nBaseBit + nMaxBit
    //            || startByte >= nBaseByte + nMaxByte
    //            || startWord >= nBaseWord + nMaxWord
    //        then
    //            failwithlog errMsg

    //        /// I,O 주소 생성은 임시적임
    //        let address =
    //            match memType with
    //            | "I" -> sprintf "%%%sX%d.%d.%d" memType (startBit/16/64) (startBit/16) (startBit%16)
    //            | "O" -> sprintf "%%%sX%d.%d.%d" "Q" (startBit/16/64) (startBit/16) (startBit%16)
    //            | "M" -> sprintf "%%%sX%d" memType startBit
    //            | "IB" -> sprintf "%%%s%d.%d" memType (startByte/64) (startByte%64)
    //            | "OB" -> sprintf "%%%s%d.%d" "QB" (startByte/64) (startByte%64)
    //            | "MB" -> sprintf "%%%s%d" memType startByte
    //            | "IW" -> sprintf "%%%s%d.%d" memType (startWord/64) (startWord%64)
    //            | "OW" -> sprintf "%%%s%d.%d" "QW" (startWord/64) (startWord%64)
    //            | "MW" -> sprintf "%%%s%d" memType startWord
    //            | "ID" -> sprintf "%%%s%d.%d" memType (startWord/64) (startWord%64)
    //            | "OD" -> sprintf "%%%s%d.%d" "QD" (startWord/64) (startWord%64)
    //            | "MD" -> sprintf "%%%s%d" memType startWord
    //            | _ ->  failwithlog "Unknown memType:" + memType


    //        match memType with
    //        | "I"  | "O"  | "M"  -> startBit  <- startBit  + 1
    //        | "IB" | "OB" | "MB" -> startByte <- startByte + 1
    //        | "IW" | "OW" | "MW" -> startWord <- startWord + 1
    //        | "ID" | "OD" | "MD" -> startWord <- startWord + 2
    //        | _ ->  failwithlog "Unknown  %s memType:" memType

    //        if (alreadyAllocatedAddresses.Contains(address)) then
    //            Debug.WriteLine $"Adress {address} already in use. Tring to choose other address.."
    //            generate()
    //        else
    //            address

    //    generate


    /// name, comment, plcType, kind 를 받아서 SymbolInfo 를 생성한다.
    let createSymbolInfo name comment plcType kind (initValue: BoxedObjectHolder) =
        { defaultSymbolInfo with
            Name = name
            Comment = escapeXml comment
            Type = plcType
            Kind = kind
            InitValue = initValue.Object }

    let copyLocal2GlobalSymbol (s: SymbolInfo) =
        { s with
            Kind = int Variable.Kind.VAR_GLOBAL
            State = 0 }


    type SymbolInfo with

        member private x.ToXgiLiteral() =
            match x.Type with
            | "BOOL" ->
                match x.InitValue :?> bool with
                | true -> "true"
                | false -> "false"
            | _ -> $"{x.InitValue}"

        /// Symbol 관련 XML tag attributes 생성
        member private x.GetXmlArgs() =
            [   $"Name=\"{x.Name}\""
                $"Comment=\"{x.Comment}\""
                match RuntimeDS.Target with
                | XGI ->
                    $"Device=\"{x.Device}\""
                    $"Kind=\"{x.Kind}\""
                    if x.Kind <> int Variable.Kind.VAR_EXTERNAL then
                        $"Type=\"{x.Type}\""

                        if x.InitValue <> null then
                            $"InitValue=\"{x.ToXgiLiteral()}\""

                        $"Address=\"{x.Address}\""
                    $"State=\"{x.State}\""
                | XGK ->
                    // <Symbol Name="autoMonitor" Device="P" DevicePos="0" Type="BIT" Comment="" ModuleInfo="" EIP="0" HMI="0"></Symbol>
                    $"Device=\"{x.Device}\""
                    $"Type=\"{x.Type}\""
                    $"DevicePos=\"{x.Address}\""
                | _ -> failwithlog "Not supported plc type"
            ] |> String.concat " "

        /// Symbol 관련 XML tag 생성
        member x.ToText() = $"<Symbol {x.GetXmlArgs()}/>"
        //Address="" Trigger="" InitValue="" Comment="" Device="" DevicePos="-1" TotalSize="0" OrderIndex="0" HMI="0" EIP="0" SturctureArrayOffset="0" ModuleInfo="" ArrayPointer="0"><MemberAddresses></MemberAddresses>

        member x.GenerateXml() =
            [ yield $"\t<Symbol {x.GetXmlArgs()}>"
              //// 사용되지 않지만, 필요한 XML children element 생성
              //yield!
              //    [ "Addresses"; "Retains"; "InitValues"; "Comments" ]
              //    |> Seq.map (sprintf "\t\t<Member%s/>")
              yield "\t</Symbol>" ]
            |> String.concat "\r\n"

    /// Symbol variable 정의 구역 xml 의 string 을 생성
    let private generateSymbolVarDefinitionXml (varType: string) (FList(symbols: SymbolInfo list)) =
        let symbols = symbols |> List.sortBy (fun s -> s.Name)

        [ yield $"<{varType} Version=\"Ver 1.0\" Count={dq}{symbols.length ()}{dq}>"
          yield "<Symbols>"
          yield! symbols |> map (fun s -> s.GenerateXml())
          yield "</Symbols>"
          yield "<TempVar Count=\"0\"></TempVar>"
          yield $"</{varType}>" ]
        |> String.concat "\r\n"

    let generateLocalSymbolsXml symbols =
        generateSymbolVarDefinitionXml "LocalVar" symbols

    let generateGlobalSymbolsXml symbols =
        generateSymbolVarDefinitionXml "GlobalVariable" symbols

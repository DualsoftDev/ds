namespace PLC.CodeGen.LSXGI

open System.Diagnostics

open Engine.Common.FS
open System.Security

// IEC-61131 Addressing
// http://www.microshadow.com/ladderdip/html/basic_iec_addressing.htm
// https://deltamotion.com/support/webhelp/rmctools/Registers/Address_Formats/Addressing_IEC.htm
(*
% [Q | I ] [X] [file] . [element] . [bit]
% [Q | I ] [B|W|D] [file] . [element]
% MX [file] . [element] . [bit]
% M [B|W|D] [file] . [element]
*)

module XGITag = //IEC61131Tag =
    type FE(f, e) =
        member x.File = f
        member x.Element = e
    type FEB(f, e, b) =
        inherit FE(f, e)
        member x.Bit = b

    //let analyzeAddress tag =



    /// memory type(I/O/M) 에 따른 연속적인  device 를 생성하는 함수를 반환한다.
    let AddressGenerator
            (memType:string)
            (nBaseBit:int, nMaxBit:int)
            (nBaseByte:int, nMaxByte:int)
            (nBaseWord:int, nMaxWord:int) (alreadyAllocatedAddresses:Set<string>)
        =
        let mutable startBit = nBaseBit
        let mutable startByte = nBaseByte
        let mutable startWord = nBaseWord
        let rec generate() =
            //let x = startBit % 16
            //let n = startBit / 16
            let errMsg = $"Device generator for {memType} bit exceeds max limit!"

            if startBit >= nBaseBit + nMaxBit
                || startByte >= nBaseByte + nMaxByte
                || startWord >= nBaseWord + nMaxWord
            then
                failwithlog errMsg

            /// I,O 주소 생성은 임시적임
            let address =
                match memType with
                | "I" -> sprintf "%%%sX%d.%d.%d" memType (startBit/16/64) (startBit/16) (startBit%16)
                | "O" -> sprintf "%%%sX%d.%d.%d" "Q" (startBit/16/64) (startBit/16) (startBit%16)
                | "M" -> sprintf "%%%sX%d" memType startBit
                | "IB" -> sprintf "%%%s%d.%d" memType (startByte/64) (startByte%64)
                | "OB" -> sprintf "%%%s%d.%d" "QB" (startByte/64) (startByte%64)
                | "MB" -> sprintf "%%%s%d" memType startByte
                | "IW" -> sprintf "%%%s%d.%d" memType (startWord/64) (startWord%64)
                | "OW" -> sprintf "%%%s%d.%d" "QW" (startWord/64) (startWord%64)
                | "MW" -> sprintf "%%%s%d" memType startWord
                | "ID" -> sprintf "%%%s%d.%d" memType (startWord/64) (startWord%64)
                | "OD" -> sprintf "%%%s%d.%d" "QD" (startWord/64) (startWord%64)
                | "MD" -> sprintf "%%%s%d" memType startWord
                | _ ->  failwithlog "Unknown memType:" + memType


            match memType with
            | "I"  | "O"  | "M"  -> startBit  <- startBit  + 1
            | "IB" | "OB" | "MB" -> startByte <- startByte + 1
            | "IW" | "OW" | "MW" -> startWord <- startWord + 1
            | "ID" | "OD" | "MD" -> startWord <- startWord + 2
            | _ ->  failwithlog "Unknown  %s memType:" memType

            if (alreadyAllocatedAddresses.Contains(address)) then
                Trace.WriteLine $"Adress {address} already in use. Tring to choose other address.."
                generate()
            else
                address

        generate



    /// devicePos, addressIEC, name, comment, device, kind, address, plcType 를 받아서 SymbolInfo 를 생성한다.
    let createSymbolWithDetail devicePos addressIEC name comment device kind  address plcType : SymbolInfo =
        let comment = SecurityElement.Escape comment
        {   Name=name; Comment=comment; Device=device; Kind = kind;
            Type=plcType; State=0; Address=address; DevicePos=devicePos; AddressIEC=addressIEC}

    /// name, comment, device, kind, address, plcType 를 받아서 SymbolInfo 를 생성한다.
    let createSymbol = createSymbolWithDetail -1 ""

    let copyLocal2GlobalSymbol (s:SymbolInfo) =
        { s with Kind = int Variable.Kind.VAR_GLOBAL; State=0; }

    /// Symbol variable 정의 구역 xml 의 string 을 생성
    let generateSymbolVars (symbols:SymbolInfo seq, bGlobal:bool) =
        [
            let varType = if bGlobal then "GlobalVariable" else "LocalVar"
            yield $"<{varType} Version=\"Ver 1.0\" Count={dq}{symbols.length()}{dq}>"
            yield "<Symbols>"
            yield! symbols |> Seq.map (fun s -> s.GenerateXml())
            yield "</Symbols>"
            yield "<TempVar Count=\"0\"></TempVar>"
            yield $"</{varType}>"
        ] |> String.concat "\r\n"




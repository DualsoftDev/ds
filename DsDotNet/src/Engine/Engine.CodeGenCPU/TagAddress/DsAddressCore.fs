namespace Engine.CodeGenCPU

open Dual.Common.Core.FS
open PLC.CodeGen.Common
open Dual.PLC.Common.FS
open XgtProtocol
open Engine.CodeGenCPU.DsAddressUtil
open Engine.Core
open System.Linq
open MelsecProtocol

[<AutoOpen>]
module DsAddressCore =

    let mutable inDigitCnt = 0
    let mutable inAnalogCnt = 0
    let mutable outDigitCnt = 0
    let mutable outAnalogCnt = 0
    let mutable memoryCnt = InitStartMemory

    let setMemoryIndex i = memoryCnt <- i
    let getCurrentMemoryIndex () = memoryCnt

    let InitializeIOMemoryIndex () =
        memoryCnt <- InitStartMemory
        inDigitCnt <- 0
        inAnalogCnt <- 0
        outDigitCnt <- 0
        outAnalogCnt <- 0

    let getValidAddress (address:string, dataType:DataType, name, isSkip, ioType, target:HwTarget) =
        let addr = address.Trim().ToUpper()
        if addr = "" then failwith $"주소가 없습니다: {name}"
        if addr <> TextNotUsed && isSkip then failwith $"{name} 생략시 반드시 '-' 기입 필요"

        let sizeBit =
            match target.HwIO with
            | LS_XGK_IO ->
                match dataType.ToBitSize() with
                | 1 | 8 | 16 -> 16
                | 32 | 64   -> dataType.ToBitSize()
                | _ -> failwith $"XGK 미지원 타입: {dataType}"
            | _ -> dataType.ToBitSize()

        let getNextAligned x = if x % sizeBit = 0 then x else x + sizeBit - (x % sizeBit)
        let increment x = getNextAligned x + sizeBit

        let assignCount () =
            match ioType with
            | In when sizeBit = 1 ->
                let ret = getNextAligned inDigitCnt
                inDigitCnt <- increment inDigitCnt; ret
            | Out when sizeBit = 1 ->
                let ret = getNextAligned outDigitCnt
                outDigitCnt <- increment outDigitCnt; ret
            | In ->
                let ret = inAnalogCnt
                inAnalogCnt <- inAnalogCnt + (if target.HwIO = LS_XGK_IO then sizeBit / 8 else 1); ret
            | Out ->
                let ret = outAnalogCnt
                outAnalogCnt <- outAnalogCnt + (if target.HwIO = LS_XGK_IO then sizeBit / 8 else 1); ret
            | Memory ->
                let ret = getNextAligned memoryCnt
                memoryCnt <- increment memoryCnt; ret
            | _ -> failwith $"지원하지 않는 IOType: {ioType}"

        let getPCIOM(head:string, offsetBit) =
            match sizeBit with
            |  1 -> $"{head}B{offsetBit /  8}.{offsetBit % 8}"
            |  8 -> $"{head}B{offsetBit /  8}"
            | 16 -> $"{head}W{offsetBit / 16}"
            | 32 -> $"{head}D{offsetBit / 32}"
            | 64 -> $"{head}L{offsetBit / 64}"
            | _ -> failwith $"Invalid size: {sizeBit}"

            

        let makeAutoAddress () =
            let cnt = assignCount()
            match target.HwIO with
            | OPC_IO->
                let prefix = match ioType with In -> "I" | Out -> "O" | Memory -> "M" | _ -> failwith $"지원 안됨: {ioType}"
                getPCIOM (prefix, (if sizeBit = 1 then cnt else cnt * 8))
            | MELSEC_IO ->
                let prefix = match ioType with In -> "X" | Out -> "Y" | Memory -> "M" | _ -> failwith $"지원 안됨: {ioType}"
                MxTagParser.ParseFromSegment(prefix, cnt, sizeBit)
            | LS_XGI_IO ->
                let prefix = match ioType with In -> "I" | Out -> "Q" | Memory -> "M" | _ -> failwith $"지원 안됨: {ioType}"
                if ioType = Memory
                    then LsXgiTagParser.ParseAddressMemory(prefix, cnt, sizeBit)    
                else
                    let iSlot, sumBit = getSlotInfoIEC(ioType, cnt, target.Slots)
                    LsXgiTagParser.ParseAddressIO(prefix, cnt, sizeBit, iSlot, sumBit)
            | LS_XGK_IO ->
                let isBool = dataType = DuBOOL

                if ioType = Memory 
                then 
                    LsXgkTagParser.ParseAddress("M", cnt, isBool)
                else
                    let slotOffset = 
                        if isBool then getSlotInfoNonIEC(ioType, cnt, target.Slots)
                        elif ioType = In then cnt + XGKAnalogOffsetByte
                        else cnt + XGKAnalogOffsetByte + XGKAnalogOutOffsetByte
                    LsXgkTagParser.ParseValidText($"P{slotOffset}", isBool)

        match addr with
        | a when a = TextAddrEmpty && not isSkip -> makeAutoAddress()
        | a when a = TextNotUsed -> TextNotUsed
        | a when a <> TextNotUsed && isSkip -> failwith $"생략된 인터페이스는 '-'로 기입하세요: {name}"
        | _ ->
            match target.HwIO with
            | LS_XGK_IO ->
                match LsXgkTagParser.ParseValidText(addr, (dataType = DuBOOL)) with
                | a when a.IsNonNull() -> a
                | _ -> failwith $"XGK 주소 오류: {name} {addr}"
            | LS_XGI_IO ->
                match tryParseXgiTag addr with
                | Some _ -> addr
                | None -> failwith $"XGI 주소 오류: {name} {addr}"
            | MELSEC_IO ->
                match MxTagParser.Parse addr with
                | Some v -> v
                | None -> failwith $"Melsec 주소 오류: {name} {addr}"
            | _ -> addr

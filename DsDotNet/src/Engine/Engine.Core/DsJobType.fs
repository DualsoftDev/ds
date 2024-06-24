// Copyright (c) Dualsoft  All Rights Reserved.
namespace Engine.Core

open System
open Dual.Common.Core.FS
open System.Runtime.CompilerServices

[<AutoOpen>]
module DsJobType =
        
    type JobTypeAction = 
        | ActionNormal  
        | Push    
        member x.ToText() = 
            match x with
            | ActionNormal   -> ""
            | Push    -> TextJobPush

    type JobTypeMulti = 
        | Single 
        | MultiAction of string * int * int option * int option
        member x.DeviceCount = 
            match x with
            | MultiAction (_, cnt, _ , _) -> cnt
            | _ -> 1

        member x.AddressInCount = 
            match x with
            | MultiAction (_, _, cnt , _) -> 
                if cnt.IsSome then cnt.Value else 1
            | _ -> 1

        member x.AddressOutCount = 
            match x with
            | MultiAction (_, _, _ , cnt) -> 
                if cnt.IsSome then cnt.Value else 1
            | _ -> 1

        member x.ToText() = 
            match x with
            | Single   -> ""
            | MultiAction (_, arrayCnt, inCnt, outCnt)-> 
                let inAddrCnt  = if inCnt.IsSome then inCnt.Value else arrayCnt
                let outAddrCnt = if outCnt.IsSome then outCnt.Value else arrayCnt
                $"{TextJobMulti}{arrayCnt}({inAddrCnt}, {outAddrCnt})"

    type JobParam(action: JobTypeAction, multi: JobTypeMulti) =
        member val JobAction = action with get
        member val JobMulti = multi with get

        member x.ToText() =
            let actionText = x.JobAction.ToText()
            let multiText = x.JobMulti.ToText()
            let parts = [actionText; multiText] |> List.filter (fun part -> not (String.IsNullOrEmpty(part)))
            String.Join("; ", parts)

        member x.DeviceCount =
            x.JobMulti.DeviceCount

    let getJobTypeAction (name: string) =
        let endContents = GetSquareBrackets(name, false)
        match endContents with
        | Some e when e = TextJobPush -> JobTypeAction.Push
        | _ -> JobTypeAction.ActionNormal

    let getJobTypeMulti (name: string) =
        let nameContents = GetBracketsRemoveName(name)
        let endContents  = GetSquareBrackets(name, false)
        if endContents.IsNone
        then 
            JobTypeMulti.Single
        else 
            let parseMultiActionString (str: string) =
                let mainPart, optionalPart =
                    if str.Contains("(") then
                        let parts = str.Split([| '('; ')' |], System.StringSplitOptions.RemoveEmptyEntries)
                        (parts.[0].TrimStart(TextJobMulti.ToCharArray()), if parts.Length > 1 then Some(parts.[1]) else None)
                    else
                        (str, None)

                let cnt = mainPart |> int
                let inCnt, outCnt =
                    match optionalPart with
                    | Some optPart ->
                        let values = optPart.Split(',')
                        let inCnt = if values.Length > 0 then Some(values.[0] |> int) else None
                        let outCnt = if values.Length > 1 then Some(values.[1] |> int) else None
                        (inCnt, outCnt)
                    | None -> (None, None)

                cnt, inCnt, outCnt

            let cnt, inCnt, outCnt = parseMultiActionString endContents.Value
            if cnt < 1 then
                failWithLog $"MultiAction Count >= 1 : {name}"

            JobTypeMulti.MultiAction (nameContents, cnt, inCnt, outCnt)


    let getParserJobType(name: string) =
        let mutable jobTypeMulti = JobTypeMulti.Single
        let mutable jobTypeAction = JobTypeAction.ActionNormal

        let items = name.Split(';')
        for item in items do
            let trimmedName = item.Trim()
            if trimmedName.Contains(',') then
                jobTypeMulti <- getJobTypeMulti trimmedName
            else
                jobTypeAction <- getJobTypeAction trimmedName


        JobParam(jobTypeAction, jobTypeMulti)

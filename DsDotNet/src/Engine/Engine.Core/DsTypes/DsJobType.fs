namespace Engine.Core

open Dual.Common.Core.FS
open System
open System.Linq
open System.Text.RegularExpressions

[<AutoOpen>]
module DsJobTypeModule =

    type JobDevParam =
        {
            TaskDevCount: int
            InCount: int 
            OutCount: int 
        }
        with
            member x.ToText() =
                if x.TaskDevCount = 1 && x.InCount = 1 && x.OutCount = 1 then
                    ""
                else
                    $"{TextJobMulti}{x.TaskDevCount}({x.InCount}, {x.OutCount})"

    let parseMultiActionString (str:string) =
        let str = str.TrimStart('[').TrimEnd(']')
        let mainPart, optionalPart = 
            if str.Contains("(") then 
                let parts = str.Split([| '('; ')' |], StringSplitOptions.RemoveEmptyEntries)
                (parts.[0].TrimStart(TextJobMulti.ToCharArray()), parts.TryItem(1))
            else 
                (str.TrimStart(TextJobMulti.ToCharArray()), None)

        let cnt = int mainPart
        let inCnt, outCnt = 
            match optionalPart with
            | Some opt -> 
                let values = opt.Split(',')
                values[0]|>int, values[1]|>int
            | None ->  cnt,  cnt

        cnt, inCnt, outCnt

    let getJobDevParam param =
        let cnt, inCnt, outCnt = parseMultiActionString param
        if cnt < 1 then failWithLog $"MultiAction Count >= 1 : {param}"
        { TaskDevCount = cnt; InCount = inCnt; OutCount = outCnt }

    let defaultJobDevParam() = { TaskDevCount = 1; InCount =  1; OutCount =  1 }

    let getParserJobType (param: string) =
        if param.IsNullOrEmpty() then defaultJobDevParam()
        else
            let cnt, inCnt, outCnt = parseMultiActionString param
            { TaskDevCount = cnt; InCount = inCnt; OutCount = outCnt }
            
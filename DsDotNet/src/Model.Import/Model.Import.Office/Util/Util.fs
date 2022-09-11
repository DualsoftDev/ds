// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System
open System.Linq
open System.Diagnostics
open System.Collections.Concurrent
open System.Runtime.CompilerServices

[<AutoOpen>]
module Util =

    type E =
        /// relay 변화를 Trace.WriteLine   
        [<Extension>] static member ConsolLogAction(text:string) = 
                        Trace.WriteLine ($"{DateTime.Now.Second}.{DateTime.Now.Millisecond} {text}") 

  
    /// ConcurrentDictionary 를 이용한 hash
    type ConcurrentHash<'T>() =
        inherit ConcurrentDictionary<'T, 'T>()
        member x.TryAdd(item:'T) = x.TryAdd(item, item)


          
    /// 시스템 전용 문자 리스트  // '_'는 선두만 불가, '~'은 앞뒤만 가능
    let SystemChar = [
                ">"; "<"; "|"; "="; "-"; ";"; ":"; "'"; "\""; "["; "]" ; "{"; "}" 
                "!"; "@"; "#"; "^"; "&"; "*";"/"; "+"; "-"; "?" 
            ]

    let IsInvalidName(name:string) = 
        let ngName = SystemChar |> Seq.filter(fun char -> name.Contains(char))
        ngName.Any() 
        || name.StartsWith("_") 
        || (name.Length > 0 && Char.IsDigit(name.[0]))
        


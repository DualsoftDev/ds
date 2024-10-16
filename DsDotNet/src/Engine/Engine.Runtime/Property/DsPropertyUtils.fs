namespace Engine.Runtime

open System
open System.IO
open System.Linq
open System.Runtime.CompilerServices
open Newtonsoft.Json
open Microsoft.FSharp.Core
open Dual.Common.Core.FS
open Engine.Core

[<AutoOpen>]
module NullableUtils =

    let toNullable (opt: 'T option) : Nullable<'T> =
        match opt with
        | Some value -> Nullable(value)
        | None -> Nullable()

    let toNull (opt: string option) : string =
        match opt with
        | Some value -> value
        | None -> null

    let fromNullable (nullable: Nullable<'T>) : 'T option =
        if nullable.HasValue then Some nullable.Value else None

    let fromNull (str: string) : string option =
        if str <> null then Some str else None

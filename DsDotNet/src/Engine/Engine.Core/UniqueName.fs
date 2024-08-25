// Copyright (c) Dualsoft  All Rights Reserved.
namespace rec Engine.Core

open System
open System.Collections.Generic
open System.Text.RegularExpressions
open Dual.Common.Core.FS
open System.Runtime.CompilerServices

[<RequireQualifiedAccess>]
module UniqueName =
    type NameGenerator = unit -> string

    let private genDict = Dictionary<string, NameGenerator>(StringComparer.OrdinalIgnoreCase)

    let generate prefix =
        if not (genDict.ContainsKey prefix) then
            genDict.[prefix] <- incrementalKeywordGenerator prefix 0
        let name = genDict.[prefix]()
        Regex.Replace(name, $"^{prefix}", prefix, RegexOptions.IgnoreCase)

    let resetAll() = genDict.Clear()

[<AutoOpen>]
module UniquePlcTagName =
    let getPlcTagAbleName (name:string) (storages:Storages) =

        let rec generateUntilValid(inputName:string) =
            if storages.ContainsKey inputName then
                generateUntilValid(UniqueName.generate inputName)
            else
                inputName

        name |> validStorageName |> generateUntilValid


    let getInActionName name = $"{name}_I"
    let getOutActionName name = $"{name}_O"
    let getMemoryActionName name = $"{name}_M"


type UniqueNameExt =
    [<Extension>]
    static member TryFindWithName (namedObjects: #INamed seq, name: string) =
        namedObjects |> Seq.tryFind (fun obj -> obj.Name = name)

    [<Extension>]
    static member TryFindWithNameComponents (namedObjects: #IQualifiedNamed seq, nameComponents: Fqdn) =
        namedObjects |> Seq.tryFind (fun obj -> obj.NameComponents = nameComponents)

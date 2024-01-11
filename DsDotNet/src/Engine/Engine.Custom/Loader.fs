namespace Engine.Custom

open System
open System.Diagnostics
open System.Reflection
open Dual.Common.Core.FS

[<AutoOpen>]
module Loader =
    let LoadFromDll (dllPath: string) =
        let typ =
            let assembly = Assembly.LoadFrom(dllPath)
            assembly.FindImplementingTypes(typeof<IEngineExtension>) |> Array.exactlyOne

        let dsApi =
            let reader =
                Func<string, obj>(fun (tag: string) ->
                    Debug.WriteLine($"Reading {tag}")
                    true)

            let writer =
                Action<string, obj>(fun (tag: string) (value: obj) -> Debug.WriteLine($"Writing {tag}={value}"))

            DsApi(reader, writer)

        let ext =
            Activator.CreateInstanceFrom(dllPath, typ.FullName).Unwrap() :?> IEngineExtension

        ext.Initialize(dsApi)

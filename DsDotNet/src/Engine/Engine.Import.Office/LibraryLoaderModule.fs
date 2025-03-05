// Copyright (c) Dualsoft  All Rights Reserved.
namespace Engine.Import.Office

open System.Linq
open System.Collections.Generic
open Dual.Common.Core.FS
open System.IO
open Newtonsoft.Json
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System.Collections.Generic
open System.Reflection


module LibraryLoaderModule =
    
   
    type LibraryConfig(version: string, libraryInfos: Dictionary<string, string>) =
        member val Version = version with get, set
        member val LibraryInfos = libraryInfos with get, set
    
        override this.ToString() =
            // 경로별로 API 이름을 그룹화
            let grouped =
                this.LibraryInfos
                |> Seq.groupBy (fun kvp -> kvp.Value)  // 경로 기준 그룹화
                |> Seq.map (fun (path, apis) -> 
                    let apiNames = apis |> Seq.map (fun kvp -> kvp.Key) |> String.concat ", "
                    sprintf "%s\r\n[%s]" path apiNames
                )
                |> String.concat "\r\n\r\n"

            sprintf "Version: %s\r\n\r\n%s" this.Version grouped

    let private jsonSettings = JsonSerializerSettings()

    let LoadLibraryConfig (path: string) =
        let json = File.ReadAllText(path)
        JsonConvert.DeserializeObject<LibraryConfig>(json, jsonSettings)

    let SaveLibraryConfig (path: string) (libraryConfig:LibraryConfig) =
        let json = JsonConvert.SerializeObject(libraryConfig, Formatting.Indented, jsonSettings)
        File.WriteAllText(path, json)

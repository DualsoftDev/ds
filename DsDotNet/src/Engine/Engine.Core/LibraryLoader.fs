namespace rec Engine.Core

open System.IO
open Newtonsoft.Json
open System.Collections.Generic


module LibraryLoaderModule =
    
   
    type LibraryConfig = {
        ///parent 시스템에서 사용한 Lib버전과 현재 설치된 Lib 버전은 항상 같아야 한다
        Version: string         
        ///Api 이름 중복을 막기위해 Dictionary 처리
        LibraryInfos: Dictionary<string, string> //Api, filePath 
    }

    let private jsonSettings = JsonSerializerSettings()

    let LoadLibraryConfig (path: string) =
        let json = File.ReadAllText(path)
        JsonConvert.DeserializeObject<LibraryConfig>(json, jsonSettings)

    let SaveLibraryConfig (path: string) (libraryConfig:LibraryConfig) =
        let json = JsonConvert.SerializeObject(libraryConfig, Formatting.Indented, jsonSettings)
        File.WriteAllText(path, json)

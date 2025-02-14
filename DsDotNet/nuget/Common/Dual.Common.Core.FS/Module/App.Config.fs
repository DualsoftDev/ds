namespace Dual.Common.Core.FS


open System
open System.Configuration
open System.IO
open System.Reflection

/// App.Config : configuration 파일 읽기
module AppConfig =
    /// App.Config 파일에서 key 값을 읽어서 주어진 function f (-> <Type>.TryParse) 를 수행한 결과를 반환한다.
    let private parseAppKey f (key:string) =
        let strValue = ConfigurationManager.AppSettings.[key];
        match f(strValue) with
        | true, value -> Some(value)
        | _ -> None


    let readIntKey key = parseAppKey Int32.TryParse key
    let readUIntKey key = parseAppKey UInt32.TryParse key
    let readShortKey key = parseAppKey Int16.TryParse key
    let readUShortKey key = parseAppKey UInt16.TryParse key
    let readLongKey key = parseAppKey Int64.TryParse key
    let readULongKey key = parseAppKey UInt64.TryParse key
    let readDoubleKey key = parseAppKey Double.TryParse key
    let readBoolKey key = parseAppKey Boolean.TryParse key

    let readStringKey (key:string) =
        let strValue = ConfigurationManager.AppSettings.[key];
        if strValue = null || strValue = "" then
            None
        else
            Some(strValue)

    /// (AppName).exe.Config 파일이 존재하는지 검사
    let isAppConfigFileExist() =
        let entry = System.Reflection.Assembly.GetEntryAssembly()
        let dir = Path.GetDirectoryName(entry.Location)
        let config = Path.Combine(dir, entry.GetName().Name) + ".exe.config"
        File.Exists(config)


module EmbeddedResource =
    /// assembly 에 embedding 된 resource file 을 읽어서 문자열 option 으로 반환
    // e.g assembly = Assembly.GetExecutingAssembly()
    let readFile (assembly:Assembly) (resourcePath:string) =
        let resource = resourcePath.ToLower()
        assembly.GetManifestResourceNames()
        |> Array.tryFind(fun n -> n.ToLower().Contains(resource))
        |> Option.map(fun n->
            let stream = assembly.GetManifestResourceStream(n)
            (new StreamReader(stream)).ReadToEnd())

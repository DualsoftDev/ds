namespace DB.DuckDB

open System
open System.IO
open System.Text.Json
open System.Text.Json.Serialization

[<AutoOpen>]
module DuckDBSetting =

    type DuckDBSetting = {
        DatabaseDir: string
        LogFlushIntervalMs: int
    }

    let private defaultSetting = {
        DatabaseDir = 
            let baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
            Path.Combine(baseDir, "Dualsoft", "DB")

        LogFlushIntervalMs = 5000
    }

    let private getSettingPath () =
        let baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
        let settingDir = Path.Combine(baseDir, "Dualsoft", "DB")
        let settingFile = Path.Combine(settingDir, "DuckDBSetting.json")
        settingDir, settingFile

    let private ensureFileExists (filePath: string) (setting: DuckDBSetting) =
        let options = JsonSerializerOptions(WriteIndented = true)
        if not (File.Exists filePath) then
            let json = JsonSerializer.Serialize(setting, options)
            Directory.CreateDirectory(Path.GetDirectoryName(filePath)) |> ignore
            File.WriteAllText(filePath, json)

    let loadSettings () : DuckDBSetting =
        let _settingDir, settingFile = getSettingPath()
        ensureFileExists settingFile defaultSetting

        let json = File.ReadAllText(settingFile)
        let options = JsonSerializerOptions()
        options.PropertyNameCaseInsensitive <- true
        options.AllowTrailingCommas <- true

        JsonSerializer.Deserialize<DuckDBSetting>(json, options)

namespace Engine.Import.Office

open System.Drawing
open System.Linq
open System.Collections.Generic
open System
open Newtonsoft.Json
open System.IO


[<AutoOpen>]
module GlobalText =

    let LibFileName = "DS_Library";
    let HelloDSFileName = "HelloDS";
    let DSManualFileName = "HelloDS_BasicManual";
    let IoTableTitleName = "IO TABLE";
    

    let PowerPointAddInHelperProcessName : string = "PowerPointAddInHelper"
    let HelperPath : string =
        let basePath = AppDomain.CurrentDomain.BaseDirectory
        let relativePath = $"..\\net8.0-windows\\{PowerPointAddInHelperProcessName}.exe"
        let absolutePath = Path.GetFullPath(Path.Combine(basePath, relativePath))
        let appPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
        if File.Exists(absolutePath) then
            absolutePath
        else
            $@"{appPath}\Dualsoft\PowerPointAddIn For Dualsoft\{PowerPointAddInHelperProcessName}.exe"


    let DSPilotProcessName : string = "DSPilot.Winform"
    let DSRuntimeProcessName : string = "DSRuntime.Winform"
    let getDSAppPath(appName:string) : string =
        let basePath = AppDomain.CurrentDomain.BaseDirectory
        let relativePath = $"..\\net8.0-windows\\{appName}.exe"
        let absolutePath = Path.GetFullPath(Path.Combine(basePath, relativePath))
        let appPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
        if File.Exists(absolutePath) then
            absolutePath
        else
            $@"{appPath}\Dualsoft\PowerPointAddIn For Dualsoft\{appName}.exe"

    let DSPilotPath : string =  getDSAppPath DSPilotProcessName    
    let DSRuntimePath : string =  getDSAppPath DSRuntimeProcessName    

    let DSUserTagsDirectory: string = 
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "dualsoft", "UserTags" );

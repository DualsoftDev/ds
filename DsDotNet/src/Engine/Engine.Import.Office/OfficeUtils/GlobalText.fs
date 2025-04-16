namespace Engine.Import.Office

open System.Drawing
open System.Linq
open System.Collections.Generic
open System
open Newtonsoft.Json
open System.IO
open System.Text.RegularExpressions

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
    let DSPWizardProcessName : string = "DSPilot.Winform.Wizard"
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

    let DSPWizardPath : string =  getDSAppPath DSPWizardProcessName    
    let DSPilotPath : string =  getDSAppPath DSPilotProcessName    
    let DSRuntimePath : string =  getDSAppPath DSRuntimeProcessName    


    let getModelConfigPath (fulPath: string) =
        if  String.IsNullOrEmpty fulPath 
        then 
            failwith "fullPath is null or empty"
        else 
            /// 전체 경로에서 확장자를 제거하고, 경로 구분자 등을 안전한 파일 이름 문자로 치환
            let makeValidFileNameFromFullPath (fileFullPath: string) =
                let noExtension = Path.Combine(Path.GetDirectoryName(fileFullPath), Path.GetFileNameWithoutExtension(fileFullPath))
                let replaced = Regex.Replace(noExtension, "[\\/:*?\"<>|]", "_") // 윈도우 파일 이름에 안 되는 문자
                replaced.Replace('\\', '_').Replace('/', '_')

            let DSUserTagsDirectory: string = 
                Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "dualsoft", "ModelConfig" );
            let fileName = 
                Path.GetFileNameWithoutExtension(makeValidFileNameFromFullPath(fulPath)) + ".json"
            Path.Combine(DSUserTagsDirectory, fileName)


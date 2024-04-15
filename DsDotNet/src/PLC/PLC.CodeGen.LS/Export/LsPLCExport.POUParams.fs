namespace PLC.CodeGen.LS

open Engine.Core
open PLC.CodeGen.Common
open Dual.Common.Core.FS

[<AutoOpen>]
module POUParametersModule =

    type XgxPOUParams =
        {
            /// POU name.  "DsLogic"
            POUName: string
            /// POU container task name
            TaskName: string
            /// POU ladder 최상단의 comment
            Comment: string
            LocalStorages: Storages
            /// 참조용 global storages
            GlobalStorages: Storages
            CommentedStatements: CommentedStatement list
        }

        //MemoryAllocatorSpec = RangeSpec (0, 640*1024)   // 640K M memory 영역
    type XgxProjectParams = {
        TargetType: PlatformTarget
        ProjectName: string
        ProjectComment: string
        GlobalStorages: Storages
        ExistingLSISprj: string option
        POUs: XgxPOUParams list
        MemoryAllocatorSpec: PLCMemoryAllocatorSpec
        EnableXmlComment: bool
        AppendDebugInfoToRungComment: bool
        RungCounter: (unit -> int) option
    }

    let createDefaultProjectParams targetType memorySize =
        {
            TargetType = targetType
            ProjectName = ""
            ProjectComment = ""
            GlobalStorages = Storages()
            ExistingLSISprj = None
            POUs = []
            MemoryAllocatorSpec = AllocatorFunctions(createMemoryAllocator "M" (0, memorySize) [] targetType)
            EnableXmlComment = false
            AppendDebugInfoToRungComment = IsDebugVersion || isInUnitTest()
            RungCounter = None
        }

    let defaultXGIProjectParams = createDefaultProjectParams XGI (640 * 1024)   // 640K "M" memory 영역
    let defaultXGKProjectParams = createDefaultProjectParams XGK (640 * 1024) 

    let getXgxProjectParams (targetType:PlatformTarget) (projectName:string) =
        if targetType = XGI 
        then { defaultXGIProjectParams with ProjectName = projectName; TargetType = targetType }
        elif targetType = XGK 
        then  { defaultXGKProjectParams with ProjectName = projectName; TargetType = targetType }
        else
            failwithf "Invalid target type: %A" targetType

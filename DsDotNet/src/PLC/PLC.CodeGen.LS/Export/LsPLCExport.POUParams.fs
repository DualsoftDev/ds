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

    type XgxProjectParams = {
        TargetType: RuntimeTargetType
        ProjectName: string
        ProjectComment: string
        GlobalStorages: Storages
        ExistingLSISprj: string option
        POUs: XgxPOUParams list
        MemoryAllocatorSpec: PLCMemoryAllocatorSpec

        EnableXmlComment: bool
        AppendDebugInfoToRungComment: bool
        /// Rung counter 생성기
        RungCounter : (unit -> int) option
    }

    let defaultXgxProjectParams = {
        TargetType = XGI
        ProjectName = ""
        ProjectComment = ""
        GlobalStorages = Storages()
        ExistingLSISprj = None
        POUs = []
        MemoryAllocatorSpec = AllocatorFunctions(createMemoryAllocator "R" (0, 640 * 1024) [] XGI) // 640K R memory 영역
        //MemoryAllocatorSpec = RangeSpec (0, 640*1024)   // 640K M memory 영역
        EnableXmlComment = false
        AppendDebugInfoToRungComment = IsDebugVersion || isInUnitTest()
        RungCounter = None
    }

    let getXgxProjectParams (targetType:RuntimeTargetType) (projectName:string) =
        { defaultXgxProjectParams with ProjectName = projectName; TargetType = targetType }

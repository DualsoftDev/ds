namespace PLC.CodeGen.LSXGI

open Engine.Core
open PLC.CodeGen.Common

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

    type XgxProjectParams =
        { ProjectName: string
          ProjectComment: string
          GlobalStorages: Storages
          ExistingLSISprj: string option
          POUs: XgxPOUParams list
          MemoryAllocatorSpec: PLCMemoryAllocatorSpec

          EnableXmlComment: bool
          AppendExpressionTextToRungComment: bool }

    let defaultXgxProjectParams =
        { ProjectName = ""
          ProjectComment = ""
          GlobalStorages = Storages()
          ExistingLSISprj = None
          POUs = []
          MemoryAllocatorSpec = AllocatorFunctions(createMemoryAllocator "M" (0, 640 * 1024) []) // 640K M memory 영역
          //MemoryAllocatorSpec = RangeSpec (0, 640*1024)   // 640K M memory 영역
          EnableXmlComment = false
          AppendExpressionTextToRungComment = true }

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
        TimerCounterGenerator:Seq.counterGeneratorType
        CounterCounterGenerator:Seq.counterGeneratorType
        RungCounter :Seq.counterGeneratorType
        /// Auto 변수의 이름을 uniq 하게 짓기 위한 용도 "_tmp_temp_internal{n}
        AutoVariableCounter: Seq.counterGeneratorType
    }

    let createDefaultProjectParams targetType memorySize =
        let voidCounterGenerator : Seq.counterGeneratorType =
            fun () -> failwith "Should be assigned with valid counter generator"
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
            TimerCounterGenerator = voidCounterGenerator
            CounterCounterGenerator = voidCounterGenerator
            RungCounter = voidCounterGenerator
            AutoVariableCounter = voidCounterGenerator
        }

    let defaultMemorySize = 640 * 1024
    let defaultXGIProjectParams = createDefaultProjectParams XGI defaultMemorySize   // 640K "M" memory 영역
    let defaultXGKProjectParams = createDefaultProjectParams XGK defaultMemorySize 

    let getXgxProjectParams (targetType:PlatformTarget) (projectName:string) =
        assert(isInUnitTest())
        let getProjectParams =
            match targetType with
            | XGI -> defaultXGIProjectParams
            | XGK -> defaultXGKProjectParams
            | _ -> failwithf "Invalid target type: %A" targetType
        { getProjectParams with 
            ProjectName = projectName; TargetType = targetType;
            MemoryAllocatorSpec = AllocatorFunctions(createMemoryAllocator "M" (0, defaultMemorySize) [] targetType)
            TimerCounterGenerator = counterGenerator 0
            CounterCounterGenerator = counterGenerator 0
            AutoVariableCounter = counterGenerator 0
            RungCounter = counterGenerator 0
        }


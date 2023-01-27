namespace PLC.CodeGen.LSXGI
open Engine.Core

[<AutoOpen>]
module POUParametersModule =

    type XgiPOUParams = {
        /// POU name.  "DsLogic"
        POUName : string
        /// POU container task name
        TaskName: string
        /// POU ladder 최상단의 comment
        Comment : string
        LocalStorages : Storages
        /// 참조용 global storages
        GlobalStorages: Storages
        CommentedStatements : CommentedStatement list
    }
    type XgiProjectParams = {
        ProjectName    : string
        ProjectComment : string
        GlobalStorages : Storages
        ExistingLSISprj: string option
        POUs           : XgiPOUParams list
    }

namespace rec Engine.CodeGenCPU

open System.Linq
open Engine.Core
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System

[<AutoOpen>]
module ConvertCoreExtUtils =

    let hasNot  (x:OperatorFunction option) = x.IsSome && x.Value.OperatorType = DuOPNot

    let getVM(v:Vertex)     = v.TagManager :?> VertexTagManager
    let getVMReal(v:Vertex) = v.TagManager :?> RealVertexTagManager
    let getVMCall(v:Vertex) = v.TagManager :?> CallVertexTagManager

    let getTarget (x:DsSystem) = (x.TagManager :?> SystemManager).TargetType
    let getSM (x:DsSystem) = x.TagManager :?> SystemManager
    let getFM (x:Flow)     = x.TagManager :?> FlowManager
    let getAM (x:ApiItem)  = x.TagManager :?> ApiItemManager
    let getDM (x:TaskDev)  = x.TagManager :?> TaskDevManager


    let errText (x:Call)  = getVMCall(x).ErrorText

    let createHwApiBridgeTag (x:HwSystemDef, sys:DsSystem)  =
        let hwApi =   sys.HwSystemDefs.First(fun f->f.Name = x.Name)
        let bridgeType =
            match x with
            | :? ButtonDef -> BridgeType.Button
            | :? LampDef -> BridgeType.Lamp
            | :? ConditionDef -> BridgeType.Condition
            | _ ->
                failwithf "bridgeType err"

        createBridgeTag(sys.TagManager.Storages, x.Name, x.InAddress, (int)HwSysTag.HwSysIn, bridgeType , sys, hwApi, x.InDataType)
        |> iter (fun t -> x.InTag   <- t)
        createBridgeTag(sys.TagManager.Storages, x.Name, x.OutAddress,(int)HwSysTag.HwSysOut ,bridgeType ,sys, hwApi, x.OutDataType)
        |> iter (fun t -> x.OutTag  <- t)

    let getTaskDevParamExpr (x:TaskDevParam option, devTag:ITag, sys:DsSystem) =
        let sysOff = (sys.TagManager :?> SystemManager).GetSystemTag(SystemTag._OFF) :?> PlanVar<bool>
        if devTag.IsNull() then
            sysOff.Expr  :> IExpression
        else
            match x with
            | None -> devTag.ToExpression()
            | Some x ->
                if x.DataType = DuBOOL then
                    if Convert.ToBoolean(x.ReadBoolValue) then
                        devTag.ToExpression()
                    else
                        !@(devTag.ToExpression():?> Expression<bool>) :> IExpression
                else // bool 타입아닌 경우 비교문 생성
                    devTag.ToExpression() <@< x.ReadRangeValue

    [<AutoOpen>]
    [<Extension>]
    type TagInfoType =
        [<Extension>] static member GetTagSys  (x:DsSystem ,typ:SystemTag)  = getSM(x).GetSystemTag(typ)
        [<Extension>] static member GetTagFlow (x:Flow     ,typ:FlowTag)    = getFM(x).GetFlowTag(typ )

        [<Extension>] static member GetInExpr (x:HwSystemDef) =
                            getTaskDevParamExpr (x.TaskDevParamIO.InParam, x.InTag, x.System) :?> Expression<bool>
        [<Extension>] static member GetInExpr (x:TaskDev, job:Job) =
                            getTaskDevParamExpr (x.GetInParam(job)|>Some, x.InTag, x.GetApiItem(job).ApiSystem)  :?> Expression<bool>

        [<Extension>] static member GetOutExpr (x:TaskDev, job:Job) =
                            getTaskDevParamExpr (x.GetOutParam(job)|>Some, x.OutTag, x.GetApiItem(job).ApiSystem)  :?> Expression<bool>
  
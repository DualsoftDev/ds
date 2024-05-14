namespace rec Engine.CodeGenCPU

open System.Linq
open Engine.Core
open Dual.Common.Core.FS
open System.Runtime.CompilerServices
open System

[<AutoOpen>]
module ConvertCoreExtUtils =
    
    let hasNot  (x:OperatorFunction option) = x.IsSome && x.Value.OperatorType = DuOPNot

    let getVM(v:Vertex)     = v.TagManager :?> VertexManager
    let getVMReal(v:Vertex) = v.TagManager :?> VertexMReal
    let getVMCoin(v:Vertex) = v.TagManager :?> VertexMCall

    let getTarget (x:DsSystem) = (x.TagManager :?> SystemManager).TargetType
    let getSM (x:DsSystem) = x.TagManager :?> SystemManager
    let getFM (x:Flow)     = x.TagManager :?> FlowManager
    let getAM (x:ApiItem)  = x.TagManager :?> ApiItemManager

   
    let errText (x:Call)  = getVMCoin(x).ErrorText

    let createHwApiBridgeTag (x:HwSystemDef, sys:DsSystem)  = 
        let hwApi =   sys.HwSystemDefs.First(fun f->f.Name = x.Name)
        let bridgeType = 
            match x with
            | :? ButtonDef -> BridgeType.Button
            | :? LampDef -> BridgeType.Lamp
            | :? ConditionDef -> BridgeType.Condition
            | _ -> 
                failwithf "bridgeType err"

        createBridgeTag(sys.TagManager.Storages, x.Name, x.InAddress, (int)HwSysTag.HwSysIn, bridgeType , sys, hwApi, x.InParam|>getDataTypeParam)
        |> iter (fun t -> x.InTag   <- t)
        createBridgeTag(sys.TagManager.Storages, x.Name, x.OutAddress,(int)HwSysTag.HwSysOut ,bridgeType ,sys, hwApi, x.OutParam|>getDataTypeParam)
        |> iter (fun t -> x.OutTag  <- t)


    let getInExpr (x:DevParam, devTag:ITag, sys:DsSystem) = 
        let sysOff = (sys.TagManager :?> SystemManager).GetSystemTag(SystemTag.off) :?> PlanVar<bool> 
        if devTag.IsNonNull()
        then 
            match x.DevValue with
            |Some(v) -> createCustomFunctionExpression TextEQ [literal2expr v;devTag.ToExpression()]   
            |None -> sysOff.Expr
        else 
            sysOff.Expr

    [<AutoOpen>]
    [<Extension>]
    type TagInfoType =
        [<Extension>] static member GetTagSys  (x:DsSystem ,typ:SystemTag)  = getSM(x).GetSystemTag(typ)
        [<Extension>] static member GetTagFlow (x:Flow     ,typ:FlowTag)    = getFM(x).GetFlowTag(typ )

        [<Extension>] static member GetInExpr (x:HwSystemDef) = 
                            getInExpr (x.InParam, x.InTag, x.System) :?> Expression<bool>
        [<Extension>] static member GetInExpr (x:TaskDev) = 
                            getInExpr (x.InParam, x.InTag, x.ApiItem.ApiSystem)  :?> Expression<bool>
                        
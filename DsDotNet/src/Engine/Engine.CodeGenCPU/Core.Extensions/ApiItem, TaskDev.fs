namespace rec Engine.CodeGenCPU

open Engine.Core
open System.Linq
open ConvertCoreExtUtils

[<AutoOpen>]
module ConvertCpuApiItem =
    let getTM(td:TaskDev) = td.TagManager:?> TaskDevManager

    type TaskDev with
        member td.ExistInput   = addressExist td.InAddress
        member td.ExistOutput  = addressExist td.OutAddress

    type ApiItem with
        member a.ApiItemSet     = getAM(a).ApiItemSet
        member a.ApiItemEnd     = getAM(a).ApiItemEnd

        ///sensorLinking
        member a.SensorLinking  = getAM(a).SensorLinking
        ///sensorLinked
        member a.SensorLinked   = getAM(a).SensorLinked

        member a.RxET    = getVMReal(a.RX).ET
        member a.TxET    = getVMReal(a.TX).ET
        //member a.UpperLimit =
        //    match a.TimeParam with
        //    |Some t -> t.USL
        //    |None -> getUpperLimitFromReals(a)







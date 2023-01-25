namespace Engine.CodeGenCPU

open Engine.Core
open Engine.Common.FS
open System.Text.RegularExpressions

module ModuleInitializer =
    let Initialize() =
        printfn "Module is being initialized..."

        fwdCreateBoolTag <-
            let createBoolTag name value =
                PlcTag<bool>(name, value) :> TagBase<bool>
            createBoolTag


        fwdCreateUShortTag <-
            let createUShortTag name value =
                PlcTag<uint16>(name, value) :> TagBase<uint16>
            createUShortTag



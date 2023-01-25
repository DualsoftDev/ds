namespace Engine.Core

module ModuleInitializer =
    let Initialize() =
        printfn "Module is being initialized..."
        fwdSerializeFunctionNameAndBoxedArguments <- serializeFunctionNameAndBoxedArguments

        (* Engine.CodeGenCPU dll loading 시, PlcTag<> 를 생성하는 함수로 overriding 됨 *)
        fwdCreateBoolTag <-
            let createBoolTag name value =
                let param = {Name=name; Value=value; Comment=None; Address=None; System = Runtime.System}
                PlanTag<bool>(param) :> TagBase<bool>
            createBoolTag


        (* Engine.CodeGenCPU dll loading 시, PlcTag<> 를 생성하는 함수로 overriding 됨 *)
        fwdCreateUShortTag <-
            let createUShortTag name value =
                let param = {Name=name; Value=value; Comment=None; Address=None; System = Runtime.System}
                PlanTag<uint16>(param) :> TagBase<uint16>
            createUShortTag

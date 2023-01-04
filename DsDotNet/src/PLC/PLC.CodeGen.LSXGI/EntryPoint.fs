namespace PLC.CodeGen.LSXGI


module ModuleInitializer =
    let Initialize() =
        printfn "Module is being initialized..."

        fwdCreateSymbol <- XGITag.createSymbol


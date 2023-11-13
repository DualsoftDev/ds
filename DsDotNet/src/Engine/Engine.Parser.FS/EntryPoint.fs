namespace Engine.Parser.FS

module ModuleInitializer =
    let Initialize () =
        printfn "Module is being initialized..."
        ModelParser.Initialize()
        ()

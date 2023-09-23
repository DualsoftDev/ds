namespace T

[<AutoOpen>]
module Fixtures =
    [<AbstractClass>]
    type EngineTestBaseClass() =
        inherit TestBaseClass("EngineLogger")
        do
        ()
            //Engine.CodeGenCPU.ModuleInitializer.Initialize()

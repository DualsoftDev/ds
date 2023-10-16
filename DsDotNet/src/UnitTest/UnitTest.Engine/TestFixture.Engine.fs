namespace T
open Dual.UnitTest.Common.FS

[<AutoOpen>]
module Fixtures =
    [<AbstractClass>]
    type EngineTestBaseClass() =
        inherit TestBaseClass("EngineLogger")
        do
            ()
            //Engine.CodeGenCPU.ModuleInitializer.Initialize()

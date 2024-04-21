namespace T
open System.IO
open Dual.UnitTest.Common.FS

[<AutoOpen>]
module Fixtures =
    [<AbstractClass>]
    type EngineTestBaseClass() =
        inherit TestClassWithLogger(Path.Combine($"{__SOURCE_DIRECTORY__}/App.config"), "EngineLogger")
        do
            ()
            //Engine.CodeGenCPU.ModuleInitializer.Initialize()

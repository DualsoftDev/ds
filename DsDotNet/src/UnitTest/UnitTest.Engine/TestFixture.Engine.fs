namespace T
open System.IO
open Dual.Common.UnitTest.FS

[<AutoOpen>]
module Fixtures =
    [<AbstractClass>]
    type EngineTestBaseClass() =
        inherit TestClassWithLogger(Path.Combine($"{__SOURCE_DIRECTORY__}/App.config"), "UnitTestLogger")
        do
            ()
            //Engine.CodeGenCPU.ModuleInitializer.Initialize()

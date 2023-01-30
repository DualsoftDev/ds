namespace Old.Dual.Core.Finale

open Old.Dual.Common
open Old.Dual.Core.Prelude
open Old.Dual.Core.Prelude.IEC61131
open Old.Dual.Core.Types
//open Old.Dual.Core.DomainModels

module FodyWeaverDllInitializerModule =
    [<AbstractClass; Sealed>]
    type ModuleInitializer() =
        static member Initialize() =
            ()
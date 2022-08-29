namespace Dual.Core.Finale

open Dual.Common
open Dual.Core.Prelude
open Dual.Core.Prelude.IEC61131
open Dual.Core.Types
//open Dual.Core.DomainModels

module FodyWeaverDllInitializerModule =
    [<AbstractClass; Sealed>]
    type ModuleInitializer() =
        static member Initialize() =
            ()
namespace Engine.CodeGenCPU

open System.Diagnostics
open Engine.Core
open System.Linq
open System
open Dual.Common.Core.FS

[<AutoOpen>]
module ActionVariableManagerModule =

     type ActionVariableManager(v:ActionVariable, sys:DsSystem)  =
        let stg = sys.TagManager.Storages
        let actionVariTag =  createVariableByType v.Name v.Type
   
        do 
            stg.Add(actionVariTag.Name, actionVariTag)
        
        interface ITagManager with
            member _.Target = v
            member _.Storages = stg

        member _.ActionVariableTag  = actionVariTag        
        member _.ActionSourceTag    = stg.First(fun f->f.Value.Address = v.Address).Value            

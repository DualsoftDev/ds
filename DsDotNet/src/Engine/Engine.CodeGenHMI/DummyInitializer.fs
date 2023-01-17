namespace Engine.CodeGenHMI

open Engine.Core

module ModuleInitializer =
    type VMM (v:IQualifiedNamed) =
        interface ITagManager with
            member x.Target = v
            member x.Storages = Storages()
        
    let Initialize() = ()
        //let createTagManager (vertex:IQualifiedNamed) : ITagManager =
        //    new VMM(vertex)

        //fwdCreateTagManager <- createTagManager

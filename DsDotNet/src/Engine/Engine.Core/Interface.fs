// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Collections.Generic

[<AutoOpen>]
module Interface =

    
    type IVertex = interface end

    type INamed  =
         abstract Name:string with get, set

    type IText  =
         abstract ToText:unit -> string

    type IRenameable =
        inherit INamed
        abstract Name:string with set

    type IQualifiedNamed =
        inherit INamed
        abstract QualifiedName:string with get  
        abstract NameComponents:string[] with get

    
    /// Expression 의 Terminal 이 될 수 있는 subclass: Tag<'T>, StorageVariable<'T>
    type IStorage =
        inherit INamed
        inherit IText
        abstract Value: obj with get, set
        abstract DataType : System.Type
        abstract NotifyValueChanged : unit -> unit
        
    //type IStorage<'T> =
    //    inherit IStorage
    //    abstract Value: 'T with get, set

    type ITag = inherit IStorage
    type IVariable = inherit IStorage
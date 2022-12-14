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

    type IMemory = interface end
    type IBit    = interface end //<- 병합후 삭제


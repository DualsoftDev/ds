// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Collections.Generic

[<AutoOpen>]
module Interface =


    /// Dualsoft 모델링 기준 Node
    type IVertex  = interface end
    /// 이름
    type INamed  =
         abstract Name:string with get, set

    type IRenameable =
        inherit INamed
        abstract Name:string with set

    type IQualifiedNamed =
        inherit INamed
        abstract QualifiedName:string with get  //,set
        //default x.QualifiedName = x.NameComponents.Combine()
        abstract NameComponents:string[] with get

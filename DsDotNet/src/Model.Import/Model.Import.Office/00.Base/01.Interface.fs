// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

[<AutoOpen>]
module Interface =

    /// Dualsoft object
    type IObject = interface end

    /// System interface
    type ISystem =
        inherit IObject
        abstract Name:string

    /// Seg interface
    /// Graph 상의 vertex 가 될 수 있는 segment 로 소속 고유 System 을 가짐
    type IVertex =
        inherit IObject
        abstract Name:string


    /// Edge interface
    /// Graph 상의 edge 가 될 수 있는 interface.  Source, Target 두 개의 IVertex 를 연결
    type IEdge =
        inherit IObject
        abstract SourceVertex:IVertex
        abstract TargetVertex:IVertex

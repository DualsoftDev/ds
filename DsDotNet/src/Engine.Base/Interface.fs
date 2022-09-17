// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

[<AutoOpen>]
module Interface =

    /// Dualsoft object
    type IBit = interface end

    type INamed =
        abstract Name:string

    /// System interface
    type ISystem =
        inherit IBit
        inherit INamed

    /// Seg interface
    /// Graph 상의 vertex 가 될 수 있는 segment 로 소속 고유 System 을 가짐
    type IVertex =
        inherit IBit
        inherit INamed

    /// Edge interface
    /// Graph 상의 edge 가 될 수 있는 interface.  Source, Target 두 개의 IVertex 를 연결
    type IEdge =
        inherit IBit
        abstract SourceVertex:IVertex
        abstract TargetVertex:IVertex

    /// <summary> Segment or Call Base </summary>
    type ICoin =
        inherit IVertex

    //IEnumerable<ICoin> Coins { get; }
    type IWallet =        inherit IVertex

    type IAlias =        inherit INamed

    type ITxRx =        inherit INamed
    type IAutoTag =        inherit IBit
    type IStrongEdge =        inherit IEdge
    type IWeakEdge =        inherit IEdge
    type ISetEdge =        inherit IEdge
    type IResetEdge =        inherit IEdge

    type ICpu = inherit INamed
    type IEngine = inherit INamed
    type IBitReadable = inherit IBit
    type IBitWritable = 
        inherit IBit
        abstract SetValue:bool;
    type IBitReadWritable = 
        inherit IBitReadable
        inherit IBitWritable
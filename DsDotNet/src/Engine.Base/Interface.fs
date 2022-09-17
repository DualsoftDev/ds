// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

[<AutoOpen>]
module Interface =

    /// Dualsoft object 공리 : 시스템 모든것은 비트다
    type IBit = interface end
    /// 이름
    type INamed = abstract Name:string

    /// 정의(1/4) 남쪽: DS 기본유닛은 Bit인 단일 자원이다.
    type IVertex =
        inherit IBit
        inherit INamed

    /// 정의(2/4) 서쪽: DS 기본유닛은 관계를 갖는다.
    type IEdge =
        inherit IBit
        abstract SourceVertex:IVertex
        abstract TargetVertex:IVertex
        
    /// 정의(3/4) 동쪽: DS 모델은 지갑안에 동전이다.
    type ICoin = inherit IVertex
    type IWallet = inherit IVertex

    /// 정의(4/4) 북쪽: DS 모델은 고유 흐름을 갖는다.
    type IFlow =  abstract IsDag:bool;
    
    //DS 모델요소
    type ISystem = inherit INamed
    type ICpu = inherit INamed
    type IAlias = inherit INamed
    type ITxRx = inherit INamed
    type IAutoTag = inherit IBit
    type IStrongEdge = inherit IEdge
    type IWeakEdge = inherit IEdge
    type ISetEdge = inherit IEdge
    type IResetEdge = inherit IEdge
    type IEngine = inherit INamed
    type IBitReadable = inherit IBit
    type IBitWritable = 
        inherit IBit
        abstract SetValue:bool;
    type IBitReadWritable = 
        inherit IBitReadable
        inherit IBitWritable
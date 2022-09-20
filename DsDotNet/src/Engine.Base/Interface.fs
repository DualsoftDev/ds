// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Core

open System.Collections.Generic

[<AutoOpen>]
module Interface =

    /// Dualsoft object 공리 : 시스템 모든것은 비트다
    type IBit = interface end
    /// 이름
    type INamed  = abstract Name:string
    /// 고유값 Guid.NewGuid().ToString()
    type IUniqueId = abstract ID:string 

    /// 정의(1/4) 남쪽: DS 기본유닛은 Bit인 단일 자원이다.
    type IVertex =
        inherit IBit
     //   inherit IUniqueId
   
    /// 정의(2/4) 서쪽: DS 모델은 원인(들)/결과의 관계를 갖는다.
    type IEdge = 
        abstract Source  :IVertex 
        abstract Target  :IVertex
        abstract Causal  :EdgeCausal
        abstract ToText  :unit -> string


    /// 정의(3/4) 동쪽: DS 모델은 능동행위가 수동행위를 포함한다.
    type IActive =
        abstract Children:IVertex seq
    /// 정의(4/4) 북쪽: DS 모델은 고유 흐름을 갖는다.
    type IFlow   =
        abstract Edges:IEdge seq
        abstract Nodes:IVertex seq

    type ICall = 
        abstract Node :IVertex  
        abstract TXs  :IVertex seq 
        abstract RXs  :IVertex seq
    
    type ISystem    = 
        abstract Flows:IFlow seq
        
    type ICpu       = inherit INamed
    type IAlias     = inherit INamed
    type ITxRx      = inherit INamed

    type IAutoTag   = inherit IBit

    type IWeakEdge   = inherit IEdge
    type ISetEdge    = inherit IEdge
    type IResetEdge  = inherit IEdge
    type IStrongEdge = inherit IEdge

    type IEngine    = inherit INamed

    type IBitReadable = inherit IBit
    type IBitWritable = 
        inherit IBit
        abstract SetValue:bool;
    type IBitReadWritable = 
        inherit IBitReadable
        inherit IBitWritable

    type ICoin      = inherit IVertex
    type IWallet    = inherit IVertex
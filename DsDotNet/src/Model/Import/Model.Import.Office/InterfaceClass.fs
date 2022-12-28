// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System.Collections.Generic
open Engine.Core

[<AutoOpen>]
module InterfaceClass =

 ///인과의 노드 종류
    type NodeType =
        | REAL          //실제 나의 시스템 1 bit
        | REALEx        //다른 Flow real
        | CALL          //지시관찰 
        | IF            //인터페이스
        | COPY_VALUE          //시스템복사
        | COPY_REF           //시스템참조
        | DUMMY         //그룹더미
        | BUTTON        //버튼 emg,start, ...
        | LAMP          //램프 runmode,stopmode, ...
        | ACTIVESYS        //model ppt active  system
        with
            member x.IsReal = x = REAL || x = REALEx
            member x.IsCall = x = CALL
            member x.IsRealorCall =  x.IsReal || x.IsCall

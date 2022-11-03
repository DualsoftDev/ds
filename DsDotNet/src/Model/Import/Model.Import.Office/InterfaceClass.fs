// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System.Collections.Generic
open Engine.Core

[<AutoOpen>]
module InterfaceClass =

 

 
 ///인과의 노드 종류
    type NodeType =
        | MY            //실제 나의 시스템 1 bit
        | TR            //지시관찰 TX RX
        | TX            //지시만
        | RX            //관찰만
        | IF            //인터페이스
        | COPY          //시스템복사
        | DUMMY         //그룹더미
        | BUTTON        //버튼 emg,start, ...
        with
            member x.IsReal = x = MY
            member x.IsCall = match x with
                                |TR |TX |RX -> true
                                |_ -> false

            member x.IsRealorCall =  x.IsReal || x.IsCall
    // 행위 Bound 정의
    type Bound =
        | ThisFlow         //이   MFlow        내부 행위정의
        | OtherFlow        //다른 MFlow     에서 행위 가져옴
        | ExBtn            //버튼(call) 가져옴


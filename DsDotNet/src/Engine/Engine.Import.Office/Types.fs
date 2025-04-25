// Copyright (c) Dualsoft  All Rights Reserved.
namespace Engine.Import.Office

open System.Collections.Generic
open Engine.Core
open Dual.Common.Core.FS
open System

[<AutoOpen>]
module InterfaceClass =


    ///인과의 노드 종류
    type NodeType =
        | REAL //실제 나의 시스템 1 bit
        | REALExF //다른 Flow real
        | CALL //지시관찰
        | AUTOPRE //전제조건 Node
        //| CALLOPFunc  //Operator 함수전용
        //| CALLCMDFunc //Command  함수전용
        | IF_DEVICE //인터페이스
        | COPY_DEV //시스템복사 deivce
        | OPEN_EXSYS_LINK //시스템참조 Passive sytem(초기 로딩과 같은 경로 ExSystem 이면 Acive)
        | OPEN_EXSYS_CALL //시스템참조 Active sytem (초기 로딩과 다른 경로 ExSystem 이면 Passive)
        | DUMMY //그룹더미
        | BUTTON //버튼 emg,start, ...
        | LAYOUT //위치 디바이스 기준
        | LAMP //램프 runmode,stopmode, ...
        | CONDITIONorAction //READY조건, Drive 조건, EmergencyAction, ...

        member x.IsReal = x = REAL || x = REALExF
        member x.IsCall = x = CALL 
        member x.IsLoadSys = x = COPY_DEV || x = OPEN_EXSYS_LINK || x = OPEN_EXSYS_CALL
        member x.IsRealorCall = x.IsReal || x.IsCall
        member x.IsIF = x = IF_DEVICE

        member x.GetLoadingType() =
            match x with
            | OPEN_EXSYS_LINK
            | OPEN_EXSYS_CALL -> DuExternal
            | COPY_DEV -> DuDevice
            | _ -> failwithlog "error"

    type JobDevParam =
        {
            TaskDevCount: int
            InCount: int 
            OutCount: int 
        }
    let defaultJobDevParam() = { TaskDevCount = 1; InCount =  1; OutCount =  1 }
    
    type ViewType =
        | VFLOW
        | VREAL
        | VCALL
        | VIF
        | VCOPY_DEV
        | VOPEN_EXSYS_LINK
        | VDUMMY
        | VBUTTON
        | VLAMP
        | VCONDITION
        | VACTION


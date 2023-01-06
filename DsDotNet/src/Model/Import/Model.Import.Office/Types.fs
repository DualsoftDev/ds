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

    
    ///인터페이스 Tag 기본 형식
    type ExcelCase =
        | XlsAddress             //주소
        | XlsVariable            //변수
        | XlsAutoBTN             //자동 버튼
        | XlsManualBTN           //수동 버튼
        | XlsEmergencyBTN        //비상 버튼
        | XlsStopBTN             //정지 버튼
        | XlsRunBTN              //운전 버튼
        | XlsDryRunBTN           //시운전 시작 버튼
        | XlsClearBTN            //해지 버튼
        | XlsHomeBTN             //홈(원위치) 버튼
        | XlsEmergencyLamp       //비상모드 램프
        | XlsRunModeLamp         //운전모드 램프
        | XlsDryRunModeLamp      //시 운전모드  램프
        | XlsManualModeLamp      //수동 모드 램프
        | XlsStopModeLamp        //정지 모드 램프

    with
        member x.ToText() =
            match x with
            | XlsAddress        -> TextXlsAddress
            | XlsVariable       -> TextXlsVariable   
            | XlsAutoBTN        -> TextXlsAutoBTN        
            | XlsManualBTN      -> TextXlsManualBTN      
            | XlsEmergencyBTN   -> TextXlsEmergencyBTN   
            | XlsStopBTN        -> TextXlsStopBTN        
            | XlsRunBTN         -> TextXlsRunBTN         
            | XlsDryRunBTN      -> TextXlsDryRunBTN      
            | XlsClearBTN       -> TextXlsClearBTN       
            | XlsHomeBTN        -> TextXlsHomeBTN        
            | XlsEmergencyLamp  -> TextXlsEmergencyLamp  
            | XlsRunModeLamp    -> TextXlsRunModeLamp    
            | XlsDryRunModeLamp -> TextXlsDryRunModeLamp 
            | XlsManualModeLamp -> TextXlsManualModeLamp 
            | XlsStopModeLamp   -> TextXlsStopModeLamp   

    let TextToXlsType(txt:string) =
        match txt.ToLower() with
        | TextXlsAddress        ->  XlsAddress     
        | TextXlsVariable       ->  XlsVariable    
        | TextXlsAutoBTN        ->  XlsAutoBTN       
        | TextXlsManualBTN      ->  XlsManualBTN     
        | TextXlsEmergencyBTN   ->  XlsEmergencyBTN  
        | TextXlsStopBTN        ->  XlsStopBTN       
        | TextXlsRunBTN         ->  XlsRunBTN        
        | TextXlsDryRunBTN      ->  XlsDryRunBTN     
        | TextXlsClearBTN       ->  XlsClearBTN      
        | TextXlsHomeBTN        ->  XlsHomeBTN       
        | TextXlsEmergencyLamp  ->  XlsEmergencyLamp 
        | TextXlsRunModeLamp    ->  XlsRunModeLamp   
        | TextXlsDryRunModeLamp ->  XlsDryRunModeLamp
        | TextXlsManualModeLamp ->  XlsManualModeLamp
        | TextXlsStopModeLamp   ->  XlsStopModeLamp  
        | _ -> failwithf $"'{txt}' TextXlsType Error check type"

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
        | XlsCommand             //지시
        | XlsObserve             //관찰
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
            | XlsAddress        -> TextAddressDev 
            | XlsVariable       -> TextVariable   
            | XlsCommand        -> TextCommand    
            | XlsObserve        -> TextObserve    
            | XlsAutoBTN        -> TextAutoBTN        
            | XlsManualBTN      -> TextManualBTN      
            | XlsEmergencyBTN   -> TextEmergencyBTN   
            | XlsStopBTN        -> TextStopBTN        
            | XlsRunBTN         -> TextRunBTN         
            | XlsDryRunBTN      -> TextDryRunBTN      
            | XlsClearBTN       -> TextClearBTN       
            | XlsHomeBTN        -> TextHomeBTN        
            | XlsEmergencyLamp  -> TextEmergencyLamp  
            | XlsRunModeLamp    -> TextRunModeLamp    
            | XlsDryRunModeLamp -> TextDryRunModeLamp 
            | XlsManualModeLamp -> TextManualModeLamp 
            | XlsStopModeLamp   -> TextStopModeLamp   

    let TextToXlsType(txt:string) =
        match txt.ToLower() with
        | TextAddressDev     ->  XlsAddress     
        | TextVariable       ->  XlsVariable    
        | TextCommand        ->  XlsCommand     
        | TextObserve        ->  XlsObserve     
        | TextAutoBTN        ->  XlsAutoBTN       
        | TextManualBTN      ->  XlsManualBTN     
        | TextEmergencyBTN   ->  XlsEmergencyBTN  
        | TextStopBTN        ->  XlsStopBTN       
        | TextRunBTN         ->  XlsRunBTN        
        | TextDryRunBTN      ->  XlsDryRunBTN     
        | TextClearBTN       ->  XlsClearBTN      
        | TextHomeBTN        ->  XlsHomeBTN       
        | TextEmergencyLamp  ->  XlsEmergencyLamp 
        | TextRunModeLamp    ->  XlsRunModeLamp   
        | TextDryRunModeLamp ->  XlsDryRunModeLamp
        | TextManualModeLamp ->  XlsManualModeLamp
        | TextStopModeLamp   ->  XlsStopModeLamp  
        | _ -> failwithf $"'{txt}' TextXlsType Error check type"

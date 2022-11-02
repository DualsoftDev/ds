// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System.Runtime.CompilerServices
open Engine.Core

[<AutoOpen>]
module UtilEdge = 
    
    let [<Literal>] StartEdge         = EdgeType.Default                                // A>B	        약 시작 연결
    let [<Literal>] StartPush         = EdgeType.Strong                                 // A>>B	        강 시작 연결    
    let [<Literal>] ResetEdge         = EdgeType.Reset                                  // A|>B	        약 리셋 연결
    let [<Literal>] ResetPush         = EdgeType.Reset ||| EdgeType.Strong              // A||>B	    강 리셋 연결     
    let [<Literal>] StartReset        = EdgeType.EditorStartReset       // A=>B	    약시작리셋
    let [<Literal>] Interlock         = EdgeType.EditorInterlock      // A<||>B	    인터락 연결             
    let [<Literal>] StartEdgeRev      = EdgeType.Default                          ||| EdgeType.Reversed   
    let [<Literal>] StartPushRev      = EdgeType.Strong                           ||| EdgeType.Reversed   
    let [<Literal>] ResetEdgeRev      = EdgeType.Reset                            ||| EdgeType.Reversed   
    let [<Literal>] ResetPushRev      = EdgeType.Reset ||| EdgeType.Strong        ||| EdgeType.Reversed   
    let [<Literal>] StartResetRev     = EdgeType.Reset ||| EdgeType.Bidirectional ||| EdgeType.Reversed   

  


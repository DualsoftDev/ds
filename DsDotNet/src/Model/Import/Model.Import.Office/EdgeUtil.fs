// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Model.Import.Office

open System.Runtime.CompilerServices
open Engine.Core

[<AutoOpen>]
module UtilEdge =

    let [<Literal>] StartEdge  = ModelingEdgeType.Start                                // A>B	        약 시작 연결
    let [<Literal>] StartPush  = ModelingEdgeType.Strong                                 // A>>B	        강 시작 연결
    let [<Literal>] ResetEdge  = ModelingEdgeType.Reset                                  // A|>B	        약 리셋 연결
    let [<Literal>] ResetPush  = ModelingEdgeType.Reset ||| ModelingEdgeType.Strong              // A||>B	    강 리셋 연결
    let [<Literal>] StartReset = ModelingEdgeType.EditorStartReset       // A=>B	    약시작리셋
    let [<Literal>] Interlock  = ModelingEdgeType.EditorInterlock      // A<||>B	    인터락 연결

    //<kwak>
    //let [<Literal>] StartEdgeRev      = ModelingEdgeType.Default                          ||| ModelingEdgeType.Reversed
    //let [<Literal>] StartPushRev      = ModelingEdgeType.Strong                           ||| ModelingEdgeType.Reversed
    //let [<Literal>] ResetEdgeRev      = ModelingEdgeType.Reset                            ||| ModelingEdgeType.Reversed
    //let [<Literal>] ResetPushRev      = ModelingEdgeType.Reset ||| ModelingEdgeType.Strong        ||| ModelingEdgeType.Reversed
    //let [<Literal>] StartResetRev     = ModelingEdgeType.Reset ||| ModelingEdgeType.Bidirectional ||| ModelingEdgeType.Reversed

  


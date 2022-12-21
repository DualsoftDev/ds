namespace Dsu.PLCConverter.FS

open System.Diagnostics
open System.Runtime.CompilerServices
open System.Collections.Generic
open Dual.Common
open System

/// Contacts 관련 Operators
type Op =
    | Load | And | Or 
    with
        member x.ToText =
            match x with
            | Load -> "LOAD"
            | And -> "AND"
            | Or -> "OR"
        static member getOp(op:string) =
            match op.ToUpper() with
                | "LOAD"-> Load
                | "AND" -> And
                | "OR"  -> Or
                | _ -> failwithlog (sprintf "Invalid op [%s] (Op ={Load, And, Or})" op)

/// 비교 관련 Operators
type OpComp =
    | NotComp | GT | GE | EQ | LE | LT | NE 
    with
        member x.ToText =
            match x with
            | NotComp -> "NotComp"
            | GT -> "GT"
            | GE -> "GE"
            | EQ -> "EQ"
            | LE -> "LE"
            | LT -> "LT"
            | NE -> "NE"

/// 비교 관련 Operators 타입
type OpCompType =
    | NoType | INT | DINT | REAL | STRING 
    with
        member x.ToText =
            match x with
            | NoType -> "NoType"
            | INT -> "INT"
            | DINT -> "DINT"
            | REAL -> "REAL"
            | STRING -> "STRING"
        static member getOp(op:string) =
            match op.ToUpper() with
                | "NoType"-> NoType
                | "INT" -> INT
                | "DINT"  -> DINT
                | "REAL"  -> REAL
                | "STRING"  -> STRING
                | _ -> failwithlog (sprintf "Invalid op [%s] (Op ={Load, And, Or})" op)

/// Load Operators
type OpLoad =
    | Base | AndLoad | OrLoad
    with
    member x.ToText =
        match x with
        | Base -> "Base"
        | AndLoad -> "AndLoad"
        | OrLoad -> "OrLoad"
    

/// Operators Option
type OpOpt =
    | Normal | Rising | Falling | Neg | Inverter | Compare | RisingEdge | FallingEdge
    with
        member x.ToText =
            match x with
            | Normal -> "Normal"
            | Rising -> "↑"
            | Falling -> "↓"
            | Neg -> "!"
            | Inverter -> "*"
            | Compare -> "Compare"
            | RisingEdge -> "RisingEdge"
            | FallingEdge -> "FallingEdge"

            
/// Instance Option
type InstOpt =
    | R_TRIG | F_TRIG | FF 
    with
        member x.ToText =
            match x with
            | R_TRIG -> "R_TRIG"
            | F_TRIG -> "F_TRIG"
            | FF -> "FF"
             
type Inst =  InstOpt * string   //InstType * InstName
                     
type InstFun =
    static member getInst(insts:seq<Inst>, inst:InstOpt) =
                 match inst with
                 | R_TRIG 
                 | F_TRIG 
                 | FF     -> sprintf "%s%d" inst.ToText (insts |> Seq.filter (fun (f,n) -> inst = f) |> Seq.length)

type contact =  string list * Op * OpOpt * OpComp * OpCompType

[<DebuggerDisplay("Start({SX},{SY})->End({EX},{EY})  Size({SizeX},{SizeY})")>]
type LoadPoint(sx:int,sy:int,ex:int,ey:int) = 
    member val SX = sx with get, set
    member val SY = sy with get, set
    member val EX = ex with get, set
    member val EY = ey with get, set
    member x.Copy() = LoadPoint(x.SX, x.SY, x.EX, x.EY)
    member x.Update(newPt:LoadPoint) = x.EX <- newPt.EX; x.EY <- newPt.EY; x.SX <- newPt.SX; x.SY <- newPt.SY; 
    member x.IsSame(newPt:LoadPoint) = x.EX = newPt.EX && x.EY = newPt.EY && x.SX = newPt.SX && x.SY = newPt.SY; 
    member x.Shift(nx:int,ny:int) = x.EX <- x.EX + nx; x.EY <- x.EY + ny;x.SX <- x.SX + nx; x.SY <- x.SY + ny
    member x.SizeX =  x.EX - x.SX  //size 0은 mLoad()로 부터 좌표만 이용한 경우
    member x.SizeY =  x.EY - x.SY
    member x.SizeZero = if(x.SizeX = 0 && x.SizeY = 0) then true else false

/// 프로그램을 저장하는 기본 구조
type LoadBlock = 
    /// Primitives
    | LoadBase  of List<contact> * LoadPoint
    /// Extend Expressions
    | Extend    of List<contact> * LoadPoint * LoadPoint   //Base Point * Extend Point
    | Mix    of LoadBlock * OpLoad * LoadBlock * LoadBlock //LoadBase * OpLoad * LoadBase * Extend
    /// Empty
    | Empty
    with
        member x.ToText =
            match x with
            | LoadBase (a,b) -> "LoadBase"
            | Extend (a,b,c) -> "Extend"
            | Mix (a,b,c,d) -> "Mix"
            | Empty -> "Empty"
        member x.Copy() =
            match x with
            | LoadBase (a,b) -> 
                let newListContact = List[]
                newListContact.AddRange(a)
                LoadBase (newListContact,b.Copy())
            | Extend (a,b,c) ->
                let newListContact = List[]
                newListContact.AddRange(a)
                Extend (newListContact, b.Copy(), c.Copy())
            | Mix (a,b,c,d) -> Mix (a.Copy(),b,c.Copy(),d.Copy())
            | Empty -> Empty
        

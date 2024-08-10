namespace PLC.CodeGen.Common

open System.Diagnostics
open Dual.Common.Core.FS
open Engine.Core
open System.Runtime.CompilerServices
open System

[<AutoOpen>]
module FlatExpressionModule2 =

    type FlatExpression with
        member x.TryGetTerminal(): IExpressionizableTerminal option =
            match x with
            | FlatTerminal (t, _p, _n) -> Some t
            | _ -> None

    let private createTerminal(text:string) : IExpressionizableTerminal =
        {   new System.Object() with
                member x.Finalize() = ()
            interface IExpressionizableTerminal with
                member x.ToText() = text
                member x.DataType = typedefof<bool>
            interface ITerminal with
                member x.Variable = None
                member x.Literal = Some(x:?>IExpressionizableTerminal)
        }
    /// '_ON' 에 대한 flat expression
    //let fakeAlwaysOnFlatExpression:FlatExpression = FlatTerminal(createTerminal("_ON"), None, false)
    let mutable fakeAlwaysOnFlatExpression:IFlatExpression = getNull<IFlatExpression>()

    /// '_OFF' 에 대한 flat expression
    //let fakeAlwaysOffFlatExpression:FlatExpression = FlatTerminal(createTerminal("_OFF"), None, false)
    let mutable fakeAlwaysOffFlatExpression:IFlatExpression = getNull<IFlatExpression>()


[<Extension>]
type FlatExpressionExt =
    /// FlatExpression 이 _ON 이나 _OFF 상수인 경우, Some(true), Some(false) 값을 반환하고, 그 이외의 모든 경우에는 None 반환
    [<Extension>]
    static member TryGetLiteralBoolValue(exp:FlatExpression) =
        match exp.TryGetTerminal() with
        | Some (:? INamedExpressionizableTerminal as v) ->
            let text = v.StorageName.ToUpper()
            if text.IsOneOf("_ON", "TRUE") then
                Some true
            elif text.IsOneOf("_OFF", "FALSE") then
                Some false
            else
                None
        | Some (:? ILiteralHolder as h) -> Some (h.BoxedEvaluatedValue = true)
        | _ -> None

    //[<Extension>]
    //static member IsTrue(exp:IExpressionizableTerminal) =
    //    match exp with
    //    | :? INamedExpressionizableTerminal as v when v.StorageName.ToUpper().IsOneOf("_ON", "TRUE") -> true
    //    | :? ILiteralHolder as h when h.BoxedEvaluatedValue = true -> true
    //    | _ -> false
    //[<Extension>]
    //static member IsFalse(exp:IExpressionizableTerminal) =
    //    match exp with
    //    | :? INamedExpressionizableTerminal as v when v.StorageName.ToUpper().IsOneOf("_OFF", "FALSE") -> true
    //    | :? ILiteralHolder as h when h.BoxedEvaluatedValue = false -> true
    //    | _ -> false
    //[<Extension>] static member IsTrue(exp:FlatExpression)  = exp.TryGetTerminal().Map(fun t -> t.IsTrue()) .DefaultValue(false)
    //[<Extension>] static member IsFalse(exp:FlatExpression) = exp.TryGetTerminal().Map(fun t -> t.IsFalse()).DefaultValue(false)

[<AutoOpen>]
module FlatExpressionModule3 =


    let rec flattenExpressionT (expression: IExpression<'T>) : IFlatExpression =
        match expression with
        | :? Expression<'T> as express ->
            match express with
            | DuTerminal(DuVariable t) -> FlatTerminal(t, None, false)
            | DuTerminal(DuLiteral b) -> FlatTerminal(b, None, false)

            (* rising/falling/negation 은 function 으로 구현되어 있으며,
               해당 function type 에 따라서 risng/falling/negation 의 contact/coil 을 생성한다.
               (Terminal<'T> 이 generic 이어서 DuTag 에 bool type 으로 제한 할 수 없음.
                Terminal<'T>.Evaluate() 가 bool type 으로 제한됨 )
             *)
            | DuFunction { Name = (FunctionNameRising | FunctionNameFalling) as n
                           Arguments = [ (:? Expression<bool> as arg) ] } ->
                let positivePulse = Some (n = FunctionNameRising)
                match arg with
                | DuTerminal(DuVariable v) -> FlatTerminal(v, positivePulse, false)
                | DuTerminal(DuLiteral b) -> FlatTerminal(b, positivePulse, false)
                | DuFunction ({ Name = "!"
                                Arguments = (:? Expression<bool> as arg0)::[]} as _f) ->
                    match arg0 with
                    | DuTerminal(DuVariable v) -> FlatTerminal(v, positivePulse, true)
                    | DuTerminal(DuLiteral b) -> FlatTerminal(b, positivePulse, true)
                    | _ -> failwithlog "ERROR"
                    //FlatTerminal(b, (n = FunctionNameRising), false)

                | _ -> failwithlog "ERROR"
            | DuFunction fs ->
                let op =
                    match fs.Name with
                    | "&&" -> Op.And
                    | "||" -> Op.Or
                    | "!" -> Op.Neg
                    | FunctionNameRisingAfter -> Op.RisingAfter
                    | FunctionNameFallingAfter -> Op.FallingAfter

                    | IsOpC _ -> // XGK 일때만 유효
                        Op.OpCompare fs.Name

                    | IsOpA _ // -> Op.OpArithmetic fs.Name
                    | _ -> failwithlog "ERROR"

                let flatArgs = fs.Arguments |> map flattenExpression |> List.cast<FlatExpression>
                FlatNary(op, flatArgs) |> optmizeFlatExpression

        //| :? DuTerminal(DuVariable (v:TypedValueStorage<'T'>)) -> FlatTerminal(v, None, false)

        | _ -> failwithlog "Not yet for non boolean expression"

    // <kwak> IExpression<'T> vs IExpression : 강제 변환
    and flattenExpression (expression: IExpression) : IFlatExpression =
        match expression with
        | :? IExpression<bool> as exp -> flattenExpressionT exp
        | :? IExpression<int8> as exp -> flattenExpressionT exp
        | :? IExpression<uint8> as exp -> flattenExpressionT exp
        | :? IExpression<int16> as exp -> flattenExpressionT exp
        | :? IExpression<uint16> as exp -> flattenExpressionT exp
        | :? IExpression<int32> as exp -> flattenExpressionT exp
        | :? IExpression<uint32> as exp -> flattenExpressionT exp
        | :? IExpression<int64> as exp -> flattenExpressionT exp
        | :? IExpression<uint64> as exp -> flattenExpressionT exp
        | :? IExpression<single> as exp -> flattenExpressionT exp
        | :? IExpression<double> as exp -> flattenExpressionT exp
        | :? IExpression<string> as exp -> flattenExpressionT exp
        | :? IExpression<char> as exp -> flattenExpressionT exp

        | _ -> failwithlog "NOT yet"
    and flattenOptimizeExpression (expression: IExpression) : IFlatExpression =
        flattenExpression expression |> optmizeFlatExpression

        //|> optmizeFlatExpression


    and optmizeFlatExpression (iexpr: IFlatExpression) : IFlatExpression =
        let expr = iexpr :?> FlatExpression
        let xxx =
            match expr with
            // negation 이 정의된 bool 상수 변환.  e.g !_ON -> _OFF 로 변환
            | FlatTerminal (_t, None, true) ->
                match expr.TryGetLiteralBoolValue() with
                | Some true  -> fakeAlwaysOnFlatExpression
                | Some false -> fakeAlwaysOffFlatExpression
                | _ -> iexpr

            // 그외 모든 terminal case : don't touch
            | FlatTerminal (_t, _p, _n) -> iexpr

            | FlatNary(And, terms) ->
                let mutable shortCircuited = false // e.g: TRUE || .... -> TRUE,    FALSE && ... -> FALSE
                let validTerms = ResizeArray<IFlatExpression>()
                for term in terms |> map (fun a -> optmizeFlatExpression a :?> FlatExpression) do
                    if not shortCircuited then
                        match term with
                        | FlatTerminal (_t, _p, _n) ->
                            match term.TryGetLiteralBoolValue() with
                            | Some true -> ()                       // AND TRUE 는 (* 1) 와 마찬가지로 무시
                            | Some false -> shortCircuited <- true  // AND FALSE 는 (* 0) 와 마찬가지로 short circuiting
                            | None -> validTerms.Add (term :> IFlatExpression)
                        // ! TRUE -> FALSE 처리
                        | FlatNary(Neg, [neg]) when neg.TryGetLiteralBoolValue().IsSome ->
                            if neg.TryGetLiteralBoolValue().Value then
                                fakeAlwaysOffFlatExpression
                            else
                                fakeAlwaysOnFlatExpression
                            |> validTerms.Add
                        | _ ->
                            validTerms.Add(term)

                if shortCircuited then
                    fakeAlwaysOffFlatExpression // OFF
                elif validTerms.isEmpty() then
                    fakeAlwaysOnFlatExpression // ON
                else
                    FlatNary(And, validTerms |> Seq.cast<FlatExpression> |> List.ofSeq)

            | FlatNary(Or, terms) ->
                let mutable shortCircuited = false // e.g: TRUE || .... -> TRUE,    FALSE && ... -> FALSE
                let validTerms = ResizeArray<IFlatExpression>()
                for term in terms |> map (fun a -> optmizeFlatExpression a :?> FlatExpression) do
                    if not shortCircuited then
                        match term with
                        | FlatTerminal (_t, _p, _n) ->
                            match term.TryGetLiteralBoolValue() with
                            | Some true -> shortCircuited <- true    // OR TRUE 는 (* 1) 와 마찬가지로 무시
                            | Some false -> ()                       // OR FALSE 는 (* 0) 와 마찬가지로 short circuiting
                            | None -> validTerms.Add (term :> IFlatExpression)
                        // ! TRUE -> FALSE 처리
                        | FlatNary(Neg, [neg]) when neg.TryGetLiteralBoolValue().IsSome ->
                            if neg.TryGetLiteralBoolValue().Value then
                                fakeAlwaysOffFlatExpression
                            else
                                fakeAlwaysOnFlatExpression
                            |> validTerms.Add
                        | _ ->
                            validTerms.Add(term)

                if shortCircuited then
                    fakeAlwaysOnFlatExpression  // ON
                elif validTerms.isEmpty() then
                    fakeAlwaysOffFlatExpression // OFF
                else
                    FlatNary(Or, validTerms |> Seq.cast<FlatExpression> |> List.ofSeq)




            //| FlatNary(Neg, [ FlatNary(Neg, [x]) ]) -> x :> IFlatExpression
            //| FlatNary(Neg, [ FlatTerminal (value, pulse, neg)]) ->
            //    match t.Literal with
            //    | Some l when t..

            //| FlatNary(risingOrFallingAfter, args) when risingOrFallingAfter = RisingAfter || risingOrFallingAfter = FallingAfter ->
            //    iexpr

            //| _ -> failwithlog "ERROR"
            | _ ->
                iexpr
        xxx


    /// expression 이 차지하는 가로, 세로 span 의 width 와 height 를 반환한다.
    let precalculateSpan (expr: FlatExpression) =
        let rec helper (expr: FlatExpression) : int * int =
            match expr with
            | FlatTerminal _ -> 1, 1
            | FlatNary(And, ands) ->
                let spanXYs = ands |> map helper
                let spanX = spanXYs |> map fst |> List.sum
                let spanY = spanXYs |> map snd |> List.max
                spanX, spanY
            | FlatNary(Or, ors) ->
                let spanXYs = ors |> map helper
                let spanX = spanXYs |> map fst |> List.max
                let spanY = spanXYs |> map snd |> List.sum
                spanX, spanY
            | FlatNary(Neg, [ neg ]) -> helper neg

            | FlatNary(risingOrFallingAfter, args) when risingOrFallingAfter = RisingAfter || risingOrFallingAfter = FallingAfter ->
                let spanXYs = args |> map helper
                let spanX = (spanXYs |> map fst |> List.sum) + 1
                let spanY = spanXYs |> map snd |> List.max
                spanX, spanY

            | _ -> failwithlog "ERROR"

        helper expr

    /// 우측으로 바로 function block 을 붙일 수 있는지 검사.
    /// false 반환 시, hLine (hypen) 을 적어도 하나 추가해야 function blcok 을 붙일 수 있다.
    (*
        false 반환 case
            - toplevel 이 OR function
            - toplevel 이 AND 이고, AND 의 마지막이 OR function
     *)
    let rec isFunctionBlockConnectable (expr: FlatExpression) =
        match expr with
        | (FlatTerminal _ | FlatNary(Neg, _)) -> true
        | FlatNary(And, ands) -> ands |> List.last |> isFunctionBlockConnectable
        | FlatNary(Or, _) -> false
        | _ -> failwithlog "ERROR"

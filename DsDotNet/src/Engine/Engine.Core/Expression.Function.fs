namespace rec Engine.Core

[<AutoOpen>]
module ExpressionFunctionModule =

    let createBinaryExpression (opnd1:obj) (op:string) (opnd2:obj) =
        let t1 = getTypeOfBoxedExpression opnd1
        let t2 = getTypeOfBoxedExpression opnd2
        if t1 <> t2 then
            failwith "ERROR: Type mismatch"

        let args = [box opnd1; opnd2]

        if t1 = typeof<byte> then
            match op with
            | "+" -> adduy args
            | "-" -> subuy args
            | "*" -> muluy args
            | "/" -> divuy args
            | _ -> failwith "NOT Yet"
            |> box
        elif t1 = typeof<sbyte> then
            match op with
            | "+" -> addy args
            | "-" -> suby args
            | "*" -> muly args
            | "/" -> divy args
            | _ -> failwith "NOT Yet"
            |> box
        elif t1 = typeof<int16> then
            match op with
            | "+" -> adds args
            | "-" -> subs args
            | "*" -> muls args
            | "/" -> divs args
            | _ -> failwith "NOT Yet"
            |> box
        elif t1 = typeof<uint16> then
            match op with
            | "+" -> addus args
            | "-" -> subus args
            | "*" -> mulus args
            | "/" -> divus args
            | _ -> failwith "NOT Yet"
            |> box
        elif t1 = typeof<int32> then
            match op with
            | "+" -> add args
            | "-" -> sub args
            | "*" -> mul args
            | "/" -> div args
            | _ -> failwith "NOT Yet"
            |> box
        elif t1 = typeof<uint32> then
            match op with
            | "+" -> addu args
            | "-" -> subu args
            | "*" -> mulu args
            | "/" -> divu args
            | _ -> failwith "NOT Yet"
            |> box
        elif t1 = typeof<double> then
            match op with
            | "+" -> addd args
            | "-" -> subd args
            | "*" -> muld args
            | "/" -> divd args
            | _ -> failwith "NOT Yet"
            |> box
        elif t1 = typeof<single> then
            match op with
            | "+" -> addf args
            | "-" -> subf args
            | "*" -> mulf args
            | "/" -> divf args
            | _ -> failwith "NOT Yet"
            |> box
        elif t1 = typeof<string> then
            match op with
            | "+" -> concat args
            | _ -> failwith "ERROR"
            |> box
        else
            failwith "ERROR"

    let createCustomFunctionExpression (funName:string) (args:Args) =
        match funName with
        | "Int" -> Int args |> box
        | "Bool" -> Bool args |> box
        | "sin" -> sin args |> box
        //| "cos" -> cos args |> box
        //| "tan" -> tan args |> box
        | _ -> failwith "NOT yet"

    let evaluateBoxedExpression (boxedExpr:obj) =
        let expr = boxedExpr :?> IExpression
        expr.BoxedEvaluatedValue


    let resolve (expr:Expression<'T>) = expr.Evaluate() |> unbox




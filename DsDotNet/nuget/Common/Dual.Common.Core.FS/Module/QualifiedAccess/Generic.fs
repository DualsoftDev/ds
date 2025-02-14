namespace Dual.Common.Core.FS
open System
open Microsoft.FSharp.Quotations
open Microsoft.FSharp.Quotations.Patterns
open Dual.Common.Core.FS

#nowarn "25"    // warning FS0025: 이 식의 패턴 일치가 완전하지 않습니다.
/// F# Generic
[<AutoOpen>]
module GenericModule =

    // https://stackoverflow.com/questions/2140079/how-to-cast-an-object-to-a-list-of-generic-type-in-f
    let ( |GenericType|_| ) =
        (* methodinfo for typedefof<_> *)
        let tdo =
            let (Call(None,t,[])) = <@ typedefof<_> @>
            t.GetGenericMethodDefinition()
        (* match type t against generic def g *)
        let rec tymatch t (g:Type) =
            if t = typeof<obj> then None
            elif g.IsInterface then
                let ints = if t.IsInterface then [|t|] else t.GetInterfaces()
                ints |> Seq.tryPick (fun t -> if (t.GetGenericTypeDefinition() = g) then Some(t.GetGenericArguments()) else None)
            elif t.IsGenericType && t.GetGenericTypeDefinition() = g then
                Some(t.GetGenericArguments())
            else
                tymatch (t.BaseType) g
        fun (e:Expr<Type>) (t:Type) ->
            match e with
            | Call(None,mi,[]) ->
                if (mi.GetGenericMethodDefinition() = tdo) then
                    let [|ty|] = mi.GetGenericArguments()
                    if ty.IsGenericType then
                        let tydef = ty.GetGenericTypeDefinition()
                        tymatch t tydef
                    else None
                else
                    None
            | _ ->
                None


    module private TestMe2 =

        // 마이크로소프트 문서
        // https://learn.microsoft.com/en-us/dotnet/framework/reflection-and-codedom/how-to-examine-and-instantiate-generic-types-with-reflection


        // https://stackoverflow.com/questions/37214017/f-generic-type-instanciation-from-object-and-not-type

        type Foo<'a,'b> =
            new () = {}

        type First = class end

        (* create Foo<int, int> programatically *)
        let genericFooType = typedefof<Foo<_,_>>.GetGenericTypeDefinition()
        let t = typedefof<int>
        let fooType = genericFooType.MakeGenericType(t, t)
        let foo = fooType.GetConstructor([||]).Invoke([||])
        foo.GetType() = typeof<Foo<int,int>> |> verify


        (* Generic Discriminated Union Case 를 생성하는 방법은 ????
            type MyUnion<'T> =
                | MySome of 'T
                | MyNone
        *)




        let o = [1..10]
        match o.GetType() with
        | GenericType <@ typedefof<list<_>> @> [|t|] -> $"List<{t.Name}>"//addChildListUntyped(t,o)
        | _                                          -> $"Something else"
        |> ignore
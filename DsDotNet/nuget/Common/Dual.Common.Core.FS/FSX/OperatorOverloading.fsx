// https://learn.microsoft.com/en-us/dotnet/fsharp/language-reference/operator-overloading

(* Unary '+', '-' 에 대해서는 uniary 임을 나타내기 위해서 '~' 를 포함해야 한다. *)

type Vector(x: float, y : float) =
   member this.x = x
   member this.y = y
   static member (~-) (v : Vector) =
     Vector(-1.0 * v.x, -1.0 * v.y)
   static member (*) (v : Vector, a) =
     Vector(a * v.x, a * v.y)
   static member (*) (a, v: Vector) =
     Vector(a * v.x, a * v.y)
   override this.ToString() =
     this.x.ToString() + " " + this.y.ToString()

let v1 = Vector(1.0, 2.0)

let v2 = v1 * 2.0
let v3 = 2.0 * v1

let v4 = - v2

printfn "%s" (v1.ToString())
printfn "%s" (v2.ToString())
printfn "%s" (v3.ToString())
printfn "%s" (v4.ToString())


(*
 * 새로운 operator 생성

 - Allowed operator characters are !, $, %, &, *, +, -, ., /, <, =, >, ?, @, ^, |, and ~.
 - The ~ character has the special meaning of making an operator unary, and is not part of the operator character sequence. Not all operators can be made unary.
 *)



let inline (+?) (x: int) (y: int) = x + 2*y
printf "%d" (10 +? 1)


let inline (-->)       x f = FAdhoc_map         $ x <| f
let inline (>>-)       x f = FAdhoc_map         $ x <| f


let (~-) x =
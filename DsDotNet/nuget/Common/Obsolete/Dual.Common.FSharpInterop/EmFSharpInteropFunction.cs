using System;
using Microsoft.FSharp.Core;

namespace Dual.Common.Core.FSharpInterOp
{
    /*
    // F# 에서 fun argument 를 받는 함수를 C# 에서 lambda 로 호출하려면...
    let myFSharpFun (f:string->unit) = printfn "%s" s

    // C# 에서 lambda 를 F# function 으로 변환해서 호출
    var f = FuncConvert.ToFSharpFunc( (string s) => Trace.WriteLine($"{s}")));
    myFSharpFun(f)
    */

    // http://blog.leifbattermann.de/2015/05/28/convert-action-to-fsharp-function/
    public static class EmFSharpInteropFunction
    {
        private static readonly Unit Unit = (Unit)Activator.CreateInstance(typeof(Unit), true);

        private static Func<T, Unit> ToFunc<T>(this Action<T> action)
        {
            return x => { action(x); return Unit; };
        }

        public static FSharpFunc<T, Unit> ToFSharpFunc<T>(this Action<T> action)
        {
            return FSharpFunc<T, Unit>.FromConverter(new Converter<T, Unit>(action.ToFunc()));
        }
    }
}

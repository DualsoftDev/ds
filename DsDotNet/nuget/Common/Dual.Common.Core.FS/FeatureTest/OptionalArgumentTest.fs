
namespace Dual.Common.Core.FS.Test
open Dual.Common.Core.FS

open System.Runtime.CompilerServices
open System.Runtime.InteropServices

//module OptionalArgumentTest =
type ExtensionClass =
    /// default argument test 용
    /// F# 에서는 default 가 동작하나, C# 에서는 default parameter 가 동작하지 않음.
    [<Extension>] static member AddWithFSharpDefault(x1:int, ?x2:int) = x1 + (x2 |? 1)

    /// C#, F# 모두 통용되는 default parameter
    // [<Optional; DefaultParameterValue(1)>]  를 축약해서 새로운 custom attribute 를 정의하는 것은 불가능.
    [<Extension>] static member AddWithDotNetDefault(x1:int, [<Optional; DefaultParameterValue(1)>] x2) = x1 + x2

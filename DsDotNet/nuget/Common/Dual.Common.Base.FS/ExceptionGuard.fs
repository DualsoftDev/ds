namespace Dual.Common.Base.FS

open System
open System.Diagnostics
open System.Runtime.CompilerServices

type Exn =
    /// F# 용: Guard 상태에서의 f 수행.  실패시 handler 호출
    static member Guard(f:unit->unit, handler:exn->unit) =
        try
            f()
        with ex ->
            handler ex


    /// C# 용: Guard 상태에서의 action 수행.  실패시 handler 호출
    static member CsGuard(action:Action, handler:Action<exn>) =
        Exn.Guard(action.Invoke, handler.Invoke)

    /// C# 용: Guard 에서 action 미지정시, 실패시 수행할 default handler
    static member val CsHandler =
        Action<exn>(fun ex ->
            Trace.WriteLine($"Exception occurred: {ex.Message}" )) with get, set

    /// C# 용: Guard 상태에서의 action 수행.  실패시 CsHandler 에 저장된 handler 호출
    static member CsGuard(acttion:Action) =
        Exn.Guard(acttion.Invoke, Exn.CsHandler.Invoke)


    /// F# 용: Guard 에서 action 미지정시, 실패시 수행할 default handler
    static member val FsHandler =
        (fun (ex:exn) -> Trace.WriteLine($"Exception occurred: {ex.Message}")) with get, set

    /// F# 용: Guard 상태에서의 action 수행.  실패시 FsHandler 에 저장된 handler 호출
    static member Guard(f:unit->unit) =
        try
            f()
        with ex ->
            Exn.FsHandler ex
type ExnExtension =
    [<Extension>] static member Handle(handler:exn->unit, excetpion:exn) = handler excetpion
    [<Extension>]
    static member Handle(handler:Action<exn>, excetpion:exn) =
        if handler = null then
            raise excetpion
        else
            handler.Invoke(excetpion)
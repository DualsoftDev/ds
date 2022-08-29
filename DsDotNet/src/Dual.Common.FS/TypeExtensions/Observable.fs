namespace Dual.Common

open System
open System.Runtime.CompilerServices


[<AutoOpen>]
module ObservableModule =
    [<Extension>]
    type ObservableExt =
        /// IObservable<'t> 의 subclass 를  IObservable<obj> 로 변환.  e.g Subject<XXX> -> IObservable<obj>
        ///
        /// Microsoft.FSharp.Control.Observable 의 대부분 기능이 IObservable<obj> 를 기반으로 동작한다.
        /// Subject<XXX> 객체에 대해서, 대부분의  Microsoft.FSharp.Control.Observable 를직접적으로 사용할 수 없어서
        /// IObservable<obj> 로 먼저 변환한다.
        ///
        /// e.g let subj:Subject<MyObservable> = ...; 
        /// let obs:IObservable<obj> = subj.ToIObservable() 
        [<Extension>]
        static member ToIObservable(subj:#IObservable<'t>) =
            subj
            :> IObservable<'t>
            |> Observable.map box



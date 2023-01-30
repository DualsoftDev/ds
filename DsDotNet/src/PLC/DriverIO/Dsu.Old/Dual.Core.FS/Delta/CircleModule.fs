namespace Old.Dual.Core

open System.Diagnostics
open System.Runtime.CompilerServices
open Old.Dual.Common

[<AutoOpen>]
module CircleModule =
    [<Extension>] // type CircleModuleExt =
    type CircleModuleExt =
        /// container task 에 포함된 circle 의 left slot (In) 을 반환
        [<Extension>]
        static member GetLeftSlots(circle:Circle, containerTask:Task) =
            [
                for e in containerTask.Edges do
                    match e.Source, e.Target with
                    //| (:? Slot as s), _ ->
                    //    yield s
                    | _, (:? CircleSlot as cs) when cs.Circle = circle ->
                        yield cs.Slot
                    | _ -> ()
            ]
        /// container task 에 포함된 circle 의 right slot (Out) 을 반환
        [<Extension>]
        static member GetRightSlots(circle:Circle, containerTask:Task) =
            [
                for e in containerTask.Edges do
                    match e.Source, e.Target with
                    //| _, (:? Slot as s) ->
                    //    yield s
                    | (:? CircleSlot as cs), _ when cs.Circle = circle ->
                        yield cs.Slot
                    | _ -> ()
            ]

        /// container task 에 포함된 circle 의 모든 slot 을 반환
        [<Extension>]
        static member GetSlots(circle:Circle, containerTask:Task) =
            (circle.GetLeftSlots(containerTask) |> List.cast<Slot>) @ (circle.GetRightSlots(containerTask) |> List.cast<Slot>)



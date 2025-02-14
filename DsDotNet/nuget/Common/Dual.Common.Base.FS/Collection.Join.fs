namespace Dual.Common.Base.FS

open System
open Microsoft.FSharp.Core

[<AutoOpen>]
module CollectionJoin =

    (*
     * 'i: input, 'o: output, 'k: key, 'r: result, okF: outerKeySelector, ikF: inputKeySelector, rF: resultSelector
     *)

    /// groupJoin 함수 (일반 함수 스타일)
    let groupJoin (outer: 'o seq) (inner: 'i seq)
                  (okF: 'o -> 'k) (ikF: 'i -> 'k)
                  (rF: 'o -> 'i seq -> 'r) : 'r seq =
        outer
        |> Seq.map (fun outerItem ->
            let outerKey = okF outerItem
            let innerGroup =
                inner
                |> Seq.filter (fun innerItem -> ikF innerItem = outerKey)
            rF outerItem innerGroup)

    /// innerJoin 함수
    // 'i: input, 'o: output, 'k: key, 'r: result, okF: outerKeySelector, ikF: inputKeySelector, rF: resultSelector
    let innerJoin (outer: 'o seq) (inner: 'i seq)
                     (okF: 'o -> 'k)
                     (ikF: 'i -> 'k)
                     (rF: 'o -> 'i -> 'r) : 'r seq =
        seq {
            for outerItem in outer do
                let outerKey = okF outerItem
                for innerItem in inner do
                    if ikF innerItem = outerKey then
                        yield rF outerItem innerItem
        }
        (*
            // InnerJoin 사용 예시
            let innerJoinResult = innerJoin outerSeq innerSeq
                                         (fun outer -> outer.Id)
                                         (fun inner -> inner.OuterId)
                                         (fun outer inner -> outer.Name, inner.Value)
        *)

    /// naturalJoin 함수
    // 'i: input, 'o: output, 'k: key, 'r: result, okF: outerKeySelector, ikF: inputKeySelector, rF: resultSelector
    let naturalJoin (outer: 'o seq) (inner: 'i seq)
                    (keyF: 'o -> 'i -> bool)
                    (rF: 'o -> 'i -> 'r) : 'r seq =
        seq {
            for outerItem in outer do
                for innerItem in inner do
                    if keyF outerItem innerItem then
                        yield rF outerItem innerItem
        }
        (*
            // NaturalJoin 사용 예시
            let naturalJoinResult = naturalJoin outerSeq innerSeq
                                           (fun outer inner -> outer.Id = inner.OuterId)
                                           (fun outer inner -> outer.Name, inner.Value)
        *)


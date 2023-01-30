#I @"..\..\bin"
#r "Old.Dual.Common.FS.dll"
open Old.Dual.Common.ComputationExpressions
open Old.Dual.Common.StateBuilder

// #load "StateBuilder.fs"


let moveUp () =
    state {
        let! pos = state.Get()
        return! state.Put(fst pos, snd pos - 1)
    }
 
let moveRight () =
    state {
        let! pos = state.Get()
        return! state.Put(fst pos + 1, snd pos)
    }
 
let test pos1 pos2 = fst pos1 = fst pos2 || snd pos1 = snd pos2
 
let moveUpAndTest testPos =
    state {
        do! moveUp()
        let! pos = state.Get()
        return test pos testPos
    }
 
let run () =
    state {
        do! moveUp()
        let! res =  moveUpAndTest (5,4)
        if res
        then
            do! moveRight ()
        else
            do! moveUp()
    }
 
let result = match run () (5,5) with
             | Ok (_,pos) -> pos
             | Error _ -> (0,0)
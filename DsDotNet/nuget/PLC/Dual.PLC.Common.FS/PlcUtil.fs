namespace Dual.PLC.Common.FS

open System
open System.Runtime.CompilerServices

[<AutoOpen>]
module PlcUtil =
    let getDiffBitPositions(n1: UInt64, n2: UInt64) =
        let xor = n1 ^^^ n2
        [ for i in 0..63 do
            if (xor &&& (1UL <<< i)) <> 0UL then yield i ]

    [<Extension>]
    type ArrayExtension =
        [<Extension>]
        static member GetLWord(bs: byte[], lwOffset) = BitConverter.ToUInt64(bs, lwOffset * 8)

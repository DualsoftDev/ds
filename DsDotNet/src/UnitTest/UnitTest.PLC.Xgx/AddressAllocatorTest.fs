namespace T
open Dual.UnitTest.Common.FS

open NUnit.Framework

open PLC.CodeGen.Common
open Engine.Core
open Dual.Common.Core.FS

type AddressAllocatorTest(xgx:PlatformTarget) =
    inherit XgxTestBaseClass(xgx)

    member __.``Allocate address beyond limit test`` () =
        let {
            BitAllocator  = x
        } = MemoryAllocator.createMemoryAllocator "M" (0, 0) [] xgx

        match xgx with
        | XGI ->
            for i = 0 to 7 do
                x() === $"%%MX{i}"   // %MX0 ~ %MX7
        | XGK ->
            for i = 0 to 7 do
                x() === sprintf "M%05d" i   // M00000 ~ M00007

        | _ -> failwith "Not supported plc type"

        (fun () -> x() |> ignore) |> ShouldFailWithSubstringT "Limit exceeded."


        let {
            WordAllocator = w
        } = MemoryAllocator.createMemoryAllocator "M" (0, 0) [] xgx

        (fun () -> w() |> ignore) |> ShouldFailWithSubstringT "Limit exceeded."

    member __.``Allocate address`` () =
        let {
            BitAllocator  = x
            ByteAllocator = b
            WordAllocator = w
            DWordAllocator= d
            LWordAllocator= l
        } = MemoryAllocator.createMemoryAllocator "M" (0, 100) [] xgx

        match xgx with
        | XGI ->
            for i = 0 to 10 do
                x() === $"%%MX{i}"   // %MX0 ~ %MX10
            b() === "%MB2"
            x() === "%MX11"
            b() === "%MB3"
            w() === "%MW2"
            w() === "%MW3"
            x() === "%MX12"
            b() === "%MB8"
            w() === "%MW5"
            b() === "%MB9"
        | XGK ->
            for i = 0 to 10 do
                x() === xgkIOMBit ("M", i)   // M00000 ~ M0000A

            x() === "M0000B"
            x() === "M0000C"
            w() === "M0001"
            w() === "M0002"
            x() === "M0000D"
            x() === "M0000E"
            x() === "M0000F"
            x() === "M00030"

        | _ -> failwith "Not supported plc type"

    member __.``Allocate address test2`` () =
        let startWord, endWord = 20, 100  
        let {
            BitAllocator  = x
            ByteAllocator = b
            WordAllocator = w
            DWordAllocator= d
            LWordAllocator= l
        } = MemoryAllocator.createMemoryAllocator "R" (startWord, endWord) [] xgx

        match xgx with
        | XGI ->
            b() === "%RB20"
            b() === "%RB21"
            b() === "%RB22"
            w() === "%RW12"
            b() === "%RB23"
            x() === "%RX208" // 26 * 8
            b() === "%RB27"
            for i = 1 to 7 do
                x() === $"%%RX{208+i}"
            d() === "%RD7"
            b() === "%RB32"
        | XGK ->
            for i = 0 to 10 do
                x() === xgkIOMBit ("R", i+(startWord*8)) 

            x() === "R00010.B"
            x() === "R00010.C"
            w() === "R00011"


        | _ -> failwith "Not supported plc type"


type XgiAddressAllocatorTest() =
    inherit AddressAllocatorTest(XGI)
    [<Test>] member __.``Allocate address beyond limit test`` () = base.``Allocate address beyond limit test``()
    [<Test>] member __.``Allocate address`` () = base.``Allocate address``()
    [<Test>] member __.``Allocate address test2`` () = base.``Allocate address test2``()



type XgkAddressAllocatorTest() =
    inherit AddressAllocatorTest(XGK)
    [<Test>] member __.``Allocate address beyond limit test`` () = base.``Allocate address beyond limit test``()
    [<Test>] member __.``Allocate address`` () = base.``Allocate address``()
    [<Test>] member __.``Allocate address test2`` () = base.``Allocate address test2``()

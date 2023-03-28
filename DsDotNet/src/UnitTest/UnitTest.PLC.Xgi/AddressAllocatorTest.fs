namespace T

open NUnit.Framework

open PLC.CodeGen.Common

type AddressAllocatorTest() =
    inherit XgiTestBaseClass()

    [<Test>]
    member __.``Allocate address beyond limit test`` () =
        let {
            BitAllocator  = x
        } = MemoryAllocator.createMemoryAllocator "M" (0, 0) []

        for i = 0 to 7 do
            x() === $"%%MX{i}"   // %MX0 ~ %MX10
        (fun () -> x() |> ignore) |> ShouldFailWithSubstringT "Limit exceeded."


        let {
            WordAllocator = w
        } = MemoryAllocator.createMemoryAllocator "M" (0, 0) []

        (fun () -> w() |> ignore) |> ShouldFailWithSubstringT "Limit exceeded."

    [<Test>]
    member __.``Allocate address`` () =
        let {
            BitAllocator  = x
            ByteAllocator = b
            WordAllocator = w
            DWordAllocator= d
            LWordAllocator= l
        } = MemoryAllocator.createMemoryAllocator "M" (0, 100) []

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

    [<Test>]
    member __.``Allocate address test2`` () =
        let {
            BitAllocator  = x
            ByteAllocator = b
            WordAllocator = w
            DWordAllocator= d
            LWordAllocator= l
        } = MemoryAllocator.createMemoryAllocator "M" (20, 100) []

        b() === "%MB20"
        b() === "%MB21"
        b() === "%MB22"
        w() === "%MW12"
        b() === "%MB23"
        x() === "%MX208" // 26 * 8
        b() === "%MB27"
        for i = 1 to 7 do
            x() === $"%%MX{208+i}"
        d() === "%MD7"
        b() === "%MB32"

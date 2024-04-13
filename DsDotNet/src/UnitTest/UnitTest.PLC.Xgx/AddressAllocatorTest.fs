namespace T
open Dual.UnitTest.Common.FS

open NUnit.Framework

open PLC.CodeGen.Common
open Engine.Core
open Dual.Common.Core.FS

type AddressAllocatorTest(xgx:RuntimeTargetType) =
    inherit XgxTestBaseClass(xgx)

    member __.``Allocate address beyond limit test`` () =
        let {
            BitAllocator  = x
        } = MemoryAllocator.createMemoryAllocator "M" (0, 0) [] xgx

        for i = 0 to 7 do
            x() === $"%%MX{i}"   // %MX0 ~ %MX10
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
                x() === sprintf "M%05X" i   // M00000 ~ M0000A

            b() === "M0001"
            x() === "M0000B"
            b() === "M0003"
            //w() === "%MW2"
            //w() === "%MW3"
            //x() === "%MX12"
            //b() === "%MB8"
            //w() === "%MW5"
            //b() === "%MB9"

        | _ -> failwith "Not supported plc type"

    member __.``Allocate address test2`` () =
        let {
            BitAllocator  = x
            ByteAllocator = b
            WordAllocator = w
            DWordAllocator= d
            LWordAllocator= l
        } = MemoryAllocator.createMemoryAllocator "M" (20, 100) [] xgx

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

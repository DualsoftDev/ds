namespace T

open NUnit.Framework

open Engine.Parser.FS
open Engine.Core
open Engine.Common.FS
open PLC.CodeGen.LSXGI
open PLC.CodeGen.Common.NewIEC61131


type AddressAllocatorTest() =
    inherit XgiTestBaseClass()

    [<Test>]
    member __.``Allocate address`` () =
        let allocator = MemoryAllocator.createMemoryAllocator "M" (0, 100)
        for i = 0 to 10 do
            allocator Size.X === $"%%MX{i}"   // %MX0 ~ %MX10
        allocator Size.B === "%MB2"
        allocator Size.X === "%MX11"
        allocator Size.B === "%MB3"
        allocator Size.W === "%MW2"
        allocator Size.W === "%MW3"
        allocator Size.X === "%MX12"
        allocator Size.B === "%MB8"
        allocator Size.W === "%MW5"
        allocator Size.B === "%MB9"
        ()



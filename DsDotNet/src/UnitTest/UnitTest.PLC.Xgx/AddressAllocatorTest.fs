namespace T
open Dual.Common.UnitTest.FS

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
                x() === $"%%MX{i}" // |xxxxxxxx|xxx   // %MX0 ~ %MX10
                                   // b0       b1       b2        b3      b4      b5       b6        b7       b8       b9       b10       b11     b12       b13     b14       b15     b16      b17      b18       b19     b20       b21     b22       b23     b24       b25     b26       b27     b28       b29     b30       b31     b32       b33     b34       b35     b36       b37     b38       b39     b40       b41     b42       b43     b44       b45     b46       b47     b48       b49     b50       b51     b52       b53     b54       b55     b56       b57     b58       b59     b60       b61     b62       b63     b64       b65     b66       b67     b68       b69     b70       b71     b72       b73     b74       b75     b76       b77     b78       b79     b80       b81     b82       b83     b84       b85     b86       b87     b88       b89     b90       b91     b92       b93     b94       b95     b96       b97     b98       b99     b100
                                   // w0                w1                w2                w3                w4                w5                w6                w7                w8                w9                w10               w11               w12               w13               w14               w15               w16               w17               w18               w19               w20               w21               w22               w23               w24               w25               w26               w27               w28               w29               w30               w31               w32               w33               w34               w35               w36               w37               w38               w39               w40               w41               w42               w43               w44               w45               w46               w47               w48               w49               w50               w51               w52               w53               w54               w55               w56               w57               w58               w59               w60               w61               w62               w63               w64               w65               w66               w67               w68               w69               w70               w71               w72               w73               w74               w75               w76               w77               w78               w79               w80               w81               w82               w83               w84               w85               w86               w87               w88               w89               w90               w91               w92               w93               w94               w95               w96               w97               w98               w99               w100
                                   // D0                                  D1                                  D2                                  D3                                  D4                                  D5                                  D6                                  D7                                  D8                                  D9                                  D10
                                   // L0                                                                      L1                                                                      L2                                                                      L3                                                                      L4                                                                      L5                                                                      L6                                                                      L7                                                                      L8                                                                      L9                                                                      L10
            b() === "%MB2"         // |oooooooo|ooo-----|xxxxxxxx|
            x() === "%MX11"        // |oooooooo|ooox----|oooooooo|
            b() === "%MB3"         // |oooooooo|oooo----|oooooooo|xxxxxxxx|  
            w() === "%MW2"         // |oooooooo|oooo----|oooooooo|oooooooo|xxxxxxxx|xxxxxxxx|
            w() === "%MW3"         // |oooooooo|oooo----|oooooooo|oooooooo|oooooooo|oooooooo|xxxxxxxx|xxxxxxxx|
            x() === "%MX12"        // |oooooooo|oooox---|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|
            b() === "%MB8"         // |oooooooo|ooooo---|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|xxxxxxxx|
            w() === "%MW5"         // |oooooooo|ooooo---|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|--------|xxxxxxxx|xxxxxxxx|
            b() === "%MB9"         // |oooooooo|ooooo---|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|xxxxxxxx|oooooooo|oooooooo|

            d() === "%MD3"         // |oooooooo|ooooo---|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|xxxxxxxx|xxxxxxxx|xxxxxxxx|xxxxxxxx|
            l() === "%ML2"         // |oooooooo|ooooo---|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|xxxxxxxx|xxxxxxxx|xxxxxxxx|xxxxxxxx|xxxxxxxx|xxxxxxxx|xxxxxxxx|xxxxxxxx
            x() === "%MX13"        // |oooooooo|ooooox--|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo
            b() === "%MB24"        // |oooooooo|oooooo--|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|xxxxxxxx
            w() === "%MW13"        // |oooooooo|oooooo--|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|--------|xxxxxxxx|xxxxxxxx
            b() === "%MB25"        // |oooooooo|oooooo--|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|xxxxxxxx|oooooooo|oooooooo
            x() === "%MX14"        // |oooooooo|oooooox-|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo
            x() === "%MX15"        // |oooooooo|ooooooox|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo
            x() === "%MX224"       // |oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|x


            let { BitAllocator = x; ByteAllocator = b; WordAllocator = w; DWordAllocator= d; LWordAllocator= l } =
                MemoryAllocator.createMemoryAllocator "M" (0, 100) [] xgx

                                   // b0       b1       b2        b3      b4      b5       b6        b7       b8       b9       b10       b11     b12       b13     b14       b15     b16      b17      b18       b19     b20       b21     b22       b23     b24       b25     b26       b27     b28       b29     b30       b31     b32       b33     b34       b35     b36       b37     b38       b39     b40       b41     b42       b43     b44       b45     b46       b47     b48       b49     b50       b51     b52       b53     b54       b55     b56       b57     b58       b59     b60       b61     b62       b63     b64       b65     b66       b67     b68       b69     b70       b71     b72       b73     b74       b75     b76       b77     b78       b79     b80       b81     b82       b83     b84       b85     b86       b87     b88       b89     b90       b91     b92       b93     b94       b95     b96       b97     b98       b99     b100
                                   // w0                w1                w2                w3                w4                w5                w6                w7                w8                w9                w10               w11               w12               w13               w14               w15               w16               w17               w18               w19               w20               w21               w22               w23               w24               w25               w26               w27               w28               w29               w30               w31               w32               w33               w34               w35               w36               w37               w38               w39               w40               w41               w42               w43               w44               w45               w46               w47               w48               w49               w50               w51               w52               w53               w54               w55               w56               w57               w58               w59               w60               w61               w62               w63               w64               w65               w66               w67               w68               w69               w70               w71               w72               w73               w74               w75               w76               w77               w78               w79               w80               w81               w82               w83               w84               w85               w86               w87               w88               w89               w90               w91               w92               w93               w94               w95               w96               w97               w98               w99               w100
                                   // D0                                  D1                                  D2                                  D3                                  D4                                  D5                                  D6                                  D7                                  D8                                  D9                                  D10
                                   // L0                                                                      L1                                                                      L2                                                                      L3                                                                      L4                                                                      L5                                                                      L6                                                                      L7                                                                      L8                                                                      L9                                                                      L10
            x() === "%MX0"         // |x-------|
            b() === "%MB1"         // |o-------|xxxxxxxx|
            w() === "%MW1"         // |o-------|oooooooo|xxxxxxxx|xxxxxxxx|
            d() === "%MD1"         // |o-------|oooooooo|oooooooo|oooooooo|xxxxxxxx|xxxxxxxx|xxxxxxxx|xxxxxxxx|
            l() === "%ML1"         // |o-------|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|xxxxxxxx|xxxxxxxx|xxxxxxxx|xxxxxxxx|xxxxxxxx|xxxxxxxx|xxxxxxxx|xxxxxxxx|
            d() === "%MD4"         // |o-------|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|xxxxxxxx|xxxxxxxx|xxxxxxxx|xxxxxxxx|
            w() === "%MW10"        // |o-------|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|xxxxxxxx|xxxxxxxx|
            b() === "%MB22"        // |o-------|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|xxxxxxxx|
            x() === "%MX1"         // |ox------|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|

            (* Reserved 영역 1, 4 회피 생성 *)
            let { BitAllocator = x; ByteAllocator = b; WordAllocator = w; DWordAllocator= d; LWordAllocator= l } =
                MemoryAllocator.createMemoryAllocator "M" (0, 100) [1;4] xgx

                                   // b0       b1       b2        b3      b4      b5       b6        b7       b8       b9       b10       b11     b12       b13     b14       b15     b16      b17      b18       b19     b20       b21     b22       b23     b24       b25     b26       b27     b28       b29     b30       b31     b32       b33     b34       b35     b36       b37     b38       b39     b40       b41     b42       b43     b44       b45     b46       b47     b48       b49     b50       b51     b52       b53     b54       b55     b56       b57     b58       b59     b60       b61     b62       b63     b64       b65     b66       b67     b68       b69     b70       b71     b72       b73     b74       b75     b76       b77     b78       b79     b80       b81     b82       b83     b84       b85     b86       b87     b88       b89     b90       b91     b92       b93     b94       b95     b96       b97     b98       b99     b100
                                   // w0                w1                w2               w3                w4                w5                w6                w7                w8                w9                w10               w11               w12               w13               w14               w15               w16               w17               w18               w19               w20               w21               w22               w23               w24               w25               w26               w27               w28               w29               w30               w31               w32               w33               w34               w35               w36               w37               w38               w39               w40               w41               w42               w43               w44               w45               w46               w47               w48               w49               w50               w51               w52               w53               w54               w55               w56               w57               w58               w59               w60               w61               w62               w63               w64               w65               w66               w67               w68               w69               w70               w71               w72               w73               w74               w75               w76               w77               w78               w79               w80               w81               w82               w83               w84               w85               w86               w87               w88               w89               w90               w91               w92               w93               w94               w95               w96               w97               w98               w99               w100
                                   // D0                                  D1                                  D2                                  D3                                  D4                                  D5                                  D6                                  D7                                  D8                                  D9                                  D10
                                   // L0                                                                      L1                                                                      L2                                                                      L3                                                                      L4                                                                      L5                                                                      L6                                                                      L7                                                                      L8                                                                      L9                                                                      L10
            x() === "%MX0"         // |x-------|RRRRRRRR|--------|--------|RRRRRRRR|
            b() === "%MB2"         // |o-------|RRRRRRRR|xxxxxxxx|--------|RRRRRRRR|
            w() === "%MW3"         // |o-------|RRRRRRRR|oooooooo|--------|RRRRRRRR|--------|xxxxxxxx|xxxxxxxx|
            d() === "%MD2"         // |o-------|RRRRRRRR|oooooooo|oooooooo|RRRRRRRR|oooooooo|oooooooo|oooooooo|xxxxxxxx|xxxxxxxx|xxxxxxxx|xxxxxxxx|
            l() === "%ML2"         // |o-------|RRRRRRRR|oooooooo|oooooooo|RRRRRRRR|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|--------|--------|--------|--------|xxxxxxxx|xxxxxxxx|xxxxxxxx|xxxxxxxx|xxxxxxxx|xxxxxxxx|xxxxxxxx|xxxxxxxx|
            d() === "%MD3"         // |o-------|RRRRRRRR|oooooooo|oooooooo|RRRRRRRR|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|xxxxxxxx|xxxxxxxx|xxxxxxxx|xxxxxxxx|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|
            w() === "%MW12"        // |o-------|RRRRRRRR|oooooooo|oooooooo|RRRRRRRR|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|xxxxxxxx|xxxxxxxx|
            b() === "%MB26"        // |o-------|RRRRRRRR|oooooooo|oooooooo|RRRRRRRR|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|xxxxxxxx|
            x() === "%MX1"         // |ox------|RRRRRRRR|oooooooo|oooooooo|RRRRRRRR|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|oooooooo|


        | XGK ->
            for i = 0 to 10 do
                x() === getXgkBitText ("M", i)   // M00000 ~ M0000A

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
                x() === getXgkBitText ("R", i+(startWord*8)) 

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

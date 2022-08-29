
//#r "System.Xml.dll"
//#r "System.Xml.ReaderWriter.dll"
//#r "System.Xml.Linq.dll"

#I @"..\..\bin\netcoreapp3.1"
#r "Dual.Common.FS.dll"
#r "Dual.Core.FS.dll"
#r "nuget: FSharpPlus" 

open System.Xml
open FSharpPlus
open Dual.Core
open Dual.Common
open Dual.Core.Prelude
open IEC61131
open Dual.Core.Prelude.NewIEC61131


module Test =
    let (===) a b = assert(a = b)
    let hwconf = HwStorageConfig3(5, 2, 16)
    hwconf.FileBitLength === 2 * 16
    hwconf.GetElementBitLength() === 16

    hwconf.GetBitOffset(0, 0, 0) === 0
    hwconf.GetBitOffset(0, 0, 1) === 1
    hwconf.GetBitOffset(0, 0, 7) === 7
    hwconf.GetBitOffset(0, 0, 15) === 15
    hwconf.GetBitOffset(0, 0, 16) === 16
    hwconf.GetBitOffset(0, 0, 17) === 17  // 범위 넘어서는 것 허용
    hwconf.GetBitOffset(0, 1, 0) === 16
    hwconf.GetBitOffset(0, 1, 1) === 17
    hwconf.GetBitOffset(4, 1, 3) === 4*(2*16) + 1*16 + 3  // 147


    let a0 = getTagIndices "%IX0.0.0"
    let a1 = getTagIndices "%IX0.0.1"
    let a2 = getTagIndices "%IX0.0.17"
    let w3 = getTagIndices "%IW1.0"
    let w4 = getTagIndices "%IW2"
    let a4 = getTagIndices "%IX0.0.0"
    let a5 = getTagIndices "%IX0.0.0"

    getTagIndices "%IX0.1.0" |> getBitOffset hwconf === 16
    getTagIndices "%IX1.0.0" |> getBitOffset hwconf === 32
    getTagIndices "%IX1.0.1" |> getBitOffset hwconf === 33
    getTagIndices "%IX1.0.17" |> getBitOffset hwconf === 49
    getTagIndices "%IX1.1.1" |> getBitOffset hwconf === 49


    getTagIndices "%IW0" |> getBitStartOffset hwconf === 0
    getTagIndices "%IW1" |> getBitStartOffset hwconf === 16
    getTagIndices "%IW1.0" |> getBitStartOffset hwconf === 32
    getTagIndices "%IW1.1" |> getBitStartOffset hwconf === 48
    getTagIndices "%IB1" |> getBitStartOffset hwconf === 8
    getTagIndices "%IB2" |> getBitStartOffset hwconf === 16

    getTagIndices "%IB0" |> getBitOffsets hwconf === [0..7]
    getTagIndices "%IB1" |> getBitOffsets hwconf === [8..15]
    getTagIndices "%IB2" |> getBitOffsets hwconf === [16..23]
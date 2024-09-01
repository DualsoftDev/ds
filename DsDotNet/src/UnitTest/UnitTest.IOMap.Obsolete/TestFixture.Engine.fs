namespace T.IOMap

open NUnit.Framework
open Dual.Common.UnitTest.FS

open System.Collections.Generic
open IOMapApi.MemoryIOApi
open FsUnit.Xunit
open T
open IOMapApi.MemoryIOManagerImpl
open Dual.Common.Core.FS
open IOMapApi
open System.IO.MemoryMappedFiles
open System
open IOMapForModeler

[<AutoOpen>]
module Fixtures =
    [<AbstractClass>]
    type MapIOTestBaseClass(advance:bool) =
        inherit TestBaseClass("IOMapLogger")
        let maps = Dictionary<string*int, MemoryIO>()
        let filesMap = Dictionary<string, MemoryMappedFile>()
        let files = 
            if advance then
                [
                    "PAIX\NMC2\I", 64
                    "PAIX\NMC2\O", 64     
                    "PAIX\NMF\I", 128
                    "PAIX\NMF\O", 128
                 
                ]
            else
                [
                    "UnitTest\A",512
                    "UnitTest\B",128
                    "UnitTest\C",256
                    "UnitTest\D",1024
                    //"UnitTest\E",int max
                ]
        do


            HwServiceManagerImpl.IOMapServiceDelete()      



        member x.MAPS =  maps
        
        member x.CreateMap() = 
            files 
            |> Seq.iter(fun (name, size)->
                    let name = @$"{name}"
                    if MemoryIOManager.Delete(name) then
                        tracefn $"MemoryIOManager Delete {name}"

                    if MemoryIOManager.Create(name, size) then
                        tracefn $"MemoryIOManager Create {name} size({size})"
                )

        member x.OpenMap() = 
            files 
            |> Seq.iter(fun (name, size)->
                    let name = @$"{name}"
                    filesMap.Add(name, MemoryIOManager.Load (name))
                    maps.Add((name, size), MemoryIO(name)) |>ignore
                )

        member x.CloseMap() = 
            maps |> Seq.iter(fun dic -> dic.Value.Dispose())
            maps.Clear()
            
            filesMap |> Seq.iter(fun dic -> dic.Value.Dispose())
            filesMap.Clear()

        // Common setup for each test can be done here.
        [<OneTimeSetUp>]
        member x.SetUp() = 
        // Setup code...
               //IOMapApiTest 는 관리자 권한 경로거나 windows Service실행이 아니기 때문에 
            MemoryUtilImpl.TestMode <- true
            x.CreateMap() 
            x.OpenMap()
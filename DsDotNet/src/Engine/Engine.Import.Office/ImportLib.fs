// Copyright (c) Dual Inc.  All Rights Reserved.
namespace Engine.Import.Office

open Engine.Core
open System.Runtime.CompilerServices
open Engine.Parser.FS
open Dual.Common.Core.FS

[<AutoOpen>]
module ImportLib =


    [<Extension>]
    type ImportPPTWithLib =

        [<Extension>]
        static member GetDSFromPPTWithLib(fullName: string) =
            pptRepo.Clear()

            do
                // library 파일을 먼저 로딩해서 DS 파일로 변환한다.

                let textLibPptx = $"{TextLibrary}.pptx" |> DsFile
                // 파일 loadingfromPPTs 시 DS_Library.ds 만드는 용도
                let libSys =
                    let libFilePath =
                        let runDir: DsPath =
                            System.Reflection.Assembly.GetEntryAssembly().Location |> getFolderOfPath

                        PathManager.getFullPath textLibPptx runDir

                    let libModel: Model = loadingfromPPTs  libFilePath  |> Tuple.first
                    libModel.System

                let loadedlibFilePath =
                    PathManager.getFullPath textLibPptx (PathManager.getFolderOfPath (fullName))

                libSys.pptxToExportDS loadedlibFilePath |> ignore



            let exportPath =
                let sys =
                    let model: Model = loadingfromPPTs  fullName  |> Tuple.first
                    model.System

                sys.pptxToExportDS fullName

            let system, loadingPaths = ParserLoader.LoadFromActivePath exportPath

            { System = system
              ActivePaths = [ exportPath ]
              LoadingPaths = loadingPaths }

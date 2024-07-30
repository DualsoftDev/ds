// Copyright (c) Dualsoft  All Rights Reserved.
namespace Engine.Import.Office

open System.Linq
open System.Collections.Generic
open Dual.Common.Core.FS
open Engine.Import.Office
open Engine.Core
open System.IO
open System.Runtime.CompilerServices
open DocumentFormat.OpenXml.Packaging
open System
open PathManager
open Engine.Parser.FS
open System.Text.RegularExpressions
open System.Diagnostics

[<AutoOpen>]
module ImportPPTModule =
    type DSFromPPT =
        { System: DsSystem 
          ActivePath: string 
          LoadingPaths: string seq
          LayoutImgPaths: string seq
           }


    
    type PPTParams = {
        TargetType: PlatformTarget
        AutoIOM: bool
        CreateFromPPT : bool
        CreateBtnLamp : bool
    }

    let dicPptDoc = Dictionary<string, PresentationDocument>()
    let pathStack = Stack<string>() //파일 오픈시 예외 로그 path PPT Stack
    let loadedParentStack = Stack<DsSystem>() //LoadedSystem.AbsoluteParents 구성하여 ExternalSystem 구분 및 UI Tree 구조 구성
    let dicLoaded = Dictionary<LoadedSystem, string>() // LoadedSystem 부모, 형제 호출시 Runtime에 변경하기 위한 정보 사전
    let layoutImgPaths = HashSet<string>() //LayoutImgPaths 저장
    
    let getHashKeys (skipCnt: int, path: string) =
        String.Join(
            ",",
            loadedParentStack
                .ToHashSet()
                .Skip(skipCnt)
                .Reverse()
                .Select(fun s -> s.GetHashCode().ToString())
                .Append(path)
        )

    let pathPPTWithEnvironmentVariablePaths loadFilePath parasAbsoluteFilePath : string =
        let loadFile = loadFilePath |> DsFile
        let baseDirectory = parasAbsoluteFilePath |> DsFile |> PathManager.getDirectoryName

        let environmentuserPaths = collectEnvironmentVariablePaths ()

        let fullPathSelector path =
            PathManager.getFullPath loadFile (path |> DsDirectory)

        let allPaths = [ baseDirectory ] @ environmentuserPaths
        allPaths |> List.map fullPathSelector |> fileExistFirstSelector



    module PowerPointImportor =
        let private sRepo = ShareableSystemRepository()


        let rec private loadSystem (pptReop: Dictionary<DsSystem, pptDoc>, theSys: DsSystem, paras: DeviceLoadParameters, isLib, pptParams:PPTParams) =
            pathStack.Push(paras.AbsoluteFilePath)
            LoadingPPTNotify.Trigger(paras.AbsoluteFilePath)
            
            currentFileName <- pathStack.Peek()

            let pathPPT = paras.AbsoluteFilePath

            let doc =
                match dicPptDoc.TryGetValue pathPPT with
                | true, existingDoc -> pptDoc (pathPPT, paras |> Some, existingDoc, pptParams.TargetType)
                | false, _ ->
                    let newDoc = Office.Open(pathPPT)
                    dicPptDoc.Add(pathPPT, newDoc)
                    pptDoc (pathPPT, paras |> Some, newDoc, pptParams.TargetType)





            //시스템 로딩시 중복이름을 부를 수 없다.
            CheckSameCopy(doc)
            SameFlowName(doc)


            doc.GetCopyPathNName()
            |> Seq.iter (fun (loadFilePath, loadedName, node) ->
                currentFileName <- pathStack.Peek()

                let pathPPT =
                    try
                        pathPPTWithEnvironmentVariablePaths loadFilePath paras.AbsoluteFilePath
                    with ex ->
                        Office.ErrorPPT(ErrorCase.Path, ErrID._29, node.Name, node.PageNum, node.Id)

                let loadRelativePath =
                    PathManager.getRelativePath (paras.AbsoluteFilePath.ToFile()) (pathPPT.ToFile())

                let loadAbsolutePath = pathPPT

                let paras =
                    getParams (
                        loadAbsolutePath,
                        loadRelativePath,
                        loadedName,
                        theSys,
                        node.NodeType.GetLoadingType(),
                        sRepo
                    )


                let addNewLoadedSys (newSys: DsSystem, bExtSys: bool, bOPEN_EXSYS_LINK: bool) =

                    loadedParentStack.Push(newSys)
                    loadSystem (pptReop, newSys, paras, isLib, pptParams) |> ignore
                    currentFileName <- pathStack.Peek()

                    let parents = loadedParentStack.ToHashSet().Skip(1).Reverse() //자신 제외

                    let newLoad =
                        if bExtSys then
                            let skipCnt = if bOPEN_EXSYS_LINK then 2 else 1 //LINK 이면 형제 관계
                            let exSys = ExternalSystem(newSys, paras, false)

                            let key = getHashKeys (skipCnt, paras.AbsoluteFilePath)
                            sRepo.Add(key, exSys.ReferenceSystem)
                            exSys :> LoadedSystem
                        else
                            Device(newSys, paras, false) :> LoadedSystem


                    loadedParentStack.Pop() |> ignore

                    newLoad

                let addNewExtSysLoaded (skipCnt) =
                    let key = getHashKeys (skipCnt, paras.AbsoluteFilePath)

                    let exSys =
                        if sRepo.ContainsKey(key) then
                            ExternalSystem(sRepo[key], paras, false) :> LoadedSystem
                        else
                            let newSys = DsSystem.Create(paras.LoadedName)
                            addNewLoadedSys (newSys, true, node.NodeType = OPEN_EXSYS_LINK)

                    theSys.AddLoadedSystem(exSys)
                    dicLoaded.Add(exSys, paras.LoadedName)

                match node.NodeType with
                | OPEN_EXSYS_CALL -> addNewExtSysLoaded 0
                | OPEN_EXSYS_LINK -> addNewExtSysLoaded 1 //LINK 이면 형제 관계
                | COPY_DEV ->
                    let device = addNewLoadedSys (DsSystem.Create(paras.LoadedName), false, false)
                    theSys.AddLoadedSystem(device)

                | _ -> failwithlog "error")

            theSys.ExternalSystems.Iter(fun f -> f.LoadedName <- dicLoaded[f])

            //ExternalSystem 위하여 인터페이스는 공통으로 시스템 생성시 만들어 줌
            doc.MakeInterfaces(theSys)

            if paras.LoadingType = DuNone || paras.LoadingType = DuDevice then //External system 은 Interfaces만 만들고 나중에 buildSystem 수행
                doc.BuildSystem(theSys, isLib, pptParams.CreateBtnLamp)

            if paras.LoadingType = DuNone then
                doc.UpdateActionIO(theSys, pptParams.AutoIOM) 
                doc.UpdateLayouts(theSys)
                layoutImgPaths.AddRange(doc.SaveSlideImage())|>ignore
            
            pathStack.Pop() |> ignore
            pptReop.Add(theSys, doc)
            theSys, doc

        let internal GetImportModel(pptReop: Dictionary<DsSystem, pptDoc>, filePath: string, isLib, pptParams) =
            //active는 시스템이름으로 ppt 파일 이름을 사용
            let fileDirectory = PathManager.getDirectoryName (filePath|>DsFile) 
            activeSysDir <- fileDirectory
            currentFileName <- filePath
            let sysName = getSystemName filePath
            let mySys = DsSystem.Create(sysName)

            if mySys.Name <> TextLibrary
            then  activeSys <- Some mySys

            let paras =
                getParams (filePath, sysName + ".pptx", mySys.Name, mySys, DuNone, sRepo)

            dicLoaded.Clear()
            loadedParentStack.Clear()
            layoutImgPaths.Clear()

            loadedParentStack.Push mySys
            loadSystem (pptReop, mySys, paras, isLib, pptParams)

    let pptRepo = Dictionary<DsSystem, pptDoc>()

    let private loadingfromPPTs (path: string ) isLib (pptParams:PPTParams)=
        Copylibrary.Clear()
        try
            try
                let cfg = { DsFilePath = path
                            HWIP =  RuntimeDS.IP
                    }

                let sys, doc = PowerPointImportor.GetImportModel(pptRepo, path, isLib, pptParams)

                //ExternalSystem 순환참조때문에 완성못한 시스템 BuildSystem 마무리하기
                pptRepo
                    .Where(fun dic -> not <| dic.Value.IsBuilded)
                    .ForEach(fun dic ->
                        let dsSystem = dic.Key
                        let pptDoc = dic.Value
                        pathStack.Push(pptDoc.Path)
                        pptDoc.BuildSystem(dsSystem, isLib, pptParams.CreateBtnLamp)
                        pathStack.Pop() |> ignore)

                { Config = cfg
                  System = sys
                  LoadingPaths = [] },
                pptRepo

            with ex ->
                let errFileName =
                    if pathStack.any () then
                        pathStack.First()
                    else
                        path

                let fileErr= "File contains corrupted data."
                let msg = if ex.Message.Contains(fileErr)
                          then ex.Message.Replace(fileErr, "문서보안 복호화가 필요하거나 파일문제가 있습니다.")
                          else ex.Message

                if not (msg.EndsWith(ErrorNotify)) then
                    ErrorPPTNotify.Trigger(errFileName, 0, 0u, "")

                //첫페이지 아니면 stack에 존재
                failwithf $"{msg} \t◆파일명 {errFileName}"
        finally
            dicPptDoc.Where(fun f -> f.Value.IsNonNull()).Iter(fun f -> f.Value.Dispose())
            dicPptDoc.Clear()



    type PptResult =
        { System: DsSystem
          Views: ViewNode seq
         }


    [<Extension>]
    type ImportPPT =
        
    
        [<Extension>]
        static member GetDSFromPPTWithLib(fullName: string, isLib:bool, pptParams:PPTParams) =
            Util.runtimeTarget <- pptParams.TargetType
            ModelParser.ClearDicParsingText()
            pptRepo.Clear()

#if DEBUG
            let stopwatch = Stopwatch()
            stopwatch.Start()
#endif

           
            let model = loadingfromPPTs  fullName isLib pptParams |> Tuple.first

            
#if DEBUG
            stopwatch.Stop()
            stopwatch.ElapsedMilliseconds |> printfn "Elapsed time: %d ms"
#endif

            let activePath = PathManager.changeExtension (fullName.ToFile()) ".ds" 

            let system, loadingPaths =
                if pptParams.CreateFromPPT 
                then 
                     model.System, model.LoadingPaths
                else 
                     ParserLoader.LoadFromActivePath (model.System.pptxToExportDS activePath)  Util.runtimeTarget false

            { 
                System = system
                ActivePath = activePath
                LoadingPaths = loadingPaths 
                LayoutImgPaths = layoutImgPaths 
            }


        [<Extension>]
        static member GetRuntimeZipFromPPT(fullName: string, pptParams)=
            let ret = ImportPPT.GetDSFromPPTWithLib(fullName, false, pptParams)
            ModelLoaderExt.saveModelZip(ret.LoadingPaths , ret.ActivePath, ret.LayoutImgPaths), ret.System


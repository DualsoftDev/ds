// Copyright (c) Dual Inc.  All Rights Reserved.
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

[<AutoOpen>]
module ImportPPTModule =
    type DSFromPPT =
        { Systems: DsSystem seq
          ActivePaths: string seq
          LoadingPaths: string seq }

    let dicPptDoc = Dictionary<string, PresentationDocument>()
    let pathStack = Stack<string>() //파일 오픈시 예외 로그 path PPT Stack
    let loadedParentStack = Stack<DsSystem>() //LoadedSystem.AbsoluteParents 구성하여 ExternalSystem 구분 및 UI Tree 구조 구성
    let dicLoaded = Dictionary<LoadedSystem, string>() // LoadedSystem 부모, 형제 호출시 Runtime에 변경하기 위한 정보 사전

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



    type ImportPowerPoint() =
        let sRepo = ShareableSystemRepository()


        let rec loadSystem (pptReop: Dictionary<DsSystem, pptDoc>, theSys: DsSystem, paras: DeviceLoadParameters) =
            pathStack.Push(paras.AbsoluteFilePath)
            currentFileName <- pathStack.Peek()

            let pathPPT = paras.AbsoluteFilePath

            let doc =
                match dicPptDoc.TryGetValue pathPPT with
                | true, existingDoc -> pptDoc (pathPPT, paras |> Some, existingDoc)
                | false, _ ->
                    let newDoc = Office.Open(pathPPT)
                    dicPptDoc.Add(pathPPT, newDoc)
                    pptDoc (pathPPT, paras |> Some, newDoc)





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
                        None,
                        node.NodeType.GetLoadingType(),
                        sRepo
                    )

                let hostIp = if paras.HostIp.IsSome then paras.HostIp.Value else ""

                let addNewLoadedSys (newSys: DsSystem, bExtSys: bool, bOPEN_EXSYS_LINK: bool) =

                    loadedParentStack.Push(newSys)
                    loadSystem (pptReop, newSys, paras) |> ignore
                    currentFileName <- pathStack.Peek()

                    let parents = loadedParentStack.ToHashSet().Skip(1).Reverse() //자신 제외

                    let newLoad =
                        if bExtSys then
                            let skipCnt = if bOPEN_EXSYS_LINK then 2 else 1 //LINK 이면 형제 관계
                            let exSys = ExternalSystem(newSys, paras)

                            let key = getHashKeys (skipCnt, paras.AbsoluteFilePath)
                            sRepo.Add(key, exSys.ReferenceSystem)
                            exSys :> LoadedSystem
                        else
                            Device(newSys, paras) :> LoadedSystem


                    loadedParentStack.Pop() |> ignore

                    newLoad

                let addNewExtSysLoaded (skipCnt) =
                    let key = getHashKeys (skipCnt, paras.AbsoluteFilePath)

                    let exSys =
                        if sRepo.ContainsKey(key) then
                            ExternalSystem(sRepo[key], paras) :> LoadedSystem
                        else
                            let newSys = DsSystem(paras.LoadedName, hostIp)
                            //let newSys = DsSystem(paras.LoadedName, "localhost")  ///test ahn :  ExternalSystem parser에서"ip" 없어도 열때 까지 임시로 "localhost"
                            addNewLoadedSys (newSys, true, node.NodeType = OPEN_EXSYS_LINK)

                    theSys.AddLoadedSystem(exSys)
                    dicLoaded.Add(exSys, paras.LoadedName)

                match node.NodeType with
                | OPEN_EXSYS_CALL -> addNewExtSysLoaded 0
                | OPEN_EXSYS_LINK -> addNewExtSysLoaded 1 //LINK 이면 형제 관계
                | COPY_DEV ->
                    let device = addNewLoadedSys (DsSystem(paras.LoadedName, hostIp), false, false)
                    theSys.AddLoadedSystem(device)

                | _ -> failwithlog "error")

            theSys.ExternalSystems.Iter(fun f -> f.LoadedName <- dicLoaded[f])

            //ExternalSystem 위하여 인터페이스는 공통으로 시스템 생성시 만들어 줌
            doc.MakeInterfaces(theSys)

            if paras.LoadingType = DuNone || paras.LoadingType = DuDevice then //External system 은 Interfaces만 만들고 나중에 buildSystem 수행
                doc.BuildSystem(theSys)

            if paras.LoadingType = DuNone then //active system 은 Update IO
                doc.UpdateActionIO(theSys)


            pathStack.Pop() |> ignore
            pptReop.Add(theSys, doc)
            theSys, doc

        member internal x.GetImportModel(pptReop: Dictionary<DsSystem, pptDoc>, filePath: string) =
            //active는 시스템이름으로 ppt 파일 이름을 사용
            let fileName = PathManager.getFileName (filePath.ToFile())
            let fileDirectory = PathManager.getDirectoryName (filePath.ToFile())
            activeSysDir <- fileDirectory
            currentFileName <- fileName
            let sysName = getSystemName fileName
            let mySys = DsSystem(sysName, "localhost")

            let paras =
                getParams (filePath, sysName + ".pptx", mySys.Name, mySys, Some mySys.HostIp, DuNone, sRepo)

            dicLoaded.Clear()
            loadedParentStack.Clear()
            loadedParentStack.Push mySys
            loadSystem (pptReop, mySys, paras)

    let pptRepo = Dictionary<DsSystem, pptDoc>()

    let loadingfromPPTs (paths: string seq) =
        try
            try
                let cfg = { DsFilePaths = paths |> Seq.toList }

                let results =
                    [ for dsFile in cfg.DsFilePaths do
                          let ppt = new ImportPowerPoint()
                          ppt.GetImportModel(pptRepo, dsFile) ]

                //ExternalSystem 순환참조때문에 완성못한 시스템 BuildSystem 마무리하기
                pptRepo
                    .Where(fun dic -> not <| dic.Value.IsBuilded)
                    .ForEach(fun dic ->
                        let dsSystem = dic.Key
                        let pptDoc = dic.Value
                        pathStack.Push(pptDoc.Path)
                        pptDoc.BuildSystem(dsSystem)
                        pathStack.Pop() |> ignore)


                let systems = results |> map fst
                let views = results |> dict

                { Config = cfg
                  Systems = systems
                  LoadingPaths = [] },
                views,
                pptRepo

            with ex ->
                let errFileName =
                    if pathStack.any () then
                        pathStack.First()
                    else
                        paths.First()

                if not (ex.Message.EndsWith(ErrorNotify)) then
                    ErrorPPTNotify.Trigger(errFileName, 0, 0u, "")

                //첫페이지 아니면 stack에 존재
                failwithf $"{ex.Message} \t◆파일명 {errFileName}"
        finally
            dicPptDoc.Where(fun f -> f.Value.IsNonNull()).Iter(fun f -> f.Value.Close())
            dicPptDoc.Clear()



    type PptResult =
        { System: DsSystem
          Views: ViewNode seq
          IsActive: bool }


    [<Extension>]
    type ImportPPT =

        [<Extension>]
        static member GetModel(paths: string seq) =
            pptRepo.Clear()
            loadingfromPPTs (paths) |> fun (model, views, pptRepo) -> model

        [<Extension>]
        static member GetLoadingAllSystem(paths: string seq) =
            pptRepo.Clear()

            loadingfromPPTs (paths)
            |> fun (model, views, pptRepo) ->
                model,
                pptRepo
                |> Seq.map (fun f ->
                    { System = f.Key
                      Views = f.Key.GetViewNodes()
                      IsActive = model.Systems.Contains(f.Key) })

// Copyright (c) Dualsoft  All Rights Reserved.
namespace Engine.Import.Office

open System.Linq
open System.Collections.Generic
open Dual.Common.Core.FS
open Engine.Import.Office
open Engine.Core
open DocumentFormat.OpenXml.Packaging
open System
open Engine.Parser.FS
open Engine.CodeGenCPU

[<AutoOpen>]
module ImportPptModule =
    type DSFromPpt = {
        System: DsSystem
        ActivePath: string
        LoadingPaths: string seq
        LayoutImgPaths: string seq
    }

    type PptParams = {
        HwTarget: HwTarget
        UserTagConfig: UserTagConfig
        AutoIOM: bool
        CreateFromPpt : bool
        CreateBtnLamp : bool
        StartMemory : int
        OpMemory: int
    }

    let defaultPptParams() =
        {
            HwTarget = getDefaltHwTarget()
            UserTagConfig = {UserTags = [||]}
            AutoIOM = true
            CreateFromPpt = false
            CreateBtnLamp = true
            StartMemory = 1000
            OpMemory = 100
        }


    let getHashKeys (skipCnt: int, path: string, loadedParentStack:Stack<DsSystem>) =
        String.Join(
            ",",
            (HashSet loadedParentStack)
                .Skip(skipCnt)
                .Reverse()
                .Select(fun s -> s.GetHashCode().ToString())
                .Append(path)
        )

    let pathPptWithEnvironmentVariablePaths loadFilePath parasAbsoluteFilePath : string =
        let loadFile = DsFile loadFilePath
        let baseDirectory = DsFile parasAbsoluteFilePath |> PathManager.getDirectoryName

        let environmentuserPaths = collectEnvironmentVariablePaths ()

        let fullPathSelector path =
            PathManager.getFullPath loadFile (DsDirectory path)

        let allPaths = baseDirectory::environmentuserPaths
        allPaths |> List.map fullPathSelector |> fileExistFirstSelector



    module PowerPointImportor =
        let private sRepo = ShareableSystemRepository()

        /// GetDSFromPptWithLib > (loadFromPpts > GetImportModel >) loadSystem
        let rec private loadSystem
          (
            pptReop: Dictionary<DsSystem, PptDoc>,
            theSys: DsSystem,
            paras: DeviceLoadParameters,
            isLib,
            pptParams:PptParams,
            dicLoaded:Dictionary<LoadedSystem, string>,
            dicPptDoc:Dictionary<string, PresentationDocument>,
            pathStack:Stack<string>,
            loadedParentStack:Stack<DsSystem>,
            layoutImgPaths:HashSet<string>
          ) =
            pathStack.Push(paras.AbsoluteFilePath)
            LoadingPptNotify.Trigger(paras.AbsoluteFilePath)

            currentFileName <- pathStack.Peek()

            let pathPpt = paras.AbsoluteFilePath

            let doc =
                match dicPptDoc.TryGetValue pathPpt with
                | true, existingDoc -> PptDoc.Create (pathPpt, Some paras, existingDoc, pptParams.HwTarget.Platform)
                | false, _ ->
                    let newDoc = Office.Open(pathPpt)
                    dicPptDoc.Add(pathPpt, newDoc)
                    PptDoc.Create (pathPpt, paras |> Some, newDoc, pptParams.HwTarget.Platform)





            //시스템 로딩시 중복이름을 부를 수 없다.
            CheckSameCopy(doc)
            //SameFlowName(doc)  //중복 페이지는 같은 페이지로 해석


            doc.GetCopyPathNName()
            |> Seq.iter (fun (loadFilePath, loadedName, node) ->
                currentFileName <- pathStack.Peek()

                let pathPpt =
                    try
                        pathPptWithEnvironmentVariablePaths loadFilePath paras.AbsoluteFilePath
                    with ex ->
                        Office.ErrorPpt(ErrorCase.Path, ErrID._29, node.Name, node.PageNum, node.Id)

                let loadRelativePath =
                    PathManager.getRelativePath (paras.AbsoluteFilePath.ToFile()) (pathPpt.ToFile())

                let loadAbsolutePath = pathPpt

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
                    loadSystem (pptReop, newSys, paras, isLib, pptParams, dicLoaded, dicPptDoc, pathStack, loadedParentStack, layoutImgPaths) |> ignore
                    currentFileName <- pathStack.Peek()

                    let parents = HashSet(loadedParentStack).Skip(1).Reverse() //자신 제외

                    let newLoad =
                        if bExtSys then
                            let skipCnt = if bOPEN_EXSYS_LINK then 2 else 1 //LINK 이면 형제 관계
                            let exSys = ExternalSystem(newSys, paras, false)

                            let key = getHashKeys (skipCnt, paras.AbsoluteFilePath, loadedParentStack)
                            sRepo.Add(key, exSys.ReferenceSystem)
                            exSys :> LoadedSystem
                        else
                            Device(newSys, paras, false) :> LoadedSystem


                    loadedParentStack.Pop() |> ignore

                    newLoad

                let addNewExtSysLoaded (skipCnt) =
                    let key = getHashKeys (skipCnt, paras.AbsoluteFilePath, loadedParentStack)

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

            theSys.ExternalSystems.Iter(fun exSys -> exSys.LoadedName <- dicLoaded[exSys])

            //ExternalSystem 위하여 인터페이스는 공통으로 시스템 생성시 만들어 줌
            doc.MakeInterfaces(theSys)

            if paras.LoadingType = DuNone || paras.LoadingType = DuDevice then //External system 은 Interfaces만 만들고 나중에 buildSystem 수행
                doc.BuildSystem(theSys, pptParams.HwTarget, isLib, pptParams.CreateBtnLamp)

            if paras.LoadingType = DuNone then
                doc.UpdateActionIO(theSys, pptParams.AutoIOM, pptParams.HwTarget)
                doc.UpdateLayouts(theSys)
                layoutImgPaths.AddRange(doc.SaveSlideImage())|>ignore

            pathStack.Pop() |> ignore
            pptReop.Add(theSys, doc)
            theSys, doc

        /// GetDSFromPptWithLib > (loadFromPpts > GetImportModel >) loadSystem
        let internal GetImportModel
          (
            pptReop: Dictionary<DsSystem, PptDoc>,
            filePath: string,
            isLib,
            pptParams,
            dicPptDoc:Dictionary<string, PresentationDocument>,
            pathStack:Stack<string>,
            layoutImgPaths:HashSet<string>
          ) : DsSystem * PptDoc =
            let loadedParentStack = Stack<DsSystem>() //LoadedSystem.AbsoluteParents 구성하여 ExternalSystem 구분 및 UI Tree 구조 구성
            //active는 시스템이름으로 ppt 파일 이름을 사용
            let fileDirectory = PathManager.getDirectoryName (filePath|>DsFile)
            activeSysDir <- fileDirectory
            currentFileName <- filePath
            let sysName = getSystemName filePath
            let mySys = DsSystem.Create(sysName)

            if mySys.Name <> TextLibrary then
                activeSys <- Some mySys

            let paras =
                getParams (filePath, sysName + ".pptx", mySys.Name, mySys, DuNone, sRepo)

            let dicLoaded = Dictionary<LoadedSystem, string>() // LoadedSystem 부모, 형제 호출시 Runtime에 변경하기 위한 정보 사전

            loadedParentStack.Push mySys
            loadSystem (pptReop, mySys, paras, isLib, pptParams, dicLoaded, dicPptDoc, pathStack, loadedParentStack, layoutImgPaths)

    let pptRepo = Dictionary<DsSystem, PptDoc>()

    /// GetDSFromPptWithLib > (loadFromPpts > GetImportModel >) loadSystem
    let private loadFromPpts (path: string ) isLib (pptParams:PptParams) (layoutImgPaths:HashSet<string>) (modelCnf:ModelConfig) =
        Copylibrary.Clear()
        let dicPptDoc = Dictionary<string, PresentationDocument>()
        let pathStack = Stack<string>() //파일 오픈시 예외 로그 path PPT Stack

        try
            try
                let cfg =  createModelConfigReplacePath (modelCnf, path)
                let sys, doc = PowerPointImportor.GetImportModel(pptRepo, path, isLib, pptParams, dicPptDoc, pathStack, layoutImgPaths)

                //ExternalSystem 순환참조때문에 완성못한 시스템 BuildSystem 마무리하기
                pptRepo
                    .Where(fun dic -> not dic.Value.IsBuilded)
                    .ForEach(fun (KeyValue(dsSystem, pptDoc)) ->
                        pathStack.Push(pptDoc.Path)
                        pptDoc.BuildSystem(dsSystem, pptParams.HwTarget, isLib, pptParams.CreateBtnLamp)
                        pathStack.Pop() |> ignore)

                {   Config = cfg
                    UserTagConfig = pptParams.UserTagConfig
                    System = sys
                    LoadingPaths = [] },
                pptRepo

            with ex ->
                let errFileName = pathStack.TryHead().DefaultValue(path)
                let fileErr= "File contains corrupted data."
                let msg =
                    if ex.Message.Contains(fileErr) then
                        ex.Message.Replace(fileErr, "베타 버전에서는 문서보안 PPT를 지원하지 않거나, PPT파일에 문제가 있습니다./\r\n복호화 후 다른 PC 실행이 필요합니다.")
                    else
                        ex.Message

                if not (msg.EndsWith(ErrorNotify)) then
                    ErrorPptNotify.Trigger(errFileName, 0, 0u, "")

                //첫페이지 아니면 stack에 존재
                failwithf $"{msg} \t◆파일명 {errFileName}"
        finally
            dicPptDoc.Values.Where(fun doc -> doc.IsNonNull()).Iter(dispose)


    type ImportPpt =
        static member GetDSFromPptWithLib(fullName: string, isLib:bool, pptParams:PptParams, cfg:ModelConfig): DSFromPpt =
            ModelParser.ClearDicParsingText()
            pptRepo.Clear()
            let layoutImgPaths = HashSet<string>() //LayoutImgPaths 저장

            let model, millisecond = duration (fun () -> loadFromPpts fullName isLib pptParams layoutImgPaths cfg |> Tuple.first)
            forceTrace $"Elapsed time for reading1 {fullName}: {millisecond} ms"

            let activePath = PathManager.changeExtension (fullName.ToFile()) "ds"

            let (system, loadingPaths), millisecond =
                duration(fun () ->
                    if pptParams.CreateFromPpt then
                        model.System, model.LoadingPaths
                    else
                        LoaderExt.ExportToDS (model.System, activePath)
                        ParserLoader.LoadFromActivePath activePath (pptParams.HwTarget.Platform) false )


            forceTrace $"Elapsed time for reading2 {fullName}: {millisecond} ms"

            {
                System = system
                ActivePath = activePath
                LoadingPaths = loadingPaths
                LayoutImgPaths = layoutImgPaths
            }

        static member GetRuntimeZipFromPpt(fullName: string, pptParams:PptParams, cfg:ModelConfig)=
            let ret = ImportPpt.GetDSFromPptWithLib(fullName, false, pptParams, cfg)
            DsAddressModule.assignAutoAddress(ret.System, pptParams.StartMemory, pptParams.OpMemory, pptParams.HwTarget)
            let zipPath = LoaderExt.saveModelZip(ret.LoadingPaths, ret.ActivePath, ret.LayoutImgPaths, cfg, pptParams.UserTagConfig)
            zipPath, ret.System


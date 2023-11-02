namespace Engine.Core

open System
open System.IO
open System.Linq
open System.Runtime.CompilerServices
open System.IO.Compression

[<AutoOpen>]
module FileManager =

    // Check if a file exists at the given path, returning the valid path
    let fileExistChecker (path: string): string =
        let path = path |> getValidFile
        if File.Exists(path) then
            path
        else
            raise (new FileNotFoundException($"File not found at path: {path}"))

    let fileExistFirstSelector (paths:string list) =
        let path = paths.FirstOrDefault(fun f -> File.Exists(f|> getValidFile))
        if path = null
        then 
            let errorMsg1 = $"loading path : {paths.Head}\n"
            let errorMsg2 = $"user environment path : {paths.Tail}"
            raise (new FileNotFoundException($"File not found at paths: \n{errorMsg1+errorMsg2}"))
        else path

    // Write the provided file content to the specified path
    let fileWriteAllText (path: string, fileContent: string) =
        let path = path |> getValidFile
        File.WriteAllText(path, fileContent)

    // Ensure that the directory of the specified path exists; create it if not
    let createDirectory (path: DsPath) =
        if not(isPathRooted path) then 
            raise (new ArgumentException($"createDirectory path must be an absolute path: {path}"))

        let directoryPath = 
            match path with
            |DsFile f-> Path.GetDirectoryName(f)
            |DsDirectory d-> d
            
        if not (Directory.Exists(directoryPath)) then
            Directory.CreateDirectory(directoryPath) |> ignore

    ///Powerpoint 에서만 구동됨 : runtime *.ds에서는 예외(다른PC, 이중관리, ..)상황을 고려해서 추후 구현
    ///PC에 초기 환경 변수 설정시 재부팅 필요
    ///사용자 DS_PATH만 적용가능 (시스템 환경변수는 관리자 권한 이슈 및 이중정의 이슈)
    ///모델 경로가 항상우선 찾고 다음 사용자 변수 Path 찾기
    let private environmentVariable = "DS_PATH"

    let collectEnvironmentVariablePaths(): string list =
        let userEnv = System.Environment.GetEnvironmentVariable(environmentVariable, EnvironmentVariableTarget.User)
        match userEnv with
        | null -> []
        | envVar ->
            envVar.Split(';', StringSplitOptions.RemoveEmptyEntries)
            |> Array.map(fun path -> path.Trim())
            |> List.ofArray

            
    let rec addFolderToZip(zip: ZipArchive, folderPath: string, baseFolderPath: string) =
        let di = DirectoryInfo(folderPath)
        for fileInfo in di.GetFiles().Where(fun w->w.Extension = ".ds") do
            let relativePath =
                if String.IsNullOrEmpty(baseFolderPath) then fileInfo.Name
                else Path.Combine(baseFolderPath, fileInfo.Name)
            let entry = zip.CreateEntry(relativePath)
            use fileStream = new FileStream(fileInfo.FullName, FileMode.Open, FileAccess.Read)
            use entryStream = entry.Open()
            fileStream.CopyTo(entryStream)
        
        for subDirectoryInfo in di.GetDirectories() do
            let subDirectoryBasePath =
                if String.IsNullOrEmpty(baseFolderPath) then subDirectoryInfo.Name
                else Path.Combine(baseFolderPath, subDirectoryInfo.Name)
            addFolderToZip(zip, subDirectoryInfo.FullName, subDirectoryBasePath)


    let zipFolderToByteArray(exportDirectoy:string) =
        use memoryStream = new MemoryStream()
        use zip = new ZipArchive(memoryStream, ZipArchiveMode.Create, true)
        let folderPath = Path.GetDirectoryName(exportDirectoy)
        addFolderToZip(zip, folderPath, "")
        memoryStream.ToArray()


    let getTopLevelDirectory (filePaths: string list) =
        if filePaths.IsEmpty then raise (new ArgumentException($"getTopLevelDirectory empty paths")) 

        // Find the common prefix among the file paths
        let commonPrefix = 
            List.fold (fun prefix filePath ->
                match prefix with
                | Some prefix' ->
                    let minLen = min (String.length prefix') (String.length filePath)
                    let mutable commonLen = 0
                    while commonLen < minLen && prefix'.[commonLen] = filePath.[commonLen] do
                        commonLen <- commonLen + 1
                    Some (prefix'.Substring(0, commonLen))
                | None -> None
            )  (Some (List.head filePaths)) (List.tail filePaths)
           
        // Extract the top-level directory from the common prefix
        commonPrefix
        //match commonPrefix with
        //| Some prefix -> Some(prefix |> Path.GetDirectoryName)
        //| None -> None


    let createZipFile(zipFilePath: string, filePaths: string seq) =
        try
            let topLevel = getTopLevelDirectory (filePaths |> Seq.toList)

            // Create a ZIP archive
            use fileStream = new FileStream(zipFilePath, FileMode.Create)
            use zip = new ZipArchive(fileStream, ZipArchiveMode.Create, true)


            for filePath in filePaths do
                if File.Exists(filePath) then
                    let fileDir  = PathManager.getDirectoryName (filePath|>DsFile)
                    let relativePath =
                        if topLevel.IsSome then
                            fileDir.Substring(topLevel.Value.Length)
                        else
                            fileDir // No common top-level directory

                    let entry = zip.CreateEntry(relativePath.Replace("\\", "/") + "/" + 6786 filePath)
                    use fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read)
                    use entryStream = entry.Open()
                    fileStream.CopyTo(entryStream)
                else
                    printfn "File not found: %s" filePath

            printfn "ZIP archive saved to: %s" zipFilePath

        with
        | :? InvalidDataException as ex ->
            printfn $"Error: InvalidDataException - The file could not be saved due to data corruption.{ex.Message}"
        | ex ->
            printfn "An unexpected error occurred: %s" ex.Message



    let saveZip(exportFilePath:string, modelRootPaths:string seq) = 
        createZipFile(exportFilePath, modelRootPaths)
      



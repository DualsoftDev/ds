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
        let dir = getDirectoryName (path|>DsFile)
        if not (Directory.Exists dir)
        then Directory.CreateDirectory dir |> ignore

        File.WriteAllText(path, fileContent)

    // Ensure that the directory of the specified path exists; create it if not
    let createDirectory (path: DsPath) =
        if not(isPathRooted (path.ToString())) then
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
            envVar.Split([|';'|], StringSplitOptions.RemoveEmptyEntries)
            |> Array.map(fun path -> path.Trim())
            |> List.ofArray

    let getValidZipFileName(topDir:string, extenstion:string): string  =
        let dir = topDir|>getValidDirectory
        let zipName =
            if isRuntimeLinux
            then
                if dir.Split('/').Length = 2 //root
                then $"{dir}/[{dir.Split('/').[1]}]"// /home  => /home/[home]
                else dir.TrimEnd('/')
            else
                if dir.Split(':').[1].Split('/').[1] = "" //root
                then $"{dir}[{dir.Split(':').[0]}]" //E:/  => E:/[E]
                else dir.TrimEnd('/')

        zipName+extenstion

    let getTopLevelDirectory (filePaths: string list) : string =
        if List.isEmpty filePaths then
            raise (new ArgumentException("getTopLevelDirectory: empty paths"))
        if filePaths.Length = 1 then
            getDirectoryName (filePaths.First().ToFile())
        else
            let splitPaths = filePaths |> List.map (fun path -> path.Split([|'\\'; '/'|]))
            let minLength = splitPaths |> List.map Array.length |> List.min

            let commonParts =
                [0 .. minLength - 1]
                |> List.map (fun i ->
                    let part = splitPaths.[0].[i]
                    if splitPaths |> List.forall (fun sp -> sp.[i] = part) then
                        Some(part)
                    else
                        None)

            let commonPrefix =
                match List.choose id commonParts with
                | [] -> raise (new ArgumentException("getTopLevelDirectory: error paths"))
                | cp -> String.Join(Path.DirectorySeparatorChar.ToString(), cp) |> getValidDirectory

            if PathManager.isPathRooted (commonPrefix)
            then commonPrefix
            else
                let topLevelDirSplit =
                    commonPrefix.Split(directorySeparatorDS) |> Array.rev |> Array.skip 1 |> Array.rev
                String.Join(directorySeparatorDS.ToString(), topLevelDirSplit)


    //모델 최상단 폴더에 Zip형태로 생성
    let saveZip(filePaths: string seq, zipFilePath:string) =

        let topLevel = getTopLevelDirectory (filePaths |> Seq.toList)
         // Create a ZIP archive
        use fileStream = new FileStream(zipFilePath, FileMode.Create)
        use zip = new ZipArchive(fileStream, ZipArchiveMode.Create, true)

        try

            for filePath in filePaths do
                if File.Exists(filePath) then
                    let fileDir  = PathManager.getDirectoryName (filePath|>DsFile)
                    let relativePath = fileDir.Substring(topLevel.Length)
                    let name =filePath |> DsFile |> getFileName
                    let entry = zip.CreateEntry(relativePath.Replace("\\", "/") + "/" + name)
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

          // Create a MemoryStream and copy the contents of the FileStream into it
        let memoryStream = new MemoryStream()
        fileStream.Seek(0L, SeekOrigin.Begin) |> ignore// Move the fileStream cursor to the beginning
        fileStream.CopyTo(memoryStream)
        zipFilePath, memoryStream


    let addFilesToExistingZipAndDeleteFiles (existingZipPath:string) (additionalFilePaths:string seq) =
        if not (File.Exists existingZipPath) then
            printfn "The specified ZIP file doesn't exist."
        else
            try
                use zipToOpen = new FileStream(existingZipPath, FileMode.Open, FileAccess.ReadWrite)
                use archive = new ZipArchive(zipToOpen, ZipArchiveMode.Update)

                for filePath in additionalFilePaths do
                    if File.Exists filePath then
                        let fileName = Path.GetFileName filePath
                        let newEntry = archive.CreateEntry fileName

                        use fileStream = new FileStream(filePath, FileMode.Open)
                        use entryStream = newEntry.Open()
                        fileStream.CopyTo(entryStream)
                        fileStream.Close()
                        // Delete the file after adding it to the ZIP archive
                        File.Delete(filePath)
                    else
                        printfn "File not found: %s" filePath

                printfn "Files added to the existing ZIP archive and deleted successfully."
            with
            |  ex ->
                printfn $"An error occurred: {ex.Message}"


    let unZip  zipDsPath =
        let extractName = PathManager.getFileNameWithoutExtension(zipDsPath|>DsFile)
        let extractPath = PathManager.combineFullPathDirectory([|PathManager.getTempPath();$"DsUnzippedFiles/{extractName}"|])
        // Ensure the temporary folder exists
        Directory.CreateDirectory(extractPath) |> ignore

        // Open the zip file for reading
        use archive = ZipFile.OpenRead(zipDsPath)

        // Extract each entry to the subfolder
        for entry in archive.Entries do
            let entryPath = PathManager.getFullPath (entry.FullName.TrimStart('/')|>DsFile) (extractPath|>DsDirectory)
            let entryDir = PathManager.getDirectoryName(entryPath|>DsFile)

            // Ensure the directory for the entry exists
            Directory.CreateDirectory(entryDir) |>ignore

            // Extract the entry to the subfolder
            entry.ExtractToFile(entryPath, true)

        // Return the path where the files are extracted
        let dsText = PathManager.getFullPath ($"{TextModelConfigJson}"|>DsFile)(extractPath|>DsDirectory)
        dsText

[<Extension>]
type FileHelper =
    [<Extension>] static member ToDsZip(filePaths: string seq, exportPath:string)  =
                        saveZip (filePaths, exportPath)|> fst
    [<Extension>] static member ToUnZip(zipDsPath:string)  =
                        unZip zipDsPath

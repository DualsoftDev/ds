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

    let getValidZipFileName(topDir:string): string  =
        if isRuntimeLinux
        then 
            if topDir.Split('/').Length = 2 //root
            then $"{topDir}/[{topDir.Split('/').[1]}]"// /home  => /home/[home]
            else topDir
        else 
            if topDir.Split(':').[1].Split('/').[1] = "" //root
            then $"{topDir}[{topDir.Split(':').[0]}]" //E:/  => E:/[E]
            else topDir

    let getTopLevelDirectory (filePaths: string list) : string =
        if List.isEmpty filePaths then
            raise (new ArgumentException("getTopLevelDirectory: empty paths"))
        let filePaths = filePaths
                        |> List.map (fun filePath -> Path.GetDirectoryName(filePath) |> getValidDirectory)

        let commonPrefix =
            filePaths
            |> List.map (fun filePath -> Path.GetDirectoryName(filePath) |> getValidDirectory)
            |> List.reduce (fun prefix dirPath ->
                let commonLen =
                    Seq.zip prefix dirPath
                    |> Seq.takeWhile (fun (c1, c2) -> c1 = c2)
                    |> Seq.length
                prefix[..commonLen-1]
            )

        let splitChar = PathManager.directorySeparatorDS
        let topLevelDirSplit = 
            let a =
                commonPrefix.Split(splitChar)
            a |> Array.rev |> Array.skip 1 |> Array.rev

        String.Join(splitChar, topLevelDirSplit)

    //모델 최상단 폴더에 Zip형태로 생성
    let saveZip(filePaths: string seq, extenstion:string) =
        let topLevel = getTopLevelDirectory (filePaths |> Seq.toList)
        let zipFilePath =(topLevel|>getValidZipFileName )+extenstion 
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
        
[<Extension>]
type FileHelper =
    [<Extension>] static member ToZip(filePaths: string seq)  = 
                        saveZip (filePaths, ".Zip")|> fst
    [<Extension>] static member ToZipPPT(filePaths: string seq)  = 
                        saveZip (filePaths, ".7z")|> fst  //".Zip" 형태지만 구분위해 확장자 다르게
    [<Extension>] static member ToZipStream(filePaths: string seq)  = 
                        saveZip (filePaths, ".Zip") |> fun (_, memoryStram) -> memoryStram.ToArray()    
   
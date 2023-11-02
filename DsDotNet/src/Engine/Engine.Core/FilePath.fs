namespace Engine.Core

open System
open System.IO
open System.Linq
open System.Runtime.CompilerServices
open System.IO.Compression

[<AutoOpen>]
module PathManager = 

    // Directory separator character:
    let directorySeparatorDS : char = '/'

    // Check if running on Linux/Unix where Path.DirectorySeparatorChar is '/'
    let isRuntimeLinux = Path.DirectorySeparatorChar = '/'

    type DsPath =
        | DsFile  of string      // Represents a file path (must include file extension)
        | DsDirectory of string // Represents a directory path (without a file extension)

        // Convert DsPath to a string representation
        override x.ToString() =
            match x with
            | DsFile filePath -> filePath
            | DsDirectory directoryPath -> directoryPath

        // Convert DsPath to a valid path string
        member x.ToValidPath() =
            let path =
                match x with
                | DsFile filePath ->

                 
                    // Validate file path
                    if String.IsNullOrWhiteSpace filePath then
                        raise (new ArgumentException($"Invalid characters in {filePath}"))
                    if not (Path.HasExtension filePath) then
                        raise (new ArgumentException($"file has no extension in {filePath}"))
                    
                    let file = Path.GetFileName filePath
                    let dirc = Path.GetDirectoryName filePath

                    if (dirc.ToCharArray() |> Array.exists (fun c -> Path.GetInvalidPathChars().Contains c)) then
                        raise (new ArgumentException($"Invalid DirectoryName in {filePath}"))

                    if (file.ToCharArray() |> Array.exists (fun c -> Path.GetInvalidFileNameChars().Contains c)) then
                        raise (new ArgumentException($"Invalid FileName in {filePath}"))

                    filePath

                | DsDirectory directoryPath ->
                    // Validate directory path
                    if String.IsNullOrWhiteSpace directoryPath then
                        raise (new ArgumentException($"Invalid characters in {directoryPath}"))
                    if Path.HasExtension directoryPath then
                        raise (new ArgumentException($"directory has an extension in {directoryPath}"))

                    if (directoryPath.ToCharArray() |> Array.exists (fun c -> Path.GetInvalidPathChars().Contains c)) then
                        raise (new ArgumentException($"Invalid DirectoryName in {directoryPath}"))

                    directoryPath

            FileInfo(path) |> ignore
            path.Replace("\\", directorySeparatorDS.ToString())

    // Get a valid DsPath from a given string
    let getValidFile (path: string) =
        DsFile(path).ToValidPath()   
       // Get a valid DsPath from a given string
    let getValidDirectory(path: string) =
        DsDirectory(path).ToValidPath()   
   
    // Get the file name from a DsPath
    let getFileName (filePath: DsPath): string =
        match filePath with
        | DsFile _ -> filePath.ToValidPath() |> Path.GetFileName |> getValidFile
        | DsDirectory directory -> raise (new ArgumentException($"({directory}) is not a file path"))

    // Get the directory name from a DsPath
    let getDirectoryName (filePath: DsPath): string =
        match filePath with
        | DsFile _ -> filePath.ToValidPath() |> Path.GetDirectoryName |> getValidDirectory
        | DsDirectory directory -> raise (new ArgumentException($"({directory}) is not a file path"))

    // Get the file name without an extension from a DsPath
    let getFileNameWithoutExtension (filePath: DsPath): string =
        match filePath with
        | DsFile _ -> filePath.ToValidPath() |> Path.GetFileNameWithoutExtension
        | DsDirectory directory -> raise (new ArgumentException($"({directory}) is not a file path"))

    // Check if a DsPath has an extension
    let hasExtension (path: DsPath): bool =
        Path.HasExtension(path.ToString())

    // Check if a DsPath is rooted (represents an absolute path)
    let isPathRooted (dsPath: DsPath): bool =
        if isRuntimeLinux then
            dsPath.ToValidPath().StartsWith("/")
        else
            Path.IsPathRooted(dsPath.ToString())

    // Change the extension of a DsPath
    let changeExtension (filePath: DsPath) (extension: string): string =
        match filePath with
        | DsFile _ -> Path.ChangeExtension(filePath.ToValidPath(), extension) |> getValidFile
        | DsDirectory directory -> raise (new ArgumentException($"({directory}) is not a file path"))

    // Get the root of a given path
    let getPathRoot (path: string): string =
        if isRuntimeLinux then
            // Unix/Linux 
            if path.Length > 0 && path.[0] = '/' then
                path.Split('/')[0] |> getValidDirectory
            else
                ""
        else
            // Windows
            Path.GetPathRoot(path) |> getValidDirectory

    // Calculate the relative path from one DsPath to another
    let getRelativePath (relativeToFilePath: DsPath) (myFilePath: DsPath): string =
        let re = relativeToFilePath
        let my = myFilePath
        
        if re.ToValidPath() = my.ToValidPath() then 
            raise (new ArgumentException($"Invalid GetRelativePath same path \n{re}\n{my}"))

        if not(re |> isPathRooted) || not(my |> isPathRooted) then 
            raise (new ArgumentException($"Invalid GetRelativePath not root \n{re}\n{my}"))

        if getPathRoot(re.ToString()) <> getPathRoot(my.ToString()) then
            raise (new ArgumentException($"Invalid GetRelativePath not same root \n{re}\n{my}"))

        let relativePath = Path.GetRelativePath(re.ToString(), my.ToString()) |> DsFile
        if isPathRooted relativePath then
            raise (new ArgumentException($"Invalid GetRelativePath between \n{re}\n{my}"))
        else
            let validPath = relativePath.ToValidPath().[3..]  //의미 없는 ../ 항상 붙어서 제거
            if validPath.StartsWith("../") then
                validPath //상위 폴더면 그대로 
            else
                $"./{validPath}" // 현재 폴더면 ./ 자동삽입

            

    // Get the full path from a relativeFilePath relative to an absoluteDirectory
    let getFullPath (relativeFilePath: DsPath) (absoluteDirectory: DsPath): string =

        match relativeFilePath with
        |DsFile _ -> ()
        |DsDirectory _ ->raise (new ArgumentException($"({relativeFilePath}) is not a file path"))
        match absoluteDirectory with
        |DsFile _ -> raise (new ArgumentException($"({absoluteDirectory}) is not a directory"))
        |DsDirectory _ -> ()


        if not (hasExtension relativeFilePath) then
            raise (new ArgumentException($"relativeFilePath error in {relativeFilePath}"))
        if not (isPathRooted absoluteDirectory) then
            raise (new ArgumentException($"absoluteDirectory error in {absoluteDirectory}"))

        Path.GetFullPath(relativeFilePath.ToString(), absoluteDirectory.ToString()) |> getValidFile

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

    ///exportFilePath 절대경로 *.Zip 형태 
    ///modelRootDir   모델 최상댄 절대경로 Directory
    let saveZip(exportFilePath:string, modelRootDir:string) =
        let exportFile   = exportFilePath |>getValidFile
        let modelRootDir = modelRootDir |>getValidDirectory

        use memoryStream = new MemoryStream()
        use zip = new ZipArchive(memoryStream, ZipArchiveMode.Create, true)
        addFolderToZip(zip, modelRootDir, "")


        use fileStream = new FileStream(exportFile, FileMode.Create)
        memoryStream.WriteTo(fileStream)


[<Extension>]
type FilePathHelper =
    [<Extension>] static member ToFile(x:string) : DsPath = DsFile(x)
    [<Extension>] static member ToDirectory(x:string) : DsPath = DsDirectory(x)


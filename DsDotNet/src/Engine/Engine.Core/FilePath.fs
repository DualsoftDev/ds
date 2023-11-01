namespace Engine.Core

open System
open System.IO
open System.Linq
open System.Runtime.CompilerServices

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

        if not(re |> isPathRooted) || not(my |> isPathRooted) then 
            raise (new ArgumentException($"Invalid GetRelativePath between {re} : {my}"))

        if getPathRoot(re.ToString()) <> getPathRoot(my.ToString()) then
            raise (new ArgumentException($"Invalid GetRelativePath between {re} : {my}"))

        let relativePath = Path.GetRelativePath(re.ToString(), my.ToString()) |> DsFile
        if isPathRooted relativePath then
            raise (new ArgumentException($"Invalid GetRelativePath between {re} : {my}"))
        else
            relativePath.ToValidPath().[3..]

    // Get the full path from a relativeFilePath relative to an absoluteDirectory
    let getFullPath (relativeFilePath: DsPath) (absoluteDirectory: DsPath): string =
        if isPathRooted relativeFilePath || not (hasExtension relativeFilePath) then
            raise (new ArgumentException($"relativeFilePath error in {relativeFilePath}"))
        if not (isPathRooted absoluteDirectory) || hasExtension absoluteDirectory then
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

    // Write the provided file content to the specified path
    let fileWriteAllText (path: string, fileContent: string) =
        let path = path |> getValidFile
        File.WriteAllText(path, fileContent)

    // Ensure that the directory of the specified path exists; create it if not
    let ensureDirectoryExists (path: string) =
        let path = path |> getValidFile
        let directoryPath = Path.GetDirectoryName(path)
        if not (Directory.Exists(directoryPath)) then
            Directory.CreateDirectory(directoryPath) |> ignore


[<Extension>]
type FilePathHelper =
    [<Extension>] static member ToFile(x:string) : DsPath = DsFile(x)
    [<Extension>] static member ToDirectory(x:string) : DsPath = DsDirectory(x)


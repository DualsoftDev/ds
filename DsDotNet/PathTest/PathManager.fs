namespace Engine.Core

open System
open System.IO
open System.Linq

module PathManager = 

    let directorySeparatorDS : char = '/'

    let isRuntimeLinux = Path.DirectorySeparatorChar = '/'


    type DsPath =
        | DsFile  of string
        | DsDirectory of string

        override x.ToString() =
            match x with
            | DsFile filePath -> filePath
            | DsDirectory directoryPath -> directoryPath

        member x.ToValidPath() =
            let path =
                match x with
                | DsFile filePath ->
                    if String.IsNullOrWhiteSpace filePath then
                        raise (new ArgumentException($"Invalid characters in {filePath}"))
                    if not (Path.HasExtension filePath) then
                        raise (new ArgumentException($"file has not extension in {filePath}"))

                    if (filePath.ToCharArray() |> Array.exists (fun c -> Path.GetInvalidPathChars().Contains c)) then
                        raise (new ArgumentException($"Invalid DirectoryName in {filePath}"))

                    if (filePath.ToCharArray() |> Array.exists (fun c -> Path.GetInvalidFileNameChars().Contains c)) then
                        raise (new ArgumentException($"Invalid FileName in {filePath}"))

                    filePath

                | DsDirectory directoryPath ->
                    if String.IsNullOrWhiteSpace directoryPath then
                        raise (new ArgumentException($"Invalid characters in {directoryPath}"))
                    if Path.HasExtension directoryPath then
                        raise (new ArgumentException($"directory has extension in {directoryPath}"))

                    if (directoryPath.ToCharArray() |> Array.exists (fun c -> Path.GetInvalidPathChars().Contains c)) then
                        raise (new ArgumentException($"Invalid DirectoryName in {directoryPath}"))

                    directoryPath

            FileInfo(path) |> ignore
            path.Replace("\\", directorySeparatorDS.ToString())

    let getValidPath (path: string) =
        DsFile(path).ToValidPath()   
   
    let getFileName (filePath: DsPath): string =
        match filePath with
        | DsFile _ -> filePath.ToValidPath() |> Path.GetFileName
        | DsDirectory directory -> raise (new ArgumentException($"({directory}) is not a file path"))

    let getDirectoryName (filePath: DsPath): string =
        match filePath with
        | DsFile _ -> filePath.ToValidPath() |> Path.GetDirectoryName
        | DsDirectory directory -> raise (new ArgumentException($"({directory}) is not a file path"))

    let getFileNameWithoutExtension (filePath: DsPath): string =
        match filePath with
        | DsFile _ -> filePath.ToValidPath() |> Path.GetFileNameWithoutExtension
        | DsDirectory directory -> raise (new ArgumentException($"({directory}) is not a file path"))

    let hasExtension (path: DsPath): bool =
        Path.HasExtension(path.ToValidPath())

    let private isPathRooted (dsPath: DsPath): bool =
        let path = dsPath.ToValidPath()
        if isRuntimeLinux then
            path.StartsWith("/")
        else
            Path.IsPathRooted(path)


    let changeExtension (filePath: DsPath) (extension: string): string =
        match filePath with
        | DsFile _ -> Path.ChangeExtension(filePath.ToValidPath(), extension) |> getValidPath
        | DsDirectory directory -> raise (new ArgumentException($"({directory}) is not a file path"))

    let getPathRoot (path: string): string =
        let path = path |> getValidPath
        if isRuntimeLinux then
            if path.Length > 0 && path.[0] = '/' then
                path.Split('/')[0]
            else
                ""
        else
            Path.GetPathRoot(path) |> getValidPath

    let getRelativePath (relativeToFilePath: DsPath) (myFilePath: DsPath): string =
        let re = relativeToFilePath.ToValidPath()
        let my = myFilePath.ToValidPath()
        if getPathRoot(re) <> getPathRoot(my) then
            raise (new ArgumentException($"Invalid GetRelativePath between {re} : {my}"))

        let relativePath = Path.GetRelativePath(re, my) |> DsFile
        if not (isPathRooted relativePath) then
            raise (new ArgumentException($"Invalid GetRelativePath between {re} : {my}"))
        else
            relativePath.ToValidPath().[3..]


    let getFullPath (relativeFilePath: DsPath) (absoluteDirectory: DsPath): string =
        if isPathRooted relativeFilePath || not (hasExtension relativeFilePath) then
            raise (new ArgumentException($"relativeFilePath error in {relativeFilePath}"))
        if not (isPathRooted absoluteDirectory) || hasExtension absoluteDirectory then
            raise (new ArgumentException($"absoluteDirectory error in {absoluteDirectory}"))

        Path.GetFullPath(relativeFilePath.ToString(), absoluteDirectory.ToString()) |> getValidPath


open PathManager

module FileManager =

    let fileExistChecker (path: string): string =
        let path = path |> getValidPath
        if File.Exists(path) then
            path
        else
            raise (new FileNotFoundException($"File not found at path: {path}"))

    let fileWriteAllText (path: string, fileContent: string) =
        let path = path |> getValidPath
        File.WriteAllText(path, fileContent)

    let ensureDirectoryExists (path: string) =
        let path = path |> getValidPath
        let directoryPath = Path.GetDirectoryName(path)
        if not (Directory.Exists(directoryPath)) then
            Directory.CreateDirectory(directoryPath) |> ignore


        
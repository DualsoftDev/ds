namespace Engine.Core

open System
open System.IO
open System.Linq
open System.Runtime.CompilerServices
open System.IO.Compression
open Dual.Common.Base.FS

[<AutoOpen>]
module PathManager =

    // Directory separator character:
    let directorySeparatorDS : char = '/'

    // Check if running on Linux/Unix where Path.DirectorySeparatorChar is '/'
    let isRuntimeLinux = Path.DirectorySeparatorChar = '/'
    // Check if a DsPath is rooted (represents an absolute path)
    let isPathRooted (dsPath: string): bool =

        if isRuntimeLinux then
            dsPath.StartsWith("/")
        else
            Net48Path.IsPathFullyQualified(dsPath.ToString())

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
                        raise (new ArgumentException($"Invalid path String.IsNullOrWhiteSpace"))
                    if not (Path.HasExtension filePath) then
                        raise (new ArgumentException($"file has no extension in {filePath}"))

                    let file = Path.GetFileName filePath
                    let dirc = Path.GetDirectoryName filePath

                    if (dirc.ToCharArray() |> Array.exists (Path.GetInvalidPathChars().Contains)) then
                        raise (new ArgumentException($"Invalid DirectoryName in {filePath}"))

                    if (file.ToCharArray() |> Array.exists (Path.GetInvalidFileNameChars().Contains)) then
                        raise (new ArgumentException($"Invalid FileName in {filePath}"))

                    let filePath =
                        if isPathRooted filePath && (filePath.Contains("../") ||filePath.Contains("..\\"))
                        then Path.GetFullPath filePath
                        else filePath

                    filePath.Replace("\\", directorySeparatorDS.ToString())

                | DsDirectory directoryPath ->
                    // Validate directory path
                    if String.IsNullOrWhiteSpace directoryPath then
                        raise (new ArgumentException($"Invalid characters in {directoryPath}"))

                    if (directoryPath.ToCharArray() |> Array.exists (Path.GetInvalidPathChars().Contains)) then
                        raise (new ArgumentException($"Invalid DirectoryName in {directoryPath}"))
                    if isPathRooted directoryPath && directoryPath.Contains("../") then
                        raise (new ArgumentException($"directoryPath path invalid (not support path ../) in {directoryPath}"))

                    let directoryPath =
                        if isPathRooted directoryPath && directoryPath.Contains("../")
                        then Path.GetFullPath directoryPath
                        else directoryPath

                    let directoryPath = directoryPath.Replace("\\", directorySeparatorDS.ToString())
                    if directoryPath.EndsWith("/")
                    then  directoryPath
                    else  directoryPath + "/"


            FileInfo(path) |> ignore
            path

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

    let getFolderOfPath (filePath: string): DsPath =
        if File.Exists filePath then
            (filePath |> DsFile).ToValidPath() |> Path.GetDirectoryName |> getValidDirectory |> DsDirectory
        else
            failwith $"({filePath}) is not a valid file path.  (not exists or is a directory)"

    // Get the file name without an extension from a DsPath
    let getFileNameWithoutExtension (filePath: DsPath): string =
        match filePath with
        | DsFile _ -> filePath.ToValidPath() |> Path.GetFileNameWithoutExtension
        | DsDirectory directory -> raise (new ArgumentException($"({directory}) is not a file path"))

    // Check if a DsPath has an extension
    let hasExtension (path: DsPath): bool =
        Path.HasExtension(path.ToString())


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

        if not(re.ToString() |> isPathRooted) || not(my.ToString() |> isPathRooted) then
            raise (new ArgumentException($"Invalid GetRelativePath not root \n{re}\n{my}"))

        if getPathRoot(re.ToString()) <> getPathRoot(my.ToString()) then
            raise (new ArgumentException($"Invalid GetRelativePath not same root \n{re}\n{my}"))

        let relativePath = Net48Path.GetRelativePath(re.ToString(), my.ToString()).Replace("\\", "/") |> DsFile
        if isPathRooted (relativePath.ToString()) then
            raise (new ArgumentException($"Invalid GetRelativePath between \n{re}\n{my}"))
        else
            // todo: 체크 필요
            // !!!!!!!!!!!!!!!!!!!!!!!!!!!! AHN
            let validPath = relativePath.ToValidPath().[3..]  //의미 없는 ../ 항상 붙어서 제거
            if validPath.StartsWith("../") || validPath.StartsWith("..\\") then
                validPath //상위 폴더면 그대로
            elif not <| validPath.StartsWith(".") then
                $"./{validPath}" // 현재 폴더면 ./ 자동삽입
            else
                $"./{relativePath}" // 현재 폴더면 ./ 자동삽입


    // Get the full path from a relativeFilePath relative to an absoluteDirectory
    let getFullPath (relativeFilePath: DsPath) (absoluteDirectory: DsPath): string =
        let rel = relativeFilePath
        let abs = absoluteDirectory


        match rel with
        |DsFile _ -> ()
        |DsDirectory _ ->raise (new ArgumentException($"({rel}) is not a file path"))
        match abs with
        |DsFile _ -> raise (new ArgumentException($"({abs}) is not a directory"))
        |DsDirectory _ -> ()


        if not (hasExtension rel) ||  rel.ToString().StartsWith("/") then
            raise (new ArgumentException($"relativeFilePath error in {rel}"))
        if not (isPathRooted (abs.ToString())) then
            raise (new ArgumentException($"absoluteDirectory error in {abs}"))

        Net48Path.GetFullPath(rel.ToString(), abs.ToString()) |> getValidFile

    let convertValidFile1 (absoluteFilePath:string) : string =
        absoluteFilePath.ToString() |> Path.GetFullPath  |> getValidFile

    let combineFullPathFile (xs: string  array) =
        Path.Combine(xs) |> Path.GetFullPath |> getValidFile
    let combineFullPathDirectory (xs: string  array) =
        Path.Combine(xs) |> Path.GetFullPath |> getValidDirectory


    let getTempPath() : string =
        Path.GetTempPath() |> getValidDirectory
    let getTempPathFile(fileName:string) : string =
        getFullPath (fileName|>DsFile) (getTempPath()|>DsDirectory)

[<Extension>]
type PathHelper =
    [<Extension>] static member ToFile(x:string) : DsPath = DsFile(x)
    [<Extension>] static member ToDirectory(x:string) : DsPath = DsDirectory(x)


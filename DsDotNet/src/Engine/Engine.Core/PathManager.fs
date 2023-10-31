module PathManager

open System
open System.IO
open System.Linq

// 디렉토리 구분 기호:
let directorySeparatorChar: char = '/'

// 경로를 Unix/Linux 스타일로 변환
let toDsPath (path:string) = path.Replace("\\", "/")
let invalidChars = [| '*' ; '?' ; '"' ; '<' ; '>' ; '|' |]
let isValidPath (path: string) =
    if (path = null) then
        raise (new ArgumentNullException($"Invalid characters in {path}"))
    if (path = "") then
        raise (new ArgumentException($"Invalid empty string"))
    if (path.ToCharArray() |> Array.exists (fun c -> invalidChars.Contains c))   then
        raise (new ArgumentException($"Invalid characters in {path}"))
    true

// 경로와 파일 이름에 허용되지 않는 특수 문자를 검사하여 FileInfo 예외를 체크
let getValidPath (path: string) =
    if isValidPath path
    then
        FileInfo(path) |> ignore
        path |> toDsPath
    else 
        raise (new ArgumentException($"Invalid path in {path}"))
        
// 함수를 사용하여 경로 변환을 수행하는 도우미
let doPathFunc (path: string) (func: string -> string): string =
    path |> getValidPath |> func |> toDsPath

// 경로에서 파일 이름을 반환
let getFileName (path: string): string =
    doPathFunc path Path.GetFileName

// 경로의 디렉토리 이름을 반환
let getDirectoryName (path: string): string =
    doPathFunc path Path.GetDirectoryName

// 파일 확장자를 제외한 이름을 반환
let getFileNameWithoutExtension (path: string): string =
    doPathFunc path Path.GetFileNameWithoutExtension

// 경로가 절대 경로인지 확인
let isPathRooted (path: string): bool =
    Path.IsPathRooted(path)

// 파일 확장자를 변경
let changeExtension (path: string) (extension: string): string =
    doPathFunc path (fun p -> Path.ChangeExtension(p, extension |> getValidPath))
// 사용할 수 없는 경로 문자를 반환
let getInvalidPathChars (): char[] =
    Path.GetInvalidPathChars()

// 사용할 수 없는 파일 이름 문자를 반환
let getInvalidFileNameChars (): char[] =
    Path.GetInvalidFileNameChars()

// 경로의 루트를 가져옴
let getPathRoot (path: string): string =
    let root = doPathFunc path Path.GetPathRoot
    if root.Length > 0 && root.[0] = '\\' then
        root |> toDsPath
    else
        root

// 경로를 결합
let getCombinePaths (path1: string) (path2: string): string =
    let path1 = path1 |> getValidPath
    let path2 = path2 |> getValidPath
    let combinedPath = Path.Combine(path1, path2)
    combinedPath |> toDsPath

// 임시 경로를 반환
let getTempPath (): string =
    Path.GetTempPath() |> toDsPath

////// 경로의 마지막 디렉토리 구분자를 제거
////let trimEndingDirectorySeparator (path: string): string =
////    doPathFunc path Path.TrimEndingDirectorySeparator

// 두 경로를 비교
let areEqual (pathA: string) (pathB: string): bool =
    String.Equals(pathA|>getValidPath, pathB|>getValidPath, StringComparison.OrdinalIgnoreCase)

//// 경로가 완전히 정규화되었는지 확인
//let isPathFullyQualified (path: string): bool =
//    if Path.DirectorySeparatorChar = '/' then  //Linux Path.DirectorySeparatorChar 실제 동작확인 필요
//        path.StartsWith("/") // Unix/Linux 스타일 경로
//    else
//        Path.IsPathFullyQualified(path) // Windows 스타일 경로

// 상대 경로를 계산
let getRelativePath (directory: string) (otherPath: string): string =
    let d = directory |> getValidPath
    let o = otherPath |> getValidPath
    if getPathRoot(d) <> getPathRoot(o) then
        raise (new ArgumentException($"Invalid GetRelativePath between {d} : {o}"))
    
    let relativePath = Path.GetRelativePath(d, o) |> toDsPath
    if not (relativePath.StartsWith("../")) then
        raise (new ArgumentException($"Invalid GetRelativePath between {d} : {o}"))
    else 
        relativePath.[3..] //첫 경로는 무의미 삭제

// 랜덤 파일 이름 생성
let getRandomFileName (): string =
    Path.GetRandomFileName()

// 경로에 확장자가 있는지 확인
let hasExtension (path: string): bool =
    Path.HasExtension(path |> getValidPath)

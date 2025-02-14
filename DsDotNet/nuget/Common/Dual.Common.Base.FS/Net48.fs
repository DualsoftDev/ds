namespace Dual.Common.Base.FS


open System
open System.Runtime.CompilerServices
open System.Diagnostics
open System.Collections.Generic
open System.IO

[<RequireQualifiedAccess>]
module Net48Path =
    let Exists (path: string) = File.Exists(path) || Directory.Exists(path)

    let GetRelativePath(fromPath: string, toPath: string) =
        let fromUri = Uri(fromPath)
        let toUri = Uri(toPath)

        if fromUri.Scheme <> toUri.Scheme then
            // 경로가 서로 다른 URI 스킴(예: file:// vs http://)을 가지고 있으면 상대 경로를 계산할 수 없습니다.
            toPath
        else
            let relativeUri = fromUri.MakeRelativeUri(toUri)
            let relativePath = Uri.UnescapeDataString(relativeUri.ToString())

            // Windows 경로의 경우 '/'를 '\'로 변환
            if toUri.Scheme = Uri.UriSchemeFile then
                relativePath.Replace('/', Path.DirectorySeparatorChar)
            else
                relativePath

    let GetFullPath (relativePath: string, absoluteBasePath: string) =
        let relativePath = relativePath.Replace("/", "\\")
        let absoluteBasePath = absoluteBasePath.Replace("/", "\\")
        // 절대 경로가 디렉터리임을 명시하기 위해 \로 끝나는지 확인
        let normalizedBasePath =
            let sep = Path.DirectorySeparatorChar.ToString()
            if absoluteBasePath.EndsWith(sep) then
                absoluteBasePath
            else
                absoluteBasePath + sep


        let baseUri = Uri(normalizedBasePath, UriKind.Absolute)
        let combinedUri = Uri(baseUri, relativePath)
        combinedUri.LocalPath

    let IsPathFullyQualified (path: string) =       // Path.IsPathFullyQualified 의 net48 버젼
        if System.String.IsNullOrEmpty(path) then false
        else
            // 경로가 절대 경로인지 확인
            if not (Path.IsPathRooted(path)) then false
            else
                // UNC 경로인지 확인
                let root = Path.GetPathRoot(path)
                not (System.String.IsNullOrEmpty(root) || root = "\\" || root = "/")



type Net48CompatibilityExtension =
    [<Extension>] static member DistinctBy(xs:'a seq, proj) = Seq.distinctBy proj xs
    [<Extension>]
    static member SkipLast(xs: seq<'a>, count: int) =
        if count <= 0 then xs
        else
            seq {
                let queue = Queue<'a>()
                for item in xs do
                    queue.Enqueue(item)
                    if queue.Count > count then
                        yield queue.Dequeue()
            }

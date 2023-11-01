module PathManagerTests

open Engine.Core.PathManager
open System
open Xunit

[<Fact>]
let ``getValidFile should return a valid DsFile path`` () =
    let path = "/path/to/file.txt"
    let validPath = getValidFile path
    Assert.Equal(path, validPath.ToString())

[<Fact>]
let ``getValidDirectory should return a valid DsDirectory path`` () =
    let path = "/path/to/directory"
    let validPath = getValidDirectory path
    Assert.Equal(path, validPath.ToString())

[<Fact>]
let ``getFileName should return the file name from a DsFile path`` () =
    let filePath = DsFile "/path/to/file.txt"
    let fileName = getFileName(filePath)
    Assert.Equal("file.txt", fileName)

[<Fact>]
let ``getFileName should raise an exception for a DsDirectory path`` () =
    let directoryPath = DsDirectory "/path/to/directory"
    Assert.Throws<ArgumentException>(fun () -> getFileName(directoryPath)|>ignore)

[<Fact>]
let ``getDirectoryName should return the directory name from a DsFile path`` () =
    let filePath = DsFile "/path/to/file.txt"
    let directoryName = getDirectoryName(filePath)
    Assert.Equal("/path/to", directoryName)

[<Fact>]
let ``getDirectoryName should raise an exception for a DsDirectory path`` () =
    let directoryPath = DsDirectory "/path/to/directory"
    Assert.Throws<ArgumentException>(fun () -> getDirectoryName(directoryPath)|>ignore)

[<Fact>]
let ``getFileNameWithoutExtension should return the file name without extension from a DsFile path`` () =
    let filePath = DsFile "/path/to/file.txt"
    let fileNameWithoutExtension = getFileNameWithoutExtension(filePath)
    Assert.Equal("file", fileNameWithoutExtension)

[<Fact>]
let ``getFileNameWithoutExtension should raise an exception for a DsDirectory path`` () =
    let directoryPath = DsDirectory "/path/to/directory"
    Assert.Throws<ArgumentException>(fun () -> getFileNameWithoutExtension(directoryPath)|>ignore)

[<Fact>]
let ``hasExtension should return true for a DsFile path with extension`` () =
    let filePath = DsFile "/path/to/file.txt"
    let result = hasExtension(filePath)
    Assert.True(result)

[<Fact>]
let ``hasExtension should return false for a DsFile path without extension`` () =
    let filePath = DsFile "/path/to/file"
    let result = hasExtension(filePath)
    Assert.False(result)

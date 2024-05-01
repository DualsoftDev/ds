namespace FileManagerTest

open Xunit
open Engine.Core.FileManager
open Engine.Core.PathManager
open System
open System.IO
open Engine.Core

module FileManagerTests =

    [<Fact>]
    let ``Given_valid_file_path_when_checking_existence_should_not_throw_exception`` () =
        // Arrange
        let filePath = "validFilePath.txt"
        File.WriteAllText(filePath, "Sample content")

        // Act and Assert
        try
            fileExistChecker(filePath) |> ignore
            // 예외가 발생하지 않았으므로 테스트 통과
        with
            | ex -> Assert.True(false, sprintf "Expected no exception, but got exception: %s" ex.Message)

        File.Delete(filePath)

    [<Fact>]
    let ``Given_invalid_file_path_when_checking_existence_should_throw_FileNotFoundException`` () =
        // Arrange
        let filePath = "invalidFilePath.txt"
    
        // Act and Assert
        Assert.Throws<FileNotFoundException>(fun () -> fileExistChecker(filePath)|>ignore)


    [<Fact>]
    let ``Given_valid_directory_path_when_ensuring_exists_should_not_throw_exception`` () =
        // Arrange
        let directoryPath = @"C:\DsTemp\validDirectoryPath"
    
        // Act and Assert
        try
            createDirectory(directoryPath.ToDirectory()) |> ignore
            // 예외가 발생하지 않았으므로 테스트 통과
        with
            | ex -> Assert.True(false, sprintf "Expected no exception, but got exception: %s" ex.Message)

        Directory.Delete(directoryPath) |> ignore

    [<Fact>]
    let ``Given_invalid_directory_path_when_ensuring_exists_should_throw_exception`` () =
        // Arrange
        let directoryPath = "validDirectoryPath/invalidPath"

        // Act and Assert
        Assert.Throws<ArgumentException>(fun () -> createDirectory(directoryPath.ToDirectory()))

    [<Fact>]
    let ``Given_valid_relative_file_path_and_absolute_directory_when_getting_full_path_should_return_valid_path`` () =
        // Arrange
        let relativeFilePath = "file.txt"
        let absoluteDirectory = Directory.GetCurrentDirectory()
    
        // Act
        let fullPath = getFullPath (relativeFilePath.ToFile()) (absoluteDirectory.ToDirectory())

        // Assert
        let expectedPath = Path.Combine(absoluteDirectory, relativeFilePath) |> getValidFile
        Assert.Equal(expectedPath, fullPath)

    [<Fact>]
    let ``Given_invalid_relative_file_path_and_absolute_directory_when_getting_full_path_should_throw_exception`` () =
        // Arrange
        let relativeFilePath = "file.txt"
        let absoluteDirectory = "invalidDirectoryPath"

        // Act and Assert
        Assert.Throws<ArgumentException>(fun () -> getFullPath(relativeFilePath.ToFile()) (absoluteDirectory.ToDirectory())|>ignore)

    [<Fact>]
    let ``Given_valid_relative_file_path_and_absolute_directory_when_getting_relative_path_should_return_valid_path`` () =
        // Arrange
        let relativeToFilePath =  @"C:\DsTemp\validDirectoryPath\file1.txt"
        let myFilePath =  @"C:\DsTemp\file2.txt"
    
        // Act
        let relativePath = getRelativePath(relativeToFilePath.ToFile()) (myFilePath.ToFile())

        // Assert
        Assert.Equal("../file2.txt", relativePath)

    [<Fact>]
    let ``Given_invalid_relative_file_path_and_absolute_directory_when_getting_relative_path_should_throw_exception`` () =
        // Arrange
        let relativeToFilePath = "file1.txt"
        let myFilePath = "file2.txt"

        // Act and Assert
        Assert.Throws<ArgumentException>(fun () -> getRelativePath(relativeToFilePath.ToFile()) (myFilePath.ToFile())|>ignore)

    [<Fact>]
    let ``Given_valid_DsPath_when_converting_to_string_should_return_valid_string_representation`` () =
        // Arrange
        let filePath = "validFilePath.txt"
        let directoryPath = "validDirectoryPath"
    
        // Act
        let filePathString = filePath.ToFile().ToString()
        let directoryPathString = directoryPath.ToDirectory().ToString()

        // Assert
        Assert.Equal(filePath, filePathString)
        Assert.Equal(directoryPath, directoryPathString)

    [<Fact>]
    let ``Given_valid_DsPath_when_checking_extension_should_return_true_if_has_extension`` () =
        // Arrange
        let filePathWithExtension = "file.txt"
        let filePathWithoutExtension = "file"
    
        // Act
        let hasExtension1 = filePathWithExtension.ToFile() |> hasExtension
        let hasExtension2 = filePathWithoutExtension.ToFile() |> hasExtension

        // Assert
        Assert.True(hasExtension1)
        Assert.False(hasExtension2)


    [<Fact>]
    let ``Given_valid_relative_directory_path_and_absolute_directory_when_getting_full_path_should_return_valid_path`` () =
        // Arrange
        let relativeDirectoryPath = "subfolder"
        let absoluteDirectory = Directory.GetCurrentDirectory()
    
        // Act
        Assert.Throws<ArgumentException>(fun () -> getFullPath(relativeDirectoryPath.ToDirectory()) (absoluteDirectory.ToDirectory())|>ignore)

    //[<Fact>]
    //let ``saveZip should create a ZIP archive and return the zip file path and memory stream`` () =
    //    // Arrange
    //    let filePaths = [ "path/to/file1.txt"; "path/to/file2.txt" ]

    //    // Act
    //    let zipFilePath =  filePaths |> FileHelper.ToZip

    //    // Assert
    //    Assert.True(File.Exists(zipFilePath)) // Check if the ZIP archive file exists

    //[<Fact>]
    //let ``ToZipStream should return a byte array of the ZIP archive`` () =
    //    // Arrange
    //    let filePaths = [ "path/to/file1.txt"; "path/to/file2.txt" ]

    //    // Act
    //    let zipStream = filePaths |> FileHelper.ToZipStream

    //    // Assert
    //    Assert.NotNull(zipStream) // Check if the zipStream is not null
    //    Assert.True(zipStream.Length > 0) // Check if the zipStream contains data

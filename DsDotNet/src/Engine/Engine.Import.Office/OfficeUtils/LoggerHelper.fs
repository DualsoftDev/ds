namespace Engine.Import.Office

open System
open System.IO

module LoggerHelper =
    
    let private logDirectory =
        let path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Dualsoft/PowerPointAddInLogger")
        if not (Directory.Exists(path)) then
            Directory.CreateDirectory(path) |> ignore
        path

    let private logFilePath = Path.Combine(logDirectory, "LoggerAddinHelper.txt")

    let Logger (message: string) =
        try
            use writer = new StreamWriter(logFilePath, true)
            writer.WriteLine($"[{DateTime.Now}] {message}")
        with ex ->
            // 예외 발생 시 콘솔 출력
            Console.WriteLine($"Error writing to log file: {ex.Message}")

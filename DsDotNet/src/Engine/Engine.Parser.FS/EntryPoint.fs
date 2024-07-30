namespace Engine.Parser.FS

open System.Reflection
open System.Linq


module ModuleInitializer =
    let Initialize () =
        let loadedAssemblies =
            Assembly.GetExecutingAssembly().GetReferencedAssemblies()

        let engineParserCS =
            loadedAssemblies.First(fun f -> f.Name = "Engine.Parser").Version

        let engineParserFS =
            Assembly.GetExecutingAssembly().GetName().Version

        if $"{engineParserFS}" <> $"{engineParserCS}" then
            failwithf $"Engine Version Error : F# ver {engineParserFS} <> C# ver {engineParserCS}"

        printfn "Engine.Parser.FS Module is being initialized..."
        ModelParser.Initialize()

        ()

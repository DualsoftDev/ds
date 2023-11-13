namespace Engine.Parser.FS
open System.Reflection
open System.Linq
open System


module ModuleInitializer =
    let Initialize() =
        let loadedAssemblies  = System.Reflection.Assembly.GetExecutingAssembly().GetReferencedAssemblies()
        let engineParserCS = loadedAssemblies.First(fun f->f.Name = "Engine.Parser").Version
        let engineParserFS = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version

        if $"{engineParserFS}" <> $"{engineParserCS}"
        then failwithf $"Engine Version Error : F# ver {engineParserFS} <> C# ver {engineParserCS}"

        printfn "Module is being initialized..."
        ModelParser.Initialize()
       
        ()

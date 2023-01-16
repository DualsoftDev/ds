// Template generated code from Antlr4BuildTasks.Template v 8.17
namespace Engine.Parser.FS

open System.IO
open System.Runtime.InteropServices
open Antlr4.Runtime
open Engine.Common.FS

type ParserError(line:int, column:int, message:string, ambient:string)=
    member val Line = line
    member val Column = column
    member val Message = message
    member val Ambient = ambient


type ErrorListener<'Symbol>([<Optional; DefaultParameterValue(false)>]throwOnError) =
    inherit ConsoleErrorListener<'Symbol>()

    member val Errors = ResizeArray<ParserError>()

    override x.SyntaxError(output:TextWriter, recognizer:IRecognizer, offendingSymbol:'Symbol, line:int,
            col:int, msg:string, e:RecognitionException) =
        let dsFile = recognizer.GrammarFileName
        match recognizer with
        | :? Parser as parser ->
            let ambient = parser.RuleContext.GetText()
            base.SyntaxError(output, recognizer, offendingSymbol, line, col, msg, e)
            logError($"Parser error on [{line}:{col}]@{dsFile}: {msg}")
            x.Errors.Add(new ParserError(line, col, msg, ambient))
            if throwOnError then
                ParserException($"{msg} near {ambient}", line, col) |> raise
        | :? Lexer as lexer ->
            logError($"Lexer error on [{line}:{col}]@{dsFile}: {msg}")
            x.Errors.Add(new ParserError(line, col, msg, ""))
            if throwOnError then
                ParserException($"Lexical error : {msg}", line, col) |> raise
        | _ ->
            failwithlog "ERROR"

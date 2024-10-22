// Template generated code from Antlr4BuildTasks.Template v 8.17
namespace Engine.Parser.FS

open System.IO
open System.Runtime.InteropServices
open Antlr4.Runtime
open Dual.Common.Core.FS

type ParserErrorRecord(line: int, column: int, message: string, ambient: string) =
    member val Line = line
    member val Column = column
    member val Message = message
    member val Ambient = ambient


type ErrorListener<'Symbol>([<Optional; DefaultParameterValue(false)>] throwOnError) =
    inherit ConsoleErrorListener<'Symbol>()

    member val Errors = ResizeArray<ParserErrorRecord>()

    override x.SyntaxError
        (
            output: TextWriter,
            recognizer: IRecognizer,
            offendingSymbol: 'Symbol,
            line: int,
            col: int,
            msg: string,
            e: RecognitionException
        ) =
        let dsFile = recognizer.GrammarFileName

        match recognizer with
        | :? Parser as parser ->
            let ambient = parser.RuleContext.GetText()
            base.SyntaxError(output, recognizer, offendingSymbol, line, col, msg, e)
            tracefn ($"Parser error on [{line}:{col}]@{dsFile}: {msg}")
            x.Errors.Add(new ParserErrorRecord(line, col, msg, ambient))

            if throwOnError then
                ParserError($"{msg} near {ambient}", line, col) |> raise
        | :? Lexer ->
            tracefn ($"Lexer error on [{line}:{col}]@{dsFile}: {msg}")
            x.Errors.Add(new ParserErrorRecord(line, col, msg, ""))

            if throwOnError then
                ParserError($"Lexical error : {msg}", line, col) |> raise
        | _ -> failwithlog "ERROR"

// Template generated code from Antlr4BuildTasks.Template v 8.17
namespace Engine.Parser.FS

using System.IO

public class ParserError
{
    public ParserError(int line, int column, string message, string ambient)
    {
        Line = line
        Column = column
        Message = message
        Ambient = ambient
    }

    public int Line { get; set; }
    public int Column { get; set; }
    public string Message { get; set; }
    public string Ambient { get; set; }

}

public class ErrorListener<Symbol> : ConsoleErrorListener<Symbol>
{
    bool _throwOnerror
    public List<ParserError> Errors = new()
    public ErrorListener(bool throwOnError = false)
    {
        _throwOnerror = throwOnError
    }

    public override void SyntaxError(TextWriter output, IRecognizer recognizer, Symbol offendingSymbol, int line,
        int col, string msg, RecognitionException e)
    {
        let dsFile = recognizer.GrammarFileName
        switch (recognizer)
        {
            case dsParser parser:
                let ambient = parser.RuleContext.GetText()
                base.SyntaxError(output, recognizer, offendingSymbol, line, col, msg, e)
                Global.Logger.Error($"Parser error on [{line}:{col}]@{dsFile}: {msg}")
                Errors.Add(new ParserError(line, col, msg, ambient))
                if (_throwOnerror)
                    throw new ParserException($"{msg} near {ambient}", line, col)
                break
            case dsLexer lexer:
                Global.Logger.Error($"Lexer error on [{line}:{col}]@{dsFile}: {msg}")
                Errors.Add(new ParserError(line, col, msg, ""))
                if (_throwOnerror)
                    throw new ParserException($"Lexical error : {msg}", line, col)
                break
            default:
                throw new Exception("ERROR")
        }
    }
}
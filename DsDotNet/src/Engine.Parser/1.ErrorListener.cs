// Template generated code from Antlr4BuildTasks.Template v 8.17
namespace Engine.Parser;

using System.IO;

public class ParserError
{
    public ParserError(int line, int column, string message, string ambient)
    {
        Line = line;
        Column = column;
        Message = message;
        Ambient = ambient;
    }

    public int Line { get; set; }
    public int Column { get; set; }
    public string Message { get; set; }
    public string Ambient { get; set; }

}

public class ErrorListener<Symbol> : ConsoleErrorListener<Symbol>
{
    bool _throwOnerror;
    public List<ParserError> Errors = new();
    public ErrorListener(bool throwOnError = false)
    {
        _throwOnerror = throwOnError;
    }

    public override void SyntaxError(TextWriter output, IRecognizer recognizer, Symbol offendingSymbol, int line,
        int col, string msg, RecognitionException e)
    {
        var dsFile = recognizer.GrammarFileName;
        var dsParser = recognizer as dsParser;
        var ambient = dsParser.RuleContext.GetText();
        base.SyntaxError(output, recognizer, offendingSymbol, line, col, msg, e);
        Global.Logger.Error($"Parser error on [{line}:{col}]@{dsFile}: {msg}");
        Errors.Add(new ParserError(line, col, msg, ambient));
        if (_throwOnerror)
            throw new ParserException($"{msg} near {ambient}", line, col);
    }
}
// Template generated code from Antlr4BuildTasks.Template v 8.17
namespace Engine.Parser;

public class Program
{
    static void Main(string[] args)
    {
        var text = @"
[sys ip = 192.168.0.1] My = {
    [flow] F = {        // GraphVertexType.Flow
        Main        // GraphVertexType.{ Segment | Parenting }
        > R3        // GraphVertexType.{ Segment }
        ;
        Main = {        // GraphVertexType.{ Segment | Parenting }
            // diamond
            Ap1 > Am1 > Bm1;
            Ap1 > Bp1 > Bm1;

            // diamond 2nd
            Bm1 >               // GraphVertexType.{ Child | Call | Aliased }
            Ap2 > Am2 > Bm2;
            Ap2 > Bp2 > Bm2;

            Bm2
            > A.""+""             // GraphVertexType.{ Child | Call }
            ;
        }
        R1              // define my local terminal real segment    // GraphVertexType.{ Segment }
            > C.""+""     // direct interface call wrapper segment    // GraphVertexType.{ Call }
            > Main2     // aliased to my real segment               // GraphVertexType.{ Segment | Aliased }
            > Ap1       // aliased to interface                     // GraphVertexType.{ Segment | Aliased | Call }
            ;

        [aliases] = {
            A.""+"" = { Ap1; Ap2; }
            A.""-"" = { Am1; Am2; }
            B.""+"" = { Bp1; Bp2; }
            B.""-"" = { Bm1; Bm2; }
            Main = { Main2; }
        }
    }
}
[sys] A = {
    [flow] F = {
        Vp > Pp > Sp;
        Vm > Pm > Sm;

        Vp |> Pm |> Sp;
        Vm |> Pp |> Sm;
        Vp <||> Vm;
    }
    [interfaces] = {
        ""+"" = { F.Vp ~ F.Sp }
        ""-"" = { F.Vm ~ F.Sm }
        // 정보로서의 상호 리셋
        ""+"" <||> ""-"";
    }
}";

        var helper = ModelParser.ParseFromString2(text, ParserOptions.Create4Simulation());
        var model = helper.Model;
        //Try("1 + 2 + 3");
        //Try("1 2 + 3");
        //Try("1 + +");
        System.Console.WriteLine("Done");
    }

    static void Try(string input)
    {
        var str = new AntlrInputStream(input);
        System.Console.WriteLine(input);
        var lexer = new dsLexer(str);
        var tokens = new CommonTokenStream(lexer);
        var parser = new dsParser(tokens);
        //var listener_lexer = new ErrorListener<int>();
        //var listener_parser = new ErrorListener<IToken>();
        //lexer.AddErrorListener(listener_lexer);
        //parser.AddErrorListener(listener_parser);
        //var tree = parser.file();
        //if (listener_lexer.had_error || listener_parser.had_error)
        //    System.Console.WriteLine("error in parse.");
        //else
        //    System.Console.WriteLine("parse completed.");
    }

    static string ReadAllInput(string fn)
    {
        var input = System.IO.File.ReadAllText(fn);
        return input;
    }
}

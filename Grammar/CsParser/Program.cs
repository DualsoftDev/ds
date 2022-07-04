// Template generated code from Antlr4BuildTasks.Template v 8.17
namespace DsParser
{
    using Antlr4.Runtime;
    using Antlr4.Runtime.Tree;

    using System.Text;

    public class Program
    {
        static void Main(string[] args)
        {
            var text = @"
[sys] it = {
    [task] T = { S1; S2; }
    [task] Task = { S1 = {TX ~ RX}; S2 = {TX ~ RX}; }
    [flow] F = {
        S1 <||> S2;
        A, B > C > D, E;
        T.S1 > T.S2;
    }
}";
//            text = @"
//[sys]MyElevatorSystem = {
//    [task]M = {
//        U;
//        D;
//        A12 = { M.U ~ S.S2U }
//    }
//    [task]B = { X; Y;  }
//    [task]T = { A21; X;  }
//    [flow] M = {U;D;}         //1모터 2방향 연결

//    [flow of B]remember_call_set = {
//        // 호출 Set기억
//        @pushr(A), #g(A), M.U > Set1F <| T.A21 ? T.X;
//        //A, B ? C > D, E;
//        myFlow = {A > B > C;}
//    }
//}";
            var parser = DsParser.FromDocument(text);
            var listener = new ElementsListener(parser);
            ParseTreeWalker.Default.Walk(listener, parser.program());
            var x = listener.links;

            Try("1 + 2 + 3");
            Try("1 2 + 3");
            Try("1 + +");
        }

        static void Try(string input)
        {
            var str = new AntlrInputStream(input);
            System.Console.WriteLine(input);
            var lexer = new dsLexer(str);
            var tokens = new CommonTokenStream(lexer);
            var parser = new dsParser(tokens);
            var listener_lexer = new ErrorListener<int>();
            var listener_parser = new ErrorListener<IToken>();
            lexer.AddErrorListener(listener_lexer);
            parser.AddErrorListener(listener_parser);
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
}

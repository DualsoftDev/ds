// Template generated code from Antlr4BuildTasks.Template v 8.17
namespace DsParser
{
    using Antlr4.Runtime;
    using Antlr4.Runtime.Tree;

    using System.Diagnostics;
    using System.Text;

    public class XProgram
    {
        static void Main(string[] args)
        {
            var text = @"
[sys] L = {
    [task] T = {
        Cp = {P.F.Vp ~ P.F.Sp}
        Cm = {P.F.Vm ~ P.F.Sm}
    }
    [flow] F = {
        Main = { T.Cp > T.Cm > X.xx; }
        //parenting = {A > B > C; C |> B; }
        //T.C1 <||> T.C2;
        //A, B > C > D, E;
        //T.C1 > T.C2;
    }
}
[sys] P = {
    [flow] F = {
        Vp > Pp > Sp;
        Vm > Pm > Sm;
    }
}
";
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
//        myFlow = {A > B > C; C |> B; }
//    }
//}";
            var parser = DsParser.FromDocument(text);
            var listener = new ModelListener(parser);
            ParseTreeWalker.Default.Walk(listener, parser.program());
            Trace.WriteLine("--- End of model listener");
            var model = listener.Model;

            parser.Reset();
            var elistener = new ElementsListener(parser, model);
            ParseTreeWalker.Default.Walk(elistener, parser.program());

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

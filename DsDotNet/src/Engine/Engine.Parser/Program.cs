// Template generated code from Antlr4BuildTasks.Template v 8.17
namespace Engine.Parser;

public class Program
{
    static string EveryScenarioText = @"
[sys ip = 192.168.0.1] My = {
    [flow] F = {        // GraphVertexType.Flow
        C1, C2 > C3, C4 |> C5;
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
            //> C.""+""     // direct interface call wrapper segment    // GraphVertexType.{ Call }
            > Main2     // aliased to my real segment               // GraphVertexType.{ Segment | Aliased }
            > Ap1       // aliased to interface                     // GraphVertexType.{ Segment | Aliased | Call }
            ;
        R2;

        [aliases] = {
            A.""+"" = { Ap1; Ap2; }
            A.""-"" = { Am1; Am2; }
            B.""+"" = { Bp1; Bp2; }
            B.""-"" = { Bm1; Bm2; }
            Main = { Main2; }
        }
        [safety] = {
            Main = {A.F.Sp; A.F.Sm}
        }
    }
    [emg] = {
        EMGBTN = { F; };
        //EmptyButton = {};
        //NonExistingFlowButton = { F1; };
    }
}
[sys ip=1.2.3.4] A = {
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
}
[sys] B = @copy_system(A);
[sys] C = @copy_system(A);
[prop] = {
    // Global safety
    [safety] = {
        My.F.Main = {B.F.Sp; B.F.Sm; C.F.Sp}
    }
    [addresses] = {
        My.F.Main = (%Q1234.2343, , )
    }
    [layouts] = {
        A.""+"" = (1309,405,205,83)
    }
}
";


    static void ParseNormal(string text)
    {
        var helper = ModelParser.ParseFromString2(text, ParserOptions.Create4Simulation());
        var model = helper.Model;
        //Try("1 + 2 + 3");
        //Try("1 2 + 3");
        //Try("1 + +");
        foreach (var kv in helper._elements)
        {
            var (p, type_) = (kv.Key, kv.Value);
            var types = type_.ToString("F");
            Trace.WriteLine(p.Combine("/")+$":{types}");
        }

        Trace.WriteLine("---- Spit result");
        var spits = model.Spit();
        foreach(var spit in spits)
        {
            var tName = spit.Obj.GetType().Name;
            var name = spit.NameComponents.Combine();
            Trace.WriteLine($"{name}:{tName}");
        }

        var spitObjs = spits.Select(spit => spit.Obj);
        var flowGraphs = spitObjs.OfType<Flow>().Select(f => f.Graph);
        var segGraphs = spitObjs.OfType<Segment>().Select(s => s.Graph);
        foreach (var gr in flowGraphs)
            gr.Dump();
        foreach (var gr in segGraphs)
            gr.Dump();

        System.Console.WriteLine("Done");
    }

    static void testBidirectional()
    {
        var values = new[]
        {
            (1, 3),
            (2, 5),
            (3, 1),
            (7, 4),
            (5, 9),
            (6, 2),
            (7, 3),
            (5, 2),
        };
        var processed = new HashSet<(int, int)>();

        IEnumerable<(int, int)[]> helper()
        {
            foreach (var value in values)
            {
                if (processed.Contains(value))
                    continue;
                processed.Add(value);

                var reverse = (value.Item2, value.Item1);
                if (values.Contains(reverse))
                {
                    yield return new[] { value, reverse };
                    processed.Add(reverse);
                }
                else
                    yield return new[] { value };
            }
        }

        var xx = helper().ToArray();

        var gr =
            values.GroupBy(tpl => values.Contains((tpl.Item2, tpl.Item1)))
                .ToArray();
    }
    public static void Main(string[] args)
    {
        testBidirectional();
        //ParseNormal(EveryScenarioText);


        ParseNormal(EveryScenarioText);
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

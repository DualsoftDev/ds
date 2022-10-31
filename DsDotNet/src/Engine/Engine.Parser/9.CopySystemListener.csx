namespace Engine.Parser;

class CopySystemListener : dsParserBaseListener   //ListenerBase
{
    public ParserHelper ParserHelper;
    dsParser _parser;

    public CopySystemListener(dsParser parser, ParserHelper helper)
    {
        ParserHelper = helper;
        parser.Reset();
        _parser = parser;
        //UpdateModelSpits();
    }


    public override void EnterModel(ModelContext ctx)
    {
        var sysCtxs = enumerateChildren<SystemContext>(ctx).ToArray();
        Console.WriteLine();

    }

    override public void EnterSystem(SystemContext ctx)
    {
        var sysCopyCtx = findFirstChild<SysCopySpecContext>(ctx);
        if (sysCopyCtx != null)
        {
            var srcSysName = findFirstChild<SourceSystemNameContext>(sysCopyCtx).GetText();
            var newSysName = findFirstChild<SystemNameContext>(ctx).GetText();

            var sysCtxs = enumerateChildren<SystemContext>(_parser.model()).ToArray();
            var srcSysCtx =
                enumerateChildren<SystemContext>(_parser.model())
                    .FirstOrDefault(sysctx => findFirstChild<SystemNameContext>(sysctx).GetText() == srcSysName)
                    ;

            var srcSys = ParserHelper.Model.Systems.First(sys => sys.Name == srcSysName);
        }
    }
}

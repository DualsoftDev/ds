namespace Engine.Parser.FS

class ParserResult
{
    public List<ParserRuleContext> rules = new();
    public List<ITerminalNode> terminals = new();
    public List<IErrorNode> errors = new();
}
class AllListener : dsParserBaseListener
{
    public ParserResult r = new ParserResult();

    // ParseTreeListener<> method
    public override void VisitTerminal(ITerminalNode node) { this.r.terminals.Add(node); }
    public override void VisitErrorNode(IErrorNode node)
    {
        this.r.errors.Add(node);
        throw new ParserException("ERROR while parsing", node);
    }
    public override void EnterEveryRule(ParserRuleContext ctx) { this.r.rules.Add(ctx); }
    public override void ExitEveryRule(ParserRuleContext ctx) { return; }

}

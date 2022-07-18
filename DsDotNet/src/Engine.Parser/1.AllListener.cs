using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

using ParseTree = Antlr4.Runtime.RuleContext;
namespace DsParser
{
    class ParserResult
    {
        public List<ParserRuleContext> rules = new List<ParserRuleContext>();
        public List<ITerminalNode> terminals = new List<ITerminalNode>();
        public List<IErrorNode> errors = new List<IErrorNode>();
    }
    class AllListener : dsBaseListener
    {
        public ParserResult r = new ParserResult();

        // ParseTreeListener<> method
        public override void VisitTerminal(ITerminalNode node)     { this.r.terminals.Add(node); }
        public override void VisitErrorNode(IErrorNode node)        { this.r.errors.Add(node); }
        public override void EnterEveryRule(ParserRuleContext ctx) { this.r.rules.Add(ctx); }
        public override void ExitEveryRule(ParserRuleContext ctx) { return; }

    }
}

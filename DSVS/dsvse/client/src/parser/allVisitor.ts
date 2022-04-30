import { ParserRuleContext, RuleContext } from "antlr4ts";
import { AbstractParseTreeVisitor, ErrorNode, ParseTree, ParseTreeWalker, TerminalNode } from "antlr4ts/tree";
import { dsListener } from "../server-bundle/dsListener";
import { dsVisitor } from "../server-bundle/dsVisitor";
import { dsParser } from "../server-bundle/dsParser";


export interface ParserResult
{
    rules: ParserRuleContext[];
    terminals: TerminalNode[];
    errors: ErrorNode[];
}

/**
 * Parse tree 전체 순회
 */
class AllListener implements dsListener
{
    r:ParserResult = {rules:[], terminals:[], errors:[]};

    // ParseTreeListener<> method
    visitTerminal (node: TerminalNode)     { this.r.terminals.push(node); }
    visitErrorNode(node: ErrorNode)        { this.r.errors.push(node); }
    enterEveryRule(ctx: ParserRuleContext) { this.r.rules.push(ctx); }
    exitEveryRule (ctx: ParserRuleContext) { return; }
}


export function getParseResult(parser:dsParser) : ParserResult
{
    const listener = new AllListener();
    ParseTreeWalker.DEFAULT.walk(listener, parser.program());
    return listener.r;
}

/**
 * parser tree 상의 모든 node (rule context, terminal node, error node) 을 반환한다.
 * @param text DS Document (Parser input)
 * @returns 
 */
export function getAllParseTrees(parser:dsParser) : ParseTree[]
{
    const r:ParserResult = getParseResult(parser);
    return [].concat.apply([], [r.rules, r.terminals, r.errors]);
}


/**
 * parser tree 상의 모든 rule 을 반환한다.
 * @param text DS Document (Parser input)
 * @returns 
 */
export function getAllParseRules(parser:dsParser) : ParseTree[]
{
    const r:ParserResult = getParseResult(parser);
    console.log(`Total ${r.rules.length} parser rules found.`);
    return r.rules;
}







// class AllVisitor extends AbstractParseTreeVisitor<number> implements dsVisitor<number> {
//     protected defaultResult(): number {
//         throw new Error("Method not implemented.");
//     }

//     visit(tree: ParseTree): number {
//         console.log('visited ' + typeof tree);

//         if (tree instanceof RuleContext)
//             this.visitChildren(tree);
//         return 0;
//     }

//     shouldVisitNextChild(node: RuleContext, currentResult: number) { return true;}
// }


// export function visitEveryRule(parser:dsParser)
// {
//     parser.reset();
//     const visitor = new AllVisitor();
//     visitor.visit(parser.program());
// }

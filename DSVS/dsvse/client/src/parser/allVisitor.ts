import { ParserRuleContext, RuleContext } from "antlr4ts";
import { AbstractParseTreeVisitor, ErrorNode, ParseTree, ParseTreeWalker, TerminalNode } from "antlr4ts/tree";
import { parserFromDocument } from "../clientParser";
import { dsListener } from "../server-bundle/dsListener";
import { dsVisitor } from "../server-bundle/dsVisitor";


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
    visitTerminal (node: TerminalNode)     { this.r.terminals.push(node) ; console.log('Visiting Terminal: ', node.text);}
    visitErrorNode(node: ErrorNode)        { this.r.errors.push(node)    ; console.log('Visiting ErrorNode: ', node.text);}
    enterEveryRule(ctx: ParserRuleContext) { this.r.rules.push(ctx)      ; console.log('Entering Rule: ', ctx.text);}
    exitEveryRule (ctx: ParserRuleContext) {                               console.log('Exiting Rule: ', ctx.text);}
}


export function getParseResult(text:string) : ParserResult
{
    const listener = new AllListener();
    const parser = parserFromDocument(text);
    ParseTreeWalker.DEFAULT.walk(listener, parser.program());
    return listener.r;
}

/**
 * parser tree 상의 모든 node (rule context, terminal node, error node) 을 반환한다.
 * @param text DS Document (Parser input)
 * @returns 
 */
export function getAllParseTrees(text:string) : ParseTree[]
{
    const r:ParserResult = getParseResult(text);
    return [].concat.apply([], [r.rules, r.terminals, r.errors]);
}


/**
 * parser tree 상의 모든 rule 을 반환한다.
 * @param text DS Document (Parser input)
 * @returns 
 */
export function getAllParseRules(text:string) : ParseTree[]
{
    const r:ParserResult = getParseResult(text);
    return r.rules;
}







class AllVisitor extends AbstractParseTreeVisitor<number> implements dsVisitor<number> {
    protected defaultResult(): number {
        throw new Error("Method not implemented.");
    }

    visit(tree: ParseTree): number {
        console.log('visited ' + typeof tree);

        if (tree instanceof RuleContext)
            this.visitChildren(tree);
        return 0;
    }

    shouldVisitNextChild(node: RuleContext, currentResult: number) { return true;}
}


export function visitEveryRule(text:string)
{
    const visitor = new AllVisitor();
    const parser = parserFromDocument(text);
    visitor.visit(parser.program());
}

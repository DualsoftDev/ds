import { ParserRuleContext } from "antlr4ts";
import { ErrorNode, ParseTree, ParseTreeWalker, TerminalNode } from "antlr4ts/tree";
import { dsListener } from "./dsListener";
import { dsParser } from "././dsParser";


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


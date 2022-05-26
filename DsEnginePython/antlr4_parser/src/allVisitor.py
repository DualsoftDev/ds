import sys
from typing import List, Dict, Any

from antlr4 import *
from dsLexer import dsLexer
from dsParser import dsParser
from dsListener import dsListener

from antlr4.tree.Trees import ParseTree, TerminalNode, ErrorNode #, ParseTreeWalker


# import { ParserRuleContext } from "antlr4ts";
# import { ErrorNode, ParseTree, ParseTreeWalker, TerminalNode } from "antlr4ts/tree";
# import { dsListener, dsParser } from './index';



class ParserResult:
    def __init__(self):
        self.rules: List[ParserRuleContext] = []
        self.terminals: List[TerminalNode] = []
        self.errors: List[ErrorNode] = []

# Parse tree 전체 순회
class AllListener(dsListener):
    def __init__(self): 
        self.r:ParserResult = ParserResult()

    # ParseTreeListener<> method
    def visitTerminal (self, node: TerminalNode):
        self.r.terminals.append(node)
    def visitErrorNode(self, node: ErrorNode):
        self.r.errors.append(node)
    def enterEveryRule(self, ctx: ParserRuleContext):
        self.r.rules.append(ctx)
    def exitEveryRule (self, ctx: ParserRuleContext):
        pass


def getParseResult(parser:dsParser) -> ParserResult:
    listener = AllListener()
    ParseTreeWalker.DEFAULT.walk(listener, parser.program())
    return listener.r

#
# parser tree 상의 모든 node (rule context, terminal node, error node) 을 반환한다.
# @param text DS Document (Parser input)
# @returns 
#
def getAllParseTrees(parser:dsParser) -> List[ParseTree]:
    r:ParserResult = getParseResult(parser)
    return r.rules + r.terminals + r.errors
    #return [].concat.apply([], [r.rules, r.terminals, r.errors])


#
# parser tree 상의 모든 rule 을 반환한다.
# @param text DS Document (Parser input)
# @returns 
#
def getAllParseRules(parser:dsParser) -> List[ParseTree]:
    r:ParserResult = getParseResult(parser)
    print(f'Total {len(r.rules)} parser rules found.')
    return r.rules



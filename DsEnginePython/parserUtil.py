# DS language parsing/traversing 을 위한 코드
from antlr4 import *
from dsLexer import dsLexer
from dsParser import dsParser
from antlr4.tree.Trees import ParseTree #, TerminalNode, ErrorNode
from typing import List #, Dict, Any

#import { ANTLRInputStream, CommonTokenStream } from 'antlr4ts';
#import { ParseTree } from 'antlr4ts/tree';


# DS 문서로부터 parser 객체를 생성해서 반환
# @param text DS 문서
def parserFromDocument(text:str):
	# Create the lexer and parser
	inputStream = InputStream(text)
	lexer = dsLexer(inputStream)
	tokenStream = CommonTokenStream(lexer)
	return dsParser(tokenStream)


# predicate: ParseTree -> boolean
def enumerateChildren(from_:ParseTree, includeMe=True, predicate = None ) -> List[ParseTree]:
    result:List[ParseTree] = []
    enumerateChildrenHelper(result, from_, includeMe, predicate);
    return result


# predicate:(t:ParseTree) => boolean
def enumerateChildrenHelper(result:List[ParseTree], from_:ParseTree, includeMe, predicate):
    def ok(t:ParseTree):
        if (predicate):
            return predicate(t)
        return True

    if (includeMe and ok(from_)):
        result.append(from_)
    for index in range(from_.getChildCount()):
        enumerateChildrenHelper(result, from_.getChild(index), True, ok)


# predicate:(t:ParseTree) => boolean 
def enumerateParents(_from:ParseTree, includeMe=True, predicate = None):
    def ok(t:ParseTree):
        if (predicate):
            return predicate(t)
        return True

    if (includeMe and ok(_from)):
        yield _from
    yield from enumerateParents(_from.parent, True, ok)



# predicate: (exp:ParseTree) => boolean
def findFirstChild(_from:ParseTree, predicate, includeMe=True):
    for c in enumerateChildren(_from, includeMe):
        if predicate(c):
            return c
    return None

# predicate: (exp:ParseTree) => boolean
def findFirstAncestor(_from:ParseTree, predicate, includeMe=True):
    for c in enumerateParents(_from, includeMe):
        if predicate(c):
            return c

    return None




# #import { CommonToken, ParserRuleContext } from "antlr4ts";

# def getOriginalText(text:str, node:ParserRuleContext):
#     const [s, e] = [node._start as CommonToken, node._stop as CommonToken];
#     const [sl, sc] = [s.line-1, s.charPositionInLine];
#     const [el, ec] = [e.line-1, e.charPositionInLine];
#     const lines = text.split('\n');

#     function *generateText() {
#         if (sl === el)
#             yield lines[sl].substring(sc, ec+1)
#         else {
#             yield lines[sl].substring(sc)
#             for (let i = sl + 1; i < el; i++)
#                 yield lines[i];
#             yield lines[el].substring(0, ec+1)
#         }
#     }

#     return Array.from(generateText()).join('\n');



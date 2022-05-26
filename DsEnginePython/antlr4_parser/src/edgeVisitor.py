# from cytoscapeVisitor.ts
# DS language parsing/traversing 을 위한 코드

from enum import Enum
from typing import List, Dict, Any, TypedDict
import json

from antlr4 import *
from dsLexer import dsLexer
from dsParser import dsParser   # CallContext, CausalOperatorContext, CausalPhraseContext, CausalTokenContext, CausalTokensCNFContext, CausalTokensDNFContext, FlowContext, ListingContext, SystemContext, TaskContext
from antlr4.tree.Trees import ParseTree, TerminalNode, ErrorNode#, ParseTreeWalker
from dsListener import dsListener
from parserUtil import enumerateChildren, enumerateParents, findFirstChild
from allVisitor import getAllParseRules, ParserResult

flatMap = lambda f, xss: [x for xs in xss for x in f(xs)]
map = lambda f, xs: [f(x) for x in xs]

#type NodeType = "system" | "task" | "call" | "proc" | "func" | "segment" | "expression" | "conjunction";
class NodeType(Enum):
    system = "system"
    task = "task"
    call = "call"
    proc = "proc"
    func = "func"
    segment = "segment"
    expression = "expression"
    conjunction = "conjunction"

class Node:
    id:str = None
    label:str = None
    type:NodeType = None
    parentId:str = None

    def __init__(self, id:str, label:str, parentId:str=None, type:NodeType=None):
        self.id = id
        self.label = label
        self.type = type
        self.parentId = parentId


# Causal 관계를 표현하는 Link.  'A > B' 일 때, left = A, right = B, operator = '>'
class CausalLink:
    # l: Node = None
    # r: Node = None
    # op:str = None

    def __init__(self, l:Node=None, r:Node=None, operator:str=None):
        self.l:Node = l
        self.r:Node = r
        self.op:str = operator
    # def get_l(self): return self.l
    # def get_r(self): return self.r
    # def get_op(self): return self.op

#
# array of Node or Nodes
# DNF (e.g A, B ? C) -> [[A, B], C]
#
#### todo 
# type Nodes = (Node | Node[])[];


# Parse tree 전체 순회
class ElementsListener(dsListener):
    def __init__(self, parser:dsParser):
        self.allParserRules = getAllParseRules(parser);
        parser.reset();

        #self.multipleSystems = self.allParserRules.filter(t => t instanceof SystemContext).length > 1;
        self.multipleSystems:bool = any(t for t in self.allParserRules if isinstance(t, dsParser.SystemContext))

        # causal operator 왼쪽
        self.left:dsParser.CausalTokensDNFContext = None
        self.op:dsParser.CausalOperatorContext = None

        self.systemName:str = None
        self.taskName:str = None
        self.flowName:str = None        # [flow of A]F={..} -> F
        self.flowOfName:str = None      # [flow of A]F={..} -> A
        self.allParserRules:List[ParseTree] = []
        self.multipleSystems:bool = False

        #self.nodes:Map<string, Node> = new Map();
        self.nodes:Dict[str, Node] = {}
        self.links:List[CausalLink] = []

        #self._existings:Map<dsParser.CausalTokensDNFContext, Nodes> = new Map();
        self._existings:Dict[dsParser.CausalTokensDNFContext, Nodes] = {}

    def enterSystem(self, ctx: dsParser.SystemContext):
        n = ctx.id_().getText()
        self.systemName = n
        if self.multipleSystems:
            self.nodes[n] = Node(n, n, None, NodeType.system)
    
    def exitSystem(self, ctx: dsParser.SystemContext):
        self.systemName = None
    
    def enterTask(self, ctx: dsParser.TaskContext):
        name = ctx.id_().getText()
        self.taskName = name
        id = f'{self.systemName}.{name}'
        self.nodes[id] = Node(id, name, self.systemName, NodeType.task)

    def exitTask(self, ctx: dsParser.TaskContext):
        self.taskName = None

    def enterListing(self, ctx: dsParser.ListingContext):
        name = ctx.id_().getText()
        id = f'{self.systemName}.{self.taskName}.{name}'
        node = {
            "data": {
                id:id,
                "label": name,
                "background_color": "gray",
                "parent": self.taskName }}

        parentId = f'{self.systemName}.{self.taskName}'
        self.nodes[id] = Node(id, name, parentId, NodeType.segment)
    

    def enterCall(self, ctx: dsParser.CallContext):
        name = ctx.id_().getText()
        label = f'{name}\n{ctx.callPhrase().getText()}'
        parentId = f'{self.systemName}.{self.taskName}'
        id = f'{parentId}.{name}'
        self.nodes[id] = Node(id, label, parentId, NodeType.call)
    

    def enterFlow(self, ctx: dsParser.FlowContext):
        flowOf = ctx.flowProp().id_()
        self.flowName = ctx.id_().getText()
        self.flowOfName = flowOf.getText() if flowOf else None
    
    def exitFlow(self, ctx: dsParser.FlowContext):
        self.flowName = None
        self.flowOfName = None
    
    

    def enterCausalPhrase(self, ctx: dsParser.CausalPhraseContext):
        self.left = None
        self.op = None
    
    def enterCausalTokensDNF(self, ctx: dsParser.CausalTokensDNFContext):
        if (self.left):
            assert self.op, 'operator expected'

            # process operator
            self.processCausal(self.left, self.op, ctx);

        self.left = ctx
    
    def enterCausalOperator(self, ctx: dsParser.CausalOperatorContext):
        self.op=ctx


    # ParseTreeListener<> method
    def visitTerminal (self, node: TerminalNode): pass
    def visitErrorNode(self, node: ErrorNode): pass
    def enterEveryRule(self, ctx: ParserRuleContext): pass
    def exitEveryRule (self, ctx: ParserRuleContext): pass


    # private
    def addNodes(self, ctx:dsParser.CausalTokensDNFContext): # -> Nodes
        if ctx in self._existings:
            return self._existings[ctx]

        cnfs: List[dsParser.CausalTokensCNFContext] =\
            enumerateChildren(ctx, False, lambda t: isinstance(t, dsParser.CausalTokensCNFContext))
            #.map(t => t as CausalTokensCNFContext)
            

        dnfNodes = []   # :Nodes
        for cnf in cnfs:
            cnfNodes:List[Node] = []
            causalTokens:List[dsParser.CausalTokenContext] =\
                enumerateChildren(cnf, False, lambda t: isinstance(t, dsParser.CausalTokenContext))
                # .map(t => t as CausalTokenContext)
            for t in causalTokens:
                text = t.getText()
                if text.startswith('#'):
                    node = Node(id=text, label=text, type=NodeType.func)
                    cnfNodes.append(node)
                elif text.startswith('@'):
                    node = Node(id=text, label=text, type=NodeType.proc)
                    cnfNodes.append(node)
                else:
                    # count number of '.' from text
                    dotCount = len(text.split('.')) - 1
                    id:str = text
                    taskId = f'{self.systemName}.{self.flowOfName}'
                    parentId = taskId
                    if dotCount == 0:
                        id = f'{taskId}.{text}'
                    elif dotCount == 1:
                        id = f'{self.systemName}.{text}'
                        parentId = f'{self.systemName}.{text.split(".")[0]}'
    
                    node = Node(id, text, taskId, NodeType.segment)
                    cnfNodes.append(node);
                
                for n in cnfNodes:
                    if not n.id in self.nodes:
                        self.nodes[n.id] = n

            dnfNodes.append(cnfNodes)

        self._existings[ctx] = dnfNodes
        
        return dnfNodes

    # @param nodes : DNF nodes
    # @param append true (==nodes 가 sink) 인 경우, conjuction 생성.  false: 개별 node 나열 생성
    # @returns 
    #private, nodes:Nodes
    def getCnfTokens(self, nodes, append=False) -> List[str]:
        cnfTokens:List[str] = []
        for x in nodes:
            isArray = isinstance(x, list) and len(x) > 1

            if append and isArray:
                id = map(lambda n: n.id, x).join(',')
                cnfTokens.append(id)
    
                conj = Node(id, label=None, parentId=self.taskName, type=NodeType.conjunction)
                self.nodes.set(id, conj)

                for src in x:
                    s = self.nodes.get(src.id)
                    self.links.append(CausalLink(l=s, r=conj, op="-"))

            else:
                if (isArray):
                    #x.flatMap(n => n.id).forEach(id => cnfTokens.push(id))
                    for id in flatMap(lambda n: n.id, x):
                        cnfTokens.append(id)
                else:
                    cnfTokens.append(x[0].id)

        return cnfTokens

    # 복합 Operator 를 분해해서 개별 operator array 로 반환
    # @param operator 복합 operator.  e.g "<||>"
    # @returns e.g [ "<|", "|>" ]
    # private
    def splitOperator(self, operator:str) -> List[str]:
        def split():
            op = operator
            for o in ['|>', '<|']:
                if o in op:
                    yield o
                    op = op.replace(o, '')

            for o in ['>', '<']:
                if o in op:
                    yield o
                    op = op.replace(o, '')

            if len(op) > 0:
                print("Error on causal operator:", operator)
            
        return list(split())

    # causal operator 를 처리해서 self.links 에 결과 누적
    # @param l operator 왼쪽의 DNF
    # @param opr (복합) operator
    # @param r operator 우측의 DNF
    # private
    def processCausal(self, l:dsParser.CausalTokensDNFContext, opr:dsParser.CausalOperatorContext, r:dsParser.CausalTokensDNFContext):
        print(f'{l.getText()} {opr.getText()} {r.getText()}')
        nodes = self.nodes

        ls = self.addNodes(l)
        rs = self.addNodes(r)
        # for (const n of self.nodes.keys())
        #     console.log(n);


        ops = self.splitOperator(opr.getText())

        for op in ops:
            sinkToRight = op == '>' or op == '|>'
            lss = self.getCnfTokens(ls, sinkToRight)
            rss = self.getCnfTokens(rs, not sinkToRight)
    
            for strL in lss:
                for strR in rss:
                    l = self.nodes.get(strL)
                    r = self.nodes.get(strR)
                    assert l and r, 'node not found'
                    if op == '|>' or op == '>':
                        self.links.append(CausalLink(l, r, op))
                    elif op == '<|' or op == '<':
                        self.links.append(CausalLink(r, l, op))
                    else:
                        print("Invalid operator:", op)
                        assert(False)

        print('-----------------');


# 전체 모델을 분석하여 grpah 생성을 위한 node 및 edge 구조를 생성한다.
# @param parser model parser tree
# @returns Graph elements (string)
def getElements(parser:dsParser) -> str:
    listener = ElementsListener(parser)
    ParseTreeWalker.DEFAULT.walk(listener, parser.program())

    def nodeMapper(n:Node):
        bg = 'green'
        style = None   # style override
        classes = [n.type]
        t = n.type
        if t == NodeType.func:
            bg = 'springgreen'; style = {"shape": "rectangle"}
        elif t == NodeType.system:
            bg = 'transparent'; style = {"shape": "rectangle"}
        elif t == NodeType.proc:
            bg = 'lightgreen'
        elif t == NodeType.task:
            bg = 'grey'
        elif t == NodeType.call:
            bg = 'purple'
        elif t == NodeType.conjunction:
            bg = 'beige'; style = {"shape": "rectangle", "width": 3, "height" : 3}

        return {
            "data": {
                "id": n.id, "label": n.label, "parent":n.parentId, "background_color": bg
            },
            "style":style,
            "classes":classes
        }

    nodes = map(nodeMapper, listener.nodes.values())

    def linkMapper(conn:CausalLink):
        [l, op, r] = [conn.l, conn.op, conn.r]
        id = l.id + op + r.id
        lineStyle = 'dashed' if '|' in op else 'solid'
        return {"data": {"id":id,"source":l.id, "target":r.id, "line-style":lineStyle}};

    # {"data":{"id":"MyElevatorSystem.B>A,B","source":"MyElevatorSystem.B","target":"A,B","line-style":"solid"}}
    edges = map(linkMapper, listener.links)

    print('nodes:')
    for n in nodes:
        print(json.dumps(n))

    print('edges:')
    for e in edges:
        print(json.dumps(e))

    elements = json.dumps([nodes, edges].flat());
        
    return elements


# from cytoscapeVisitor.ts
# DS language parsing/traversing 을 위한 코드

import copy
from enum import Enum
# from platform import node
from typing import List, Dict, Union #, Any, TypedDict
# import json

from antlr4 import *
# from dsLexer import dsLexer
from dsParser import dsParser   # CallContext, CausalOperatorContext, CausalPhraseContext, CausalTokenContext, CausalTokensCNFContext, CausalTokensDNFContext, FlowContext, ListingContext, SystemContext, TaskContext
from antlr4.tree.Trees import ParseTree, TerminalNode, ErrorNode#, ParseTreeWalker
from dsListener import dsListener
from ds_data_handler import ds_signal_exchanger
from parserUtil import enumerateChildren #, enumerateParents, findFirstChild
from allVisitor import getAllParseRules #, ParserResult

from ds_system_builder import ds_system
from ds_system_builder import ds_relay
from ds_system_builder import ds_tag
from ds_system_builder import ds_segment
from ds_system_builder import ds_consumer_builder
from ds_signal_handler import ds_status

flatMap = lambda f, xss: [x for xs in xss for x in f(xs)]
map = lambda f, xs: [f(x) for x in xs]

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
    def __init__(self, id:str, label:str, parentId:str=None, type:NodeType=None):
        assert id != None and id != "None" and ".None." not in id
        self.id:str = id
        self.label:str = label
        self.type:NodeType = type
        self.parentId:str = parentId


# Causal 관계를 표현하는 Link.  'A > B' 일 때, left = A, right = B, operator = '>'
class CausalLink:
    def __init__(self, l:Node=None, r:Node=None, operator:str=None):
        self.l:Node = l
        self.r:Node = r
        self.op:str = operator

#
# array of Node or Nodes
# DNF (e.g A, B ? C) -> [[A, B], C]
#
Nodes: List[Union[Node, List[Node]]]
now_parent = None

# Parse tree 전체 순회
class ElementsListener(dsListener):
    def __init__(self, parser:dsParser):
        self.allParserRules = getAllParseRules(parser)
        parser.reset()

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

        self.nodes:Dict[str, Node] = {}
        self.links:List[CausalLink] = []

        self._existings:Dict[dsParser.CausalTokensDNFContext, Nodes] = {}

        self.engine_builder = ds_consumer_builder()

    def enterSystem(self, ctx: dsParser.SystemContext):
        n = ctx.id_().getText()
        self.systemName = n
        # print("0.", self.systemName)
        if self.multipleSystems:
            self.nodes[n] = Node(n, n, None, NodeType.system)

        self.engine_builder.assign_object(ds_system(self.systemName))
    
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
        print("????????", id)
        self.nodes[id] = Node(id, name, parentId, NodeType.segment)
    

    def enterCall(self, ctx: dsParser.CallContext):
        name = ctx.id_().getText()
        label = f'{name}\n{ctx.callPhrase().getText()}'
        parentId = f'{self.systemName}.{self.taskName}'
        id = f'{parentId}.{name}'
        self.nodes[id] = Node(id, label, parentId, NodeType.call)
        # print("1.", id)
        sys:ds_system = self.engine_builder.get_object(self.systemName)
        _, sys_imp = sys.get_imparter()
        self.engine_builder.assign_object(
            ds_tag(id, self.systemName, sys_imp)
        )

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

    def enterParenting(self, ctx: dsParser.ParentingContext):
        full_text = ctx.getText().split('=')
        now_parent = f"{self.systemName}.{full_text[0]}"
        lines = full_text[1].replace('{', '').replace('}', '').split(';')
        lines = [
            line.replace('<|', '^_^')\
                .replace('|>', '^_^')\
                .replace('<', '^_^')\
                .replace('>', '^_^')\
                .split('^_^')
            for line in lines
            if len(line) > 0
        ]
        segments = [f"{now_parent}.{seg}" for seg in sum(lines, [])]
        sys:ds_system = self.engine_builder.get_object(self.systemName)
        _, sys_imp = sys.get_imparter()
        parent_relay = ds_relay(now_parent, self.systemName, sys_imp)
        pr_name, pr_imp = parent_relay.get_imparter()
        parent_tag = ds_tag(now_parent, pr_name, pr_imp)
        pt_name, pt_imp = parent_tag.get_imparter()
        parent_seg = ds_segment(now_parent, pt_name, pt_imp)
        ps_name, ps_imp = parent_seg.get_imparter()
        
        parent_relay.assign_end_signal(pt_name, ds_status.F)
        parent_relay.assign_clear_signal(pt_name, ds_status.R)

        parent_tag.assign_start_signal(pr_name, ds_status.G)
        parent_tag.assign_end_signal(ps_name, ds_status.F)
        parent_tag.assign_reset_signal(pr_name, ds_status.H)
        parent_tag.assign_clear_signal(ps_name, ds_status.R)

        parent_seg.assign_start_signal(pt_name, ds_status.G)
        parent_seg.assign_reset_signal(pt_name, ds_status.H)

        for seg in sum(lines, []):
            relay_name = f"{now_parent}.{seg}"
            tag_name = f"{self.systemName}.{seg}.Tag"
            linked_tag:ds_tag = self.engine_builder.get_object(tag_name)
            parent_seg.assign_end_signal(f"{relay_name}.Relay", ds_status.F)
            parent_seg.assign_clear_signal(f"{relay_name}.Relay", ds_status.R)
            now_relay = ds_relay(relay_name, ps_name, ps_imp)
            # print(" -", relay_name, "start by :", ps_name, "& linked tag :", tag_name)
            now_relay.assign_start_signal(ps_name, ds_status.G)
            now_relay.assign_end_signal(tag_name, ds_status.F)
            now_relay.assign_reset_signal(ps_name, ds_status.H)
            now_relay.assign_clear_signal(tag_name, ds_status.R)
            linked_tag.assign_start_signal(f"{relay_name}.Relay", ds_status.G)
            linked_tag.assign_reset_signal(f"{relay_name}.Relay", ds_status.H)
            _, tag_exc = linked_tag.get_exchanger()
            _, now_imp = now_relay.get_imparter()
            tag_exc.connect(tag_name, f"{relay_name}.Relay", now_imp)
            self.engine_builder.assign_object(now_relay)
        # print("2. parent :", now_parent, ", children :", segments)

        self.engine_builder.assign_object(
            ds_relay(now_parent, self.systemName, sys_imp)
        )
        self.engine_builder.assign_object(parent_relay)
        self.engine_builder.assign_object(parent_tag)
        self.engine_builder.assign_object(parent_seg)
        return
    

    def enterCausalTokensDNF(self, ctx: dsParser.CausalTokensDNFContext):
        if (self.left):
            assert self.op, 'operator expected'

            # process operator
            self.processCausal(self.left, self.op, ctx)

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
                    taskId = self.systemName
                    if self.flowOfName:
                        taskId += f'.{self.flowOfName}'
                    parentId = taskId
                    if dotCount == 0:
                        id = f'{taskId}.{text}'
                    elif dotCount == 1:
                        id = f'{self.systemName}.{text}'
                        parentId = f'{self.systemName}.{text.split(".")[0]}'
    
                    node = Node(id, text, taskId, NodeType.segment)
                    cnfNodes.append(node)
                
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
        print("???", nodes)
        for x in nodes:
            isArray = type(x) == list and len(x) > 1

            if append and isArray:
                id = map(lambda n: n.id, x)
                id = ','.join(id)
                cnfTokens.append(id)
                conj = Node(id, label=None, parentId=self.taskName, type=NodeType.conjunction)
                self.nodes[id] = conj

                for src in x:
                    s = self.nodes[src.id]
                    self.links.append(CausalLink(l=s, r=conj, operator="-"))

            else:
                if (isArray):
                    #x.flatMap(n => n.id).forEach(id => cnfTokens.push(id))
                    for id in map(lambda n: n.id, x):
                        print("====", id)
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
        # print(f'{l.getText()} {opr.getText()} {r.getText()}')
        # nodes = self.nodes
        # print(f"3. {self.systemName}.{l.getText()}, {self.systemName}.{r.getText()}")
        objs = [
            f"{self.systemName}.{l.getText()}",
            f"{self.systemName}.{r.getText()}"
        ]
        ls = self.addNodes(l)
        rs = self.addNodes(r)

        ops = self.splitOperator(opr.getText())

        for op in ops:
            sinkToRight = op == '>' or op == '|>'
            lss = self.getCnfTokens(ls, sinkToRight)
            rss = self.getCnfTokens(rs, not sinkToRight)
    
            for strL in lss:
                for strR in rss:
                    print(strL, strR)
                    l = self.nodes[strL]
                    r = self.nodes[strR]
                    print(l, r)
                    assert l and r, 'node not found'
                    if op == '|>' or op == '>':
                        self.links.append(CausalLink(l, r, op))
                    elif op == '<|' or op == '<':
                        self.links.append(CausalLink(r, l, op))
                    else:
                        print("Invalid operator:", op)
                        assert(False)

        sys:ds_system = self.engine_builder.get_object(self.systemName)
        _, sys_imp = sys.get_imparter()
        for obj in objs:
            if not self.engine_builder.check_object_in_raw(obj) == True:
                now_relay = ds_relay(obj, self.systemName, sys_imp)
                pr_name, pr_imp = now_relay.get_imparter()
                now_tag = ds_tag(obj, pr_name, pr_imp)
                pt_name, pt_imp = now_tag.get_imparter()
                now_seg = ds_segment(obj, pt_name, pt_imp)
                ps_name, _ = now_seg.get_imparter()
                
                now_relay.assign_end_signal(pt_name, ds_status.F)
                now_relay.assign_clear_signal(pt_name, ds_status.R)

                now_tag.assign_start_signal(pr_name, ds_status.G)
                now_tag.assign_end_signal(ps_name, ds_status.F)
                now_tag.assign_reset_signal(pr_name, ds_status.H)
                now_tag.assign_clear_signal(ps_name, ds_status.R)

                now_seg.assign_start_signal(pt_name, ds_status.G)
                now_seg.assign_reset_signal(pt_name, ds_status.H)

                self.engine_builder.assign_object(now_relay)
                self.engine_builder.assign_object(now_tag)
                self.engine_builder.assign_object(now_seg)

        print('-----------------')


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

        assert ".None." not in n.id, f'{n.id}'

        return {
            "data": {
                "id": n.id, "label": n.label, "parent":n.parentId, "background_color": bg
            },
            "style":style,
            "classes":classes
        }

    def linkMapper(conn:CausalLink):
        [l, op, r] = [conn.l, conn.op, conn.r]
        id = l.id + op + r.id
        lineStyle = 'reset' if '|' in op else 'start'
        return {"data": {"id":id,"source":l.id, "target":r.id, "line-style":lineStyle}}

    nodes = map(nodeMapper, listener.nodes.values())

    edges = map(linkMapper, listener.links)

    now_dict = listener.engine_builder.get_object_list()
    
    print('edges:')
    for e in edges:
        edge = e['data']['line-style']
        src = f"{e['data']['source']}.Relay"
        tgt = f"{e['data']['target']}.Relay"
        if listener.engine_builder.check_object_in(src) == False:
            finder = src.split('.')
            finder.reverse()
            finder.pop()
            finder.reverse()
            find_src = f"{'.'.join(finder)}"
            for obj_name in now_dict:
                if find_src in obj_name:
                    src = obj_name
        if listener.engine_builder.check_object_in(tgt) == False:
            finder = tgt.split('.')
            finder.reverse()
            finder.pop()
            finder.reverse()
            find_tgt = f"{'.'.join(finder)}"
            for obj_name in now_dict:
                if find_tgt in obj_name:
                    tgt = obj_name

        print(src, edge, tgt)
        target_obj:ds_relay = listener.engine_builder.get_object(tgt)

        if src == tgt:
            if edge == "reset":
                target_obj.assign_reset_signal(src, ds_status.F)
        else:
            if edge == "start":
                target_obj.assign_start_signal(src, ds_status.F)
            elif edge == "reset":
                target_obj.assign_reset_signal(src, ds_status.G)

    print('start check:')
    start_sets = {}
    for obj in now_dict:
        obj_type = obj.split('.')
        obj_type.reverse()
        now_system = copy.deepcopy(obj_type).pop()
        if obj_type[0] == "Relay":
            now_relay:ds_relay = listener.engine_builder.get_object(obj)
            _, rel_exc = now_relay.get_exchanger()
            if len(rel_exc.start_signals) == 0:
                print("there are no start signals :", obj)
                obj_type.reverse()
                obj_type.pop()
                starter = f"{'.'.join(obj_type)}_starter"
                print("generate starter tag :", f"{starter}.Tag", "in system", now_system)
                now_system:ds_system = listener.engine_builder.get_object(now_system)
                _, sys_imp = now_system.get_imparter()
                starter_tag = ds_tag(starter, now_system, sys_imp)
                starter_tag.assign_reset_signal(obj, ds_status.G)
                starter_tag.toggle_pausable_switch(False)
                start_sets[f"{starter}.Tag"] = starter_tag
                now_obj:ds_relay = listener.engine_builder.get_object(obj)
                now_obj.assign_start_signal(f"{starter}.Tag", ds_status.F)

    for name, starter in start_sets.items():
        listener.engine_builder.assign_object(starter)
        listener.engine_builder.assign_outer_object(name)

    print('nodes:')
    for n in nodes:
        if len(n['data']['label'].split('\n')) > 1:
            call = n['data']['label'].split('\n')
            now = f"{n['data']['parent']}.{call[0]}.Tag"
            ser = call[1].split('~')
            now_tag:ds_tag = listener.engine_builder.get_object(now)
            _, now_imp = now_tag.get_imparter()
            if len(ser) == 3:
                start, end, reset = ser

                if reset != '_' and\
                    listener.engine_builder.check_object_in(f"{reset}_reseter.Tag") == True:
                    rst_tag:ds_tag = listener.engine_builder.get_object(f"{reset}_reseter.Tag")
                    rst_tag.assign_start_signal(now, ds_status.H)
                else:
                    # To do...
                    # Reset 관련해서 처리해야함
                    pass
            else:
                start, end = ser
            
            if start != '_' and \
                listener.engine_builder.check_object_in(f"{start}_starter.Tag") == True:
                st_tag:ds_tag = listener.engine_builder.get_object(f"{start}_starter.Tag")
                st_name, st_exc = st_tag.get_exchanger()
                st_tag.assign_start_signal(now, ds_status.G)
                st_exc.connect(st_name, now, now_imp)

            if end != '_':
                ed_tag:ds_tag = listener.engine_builder.get_object(f"{end}.Tag")
                ed_name, ed_exc = ed_tag.get_exchanger()
                now_tag.assign_end_signal(ed_name, ds_status.F)
                ed_exc.connect(ed_name, now, now_imp)

    print("ds_object list:")
    final_dict = listener.engine_builder.get_object_list()
    for name, obj in final_dict.items():
        if not type(obj) == ds_system:
            _, now_exc = obj.get_exchanger()
            print(name, now_exc.start_signals, now_exc.end_signals)

    # elements = json.dumps([nodes, edges].flat())
    # elements = [nodes, edges]
        
    return listener.engine_builder
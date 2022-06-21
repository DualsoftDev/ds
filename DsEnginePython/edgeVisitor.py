# from cytoscapeVisitor.ts
# DS language parsing/traversing 을 위한 코드

import re
import copy
# from iteration_utilities import deepflatten
import itertools
from functools import reduce
import operator

from cgi import print_arguments
from enum import Enum
from select import select
from statistics import variance
# from platform import node
from typing import List, Dict, Tuple, Union #, Any, TypedDict
# import json

from antlr4 import *
# from dsLexer import dsLexer
from dsParser import dsParser   # CallContext, CausalOperatorContext, CausalPhraseContext, CausalTokenContext, CausalTokensCNFContext, CausalTokensDNFContext, FlowContext, ListingContext, SystemContext, TaskContext
from antlr4.tree.Trees import ParseTree, TerminalNode, ErrorNode#, ParseTreeWalker
from dsListener import dsListener
from ds_data_handler import ds_signal_exchanger
from parserUtil import enumerateChildren, enumerateChildrenHelper, enumerateParents, findFirstChild
from allVisitor import getAllParseRules #, ParserResult

from ds_system_builder import ds_system_object
from ds_system_builder import ds_relay
from ds_system_builder import ds_tag
from ds_system_builder import ds_segment
from ds_system_builder import ds_consumer_builder
from ds_signal_handler import ds_object, ds_status, signal_set

from ds_expression_handler import parse_expr

flatMap = lambda f, xss: [x for xs in xss for x in f(xs)]
map = lambda f, xs: [f(x) for x in xs]

class NodeType(Enum):
    system = "system"
    flow = "flow"
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
    def __init__(self, l:Node=None, r:Node=None, operator:str=None, parentId:str=None):
        self.l:Node = l
        self.r:Node = r
        self.op:str = operator
        self.parentId:str = parentId

#
# array of Node or Nodes
# DNF (e.g A, B ? C) -> [[A, B], C]
#
Nodes: List[Union[Node, List[Node]]]
now_parent = None
childerns:Dict[str, str] = {}

# Parse tree 전체 순회
class ElementsListener(dsListener):
    def __init__(self, parser:dsParser):
        self.allParserRules = getAllParseRules(parser)
        parser.reset()

        self.multipleSystems:bool = any(t for t in self.allParserRules if isinstance(t, dsParser.SystemContext))

        # causal operator 왼쪽
        self.left:dsParser.CausalTokensDNFContext = None
        self.op:dsParser.CausalOperatorContext    = None

        self.systemName:str = None
        self.taskName:str   = None
        self.flowName:str   = None      # [flow of A]F={..} -> F
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
        middle = self.taskName if self.taskName else self.flowName
        id = f'{self.systemName}.{middle}.{name}'
        now_type = NodeType.segment
        if name[0] == '!' or name[1] == "#":
            id = f'{name}'
            now_type = NodeType.func
        elif name[0] == "@":
            id = f'{name}'
            now_type = NodeType.func
        parentId = f'{self.systemName}.{middle}'
        if not id in self.nodes:
            self.nodes[id] = Node(id, name, parentId, now_type)
    
    def enterCall(self, ctx: dsParser.CallContext):
        name = ctx.id_().getText()
        label = f'{ctx.callPhrase().getText()}'
        parentId = f'{self.systemName}.{self.taskName}'
        id = f'{parentId}.{name}'
        if not id in self.nodes:
            self.nodes[id] = Node(id, label, parentId, NodeType.call)

    def enterFlow(self, ctx: dsParser.FlowContext):
        flowOf = ctx.flowProp().id_()
        self.flowName = ctx.id_().getText()
        self.flowOfName = flowOf.getText() if flowOf else None
        id = f'{self.systemName}.{self.flowName}'
        self.nodes[id] = Node(id, self.flowName, self.systemName, NodeType.flow)

    def exitFlow(self, ctx: dsParser.FlowContext):
        self.flowName = None
        self.flowOfName = None

    def enterCausalPhrase(self, ctx: dsParser.CausalPhraseContext):
        self.left = None
        self.op = None

    def enterParenting(self, ctx: dsParser.ParentingContext):
        all_children = []
        children: List[dsParser.CausalTokensCNFContext] =\
            enumerateChildren(ctx, False, lambda t: isinstance(t, dsParser.CausalTokensCNFContext))
        if not len(ctx.call_listing()) == 0:
            all_children = ctx.call_listing()

        all_children.extend(children)

        global now_parent
        middle = self.taskName if self.taskName else self.flowName
        now_parent = f"{self.systemName}.{middle}.{ctx.id_().getText()}"
        self.nodes[now_parent] = \
            Node(now_parent, None, f"{self.systemName}.{middle}", NodeType.segment)
        print(f"children in {now_parent}:")
        for child in all_children:
            str_child = child.getText()
            print("child :", str_child)
            childerns[str_child] = now_parent
            id = str_child
            child_type = NodeType.segment
            if len(str_child.split(',')) >= 2:
                child_type = NodeType.conjunction
            elif str_child[0] == '!' or str_child[0] == '#':
                child_type = NodeType.func
            elif str_child[0] == '@':
                child_type = NodeType.proc
            else:
                id = f"{now_parent}.{id}"
            
            self.nodes[id] = Node(str_child, None, now_parent, child_type)

        if len(children) == 0:
            now_parent = None
        print("-----------------")

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

        global now_parent

        cnfs: List[dsParser.CausalTokensCNFContext] =\
            enumerateChildren(ctx, False, lambda t: isinstance(t, dsParser.CausalTokensCNFContext))

        dnfNodes = []   # :Nodes
        for cnf in cnfs:
            cnfNodes:Dict[str, Node] = {}
            causalTokens:List[dsParser.CausalTokenContext] =\
                enumerateChildren(cnf, False, lambda t: isinstance(t, dsParser.CausalTokenContext))
            middle = self.taskName if self.taskName else self.flowName

            parent = now_parent
            if now_parent == None:
                parent = f"{self.systemName}.{middle}"

            for t in causalTokens:
                text = t.getText()
                if text[0] == '#':
                    node = Node(id=text, label=text, parentId=parent, type=NodeType.func)
                    cnfNodes[text] = node
                elif text[0] == '@':
                    node = Node(id=text, label=text, parentId=parent, type=NodeType.proc)
                    cnfNodes[text] = node
                elif text[0] == '!':
                    node = Node(id=text, label=text, parentId=parent, type=NodeType.func)
                    cnfNodes[text] = node
                else:
                    # count number of '.' from text
                    dotCount = len(text.split('.')) - 1
                    id:str = text
                    # if dotCount == 0:
                    #     id = text
                    # elif dotCount == 1:
                    #     # parent = f'{self.systemName}'
                    #     id = text
                    if id in childerns:
                        parent = childerns[id]
                    print(f"id : {id} - {f'{parent}.{text}'}")
                    node = Node(id, text, parent, NodeType.segment)
                    cnfNodes[f"{parent}.{text}"] = node
                
                for id, node in cnfNodes.items():
                    if not id in self.nodes:
                        self.nodes[id] = node

            dnfNodes.append(list(cnfNodes.values()))

        self._existings[ctx] = dnfNodes
        
        return dnfNodes

    # @param nodes : DNF nodes
    # @param append true (==nodes 가 sink) 인 경우, conjuction 생성.  false: 개별 node 나열 생성
    # @returns 
    #private, nodes:Nodes
    def getCnfTokens(self, nodes, append=False) -> List[str]:
        cnfTokens:List[str] = []
        for x in nodes:
            isArray = type(x) == list and len(x) > 1

            if append and isArray:
                id = list(set(map(lambda n: n.id, x)))
                if len(id) >= 2:
                    id = [
                        name.replace(f"{self.systemName}.", "")\
                            .replace(f"{self.flowName}.", "")
                        for name in id
                    ]
                    
                id = ','.join(id)
                cnfTokens.append(id)
                parent = f"{self.systemName}.{self.flowName}"
                if not now_parent == None:
                    parent = now_parent
                conj = Node(id, label=None, parentId=parent, type=NodeType.conjunction)
                self.nodes[id] = conj

                for src in x:
                    if '!' in src.id or '#' in src.id or '@' in src.id or ',' in src.id :
                        s = self.nodes[src.id]
                    else:
                        s = self.nodes[f"{parent}.{src.id}"]
                    self.links.append(CausalLink(l=s, r=conj, operator="-", parentId=parent))

            else:
                if (isArray):
                    for id in map(lambda n: n.id, x):
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
        global now_parent
        middle = self.taskName if self.taskName else self.flowName
        
        ls = self.addNodes(l)
        rs = self.addNodes(r)

        ops = self.splitOperator(opr.getText())
        if now_parent == None:
            now_parent = f"{self.systemName}.{middle}"
        print("now_parent :", now_parent)
        for op in ops:
            sinkToRight = op == '>' or op == '|>'
            lss = self.getCnfTokens(ls, sinkToRight)
            rss = self.getCnfTokens(rs, not sinkToRight)
    
            for strL in lss:
                for strR in rss:
                    if '#' in strL or '!' in strL or '@' in strL or ',' in strL:
                        str_real_L = strL
                    else:
                        if strL in childerns:
                            parent = childerns[strL]
                            str_real_L = f"{parent}.{strL}"
                        else:
                            str_real_L = f"{now_parent}.{strL}"
                    l = self.nodes[str_real_L]
                    
                    if '#' in strR or '!' in strR or '@' in strR or ',' in strR:
                        str_real_R = strR
                    else:
                        if strR in childerns:
                            parent = childerns[strR]
                            str_real_R = f"{parent}.{strR}"
                        else:
                            str_real_R = f"{now_parent}.{strR}"
                    r = self.nodes[str_real_R]
                    
                    assert l and r, 'node not found'
                    if not l.type == NodeType.func:
                        print(l.id, l.type, l.label, l.parentId)
                    if not r.type == NodeType.func:
                        print(r.id, r.type, r.label, r.parentId)

                    if op == '|>' or op == '>':
                        self.links.append(CausalLink(l, r, op, l.parentId))
                    elif op == '<|' or op == '<':
                        self.links.append(CausalLink(r, l, op, r.parentId))
                    else:
                        print("Invalid operator:", op)
                        assert(False)
        
        print('-----------------')
        now_parent = None

# 전체 모델을 분석하여 grpah 생성을 위한 node 및 edge 구조를 생성한다.
# @param parser model parser tree
# @returns Graph elements (string)
def getElements(parser:dsParser) -> str:
    listener = ElementsListener(parser)
    ParseTreeWalker.DEFAULT.walk(listener, parser.program())
    id_cnt_map:Dict[str, int] = {}
    to_use_id_map:Dict[str, List[str]] = {}
    final_reset_map:Dict[str, List[str]] = {}
    variance_id_map:Dict[str, List[str]] = {}
    call_segments:Dict[str, List[str]] = {}

    def nodeMapper(n:Node):
        classes = [n.type]
        assert ".None." not in n.id, f'{n.id}'

        return {
            "id": n.id, "label": n.label, "parent":n.parentId, "classes":classes[0]
        }

    def linkMapper(conn:CausalLink):
        [l, op, r, p] = [conn.l, conn.op, conn.r, conn.parentId]
        id = l.id + op + r.id
        edge_type = 'reset' if '|' in op else 'include' if '-' in op else 'start'
        return {"id":id,"source":l.id, "target":r.id, "parent": p,"edge_type":edge_type}

    def name_setter(node, target):
        names =  re.finditer(rf'{target}', node['id'])
        name_set = [
            (
                node['id'][n.span()[0]:node['id'].find(')', n.span()[1], len(node['id'])) + 1],
                node['id'][n.span()[1]:node['id'].find(')', n.span()[1], len(node['id']))]
            )
            for n in names
        ]
        for name in name_set:
            final_name = name[1]
            node['id'] = node['id'].replace(name[0], f"#({final_name}%G)")

        return node

    def remove_symbols(exp:list):
        symbol = True
        target_symbols = '#!&|,gh'
        while symbol == True:
            symbol = False
            for ts in target_symbols:
                if ts in exp:
                    symbol = True
                    exp.remove(ts)
        return exp

    def add_parent(node):
        name_set = node['id'].split(',')
        parents = node['parent'].split('.')
        for name in name_set:
            if '#' in name:
                name_list = parse_expr(name)
                name_list = remove_symbols(name_list)
                name_list = list(set(name_list))
                new_name_list = [
                    f"{parents[0]}.{parents[1]}.{n}" 
                    if n.count('.') == 0 \
                    else 
                    f"{parents[0]}.{n}" \
                    if n.count('.') == 1 
                    else 
                    n
                    for n in name_list
                ]
                for i in range(len(name_list)):
                    # tag names
                    node['id'] = \
                        node['id']\
                            .replace(
                                name_list[i], 
                                new_name_list[i]
                            )
            else:
                relay_name = \
                    f"{node['parent']}.{name}" if name.count('.') == 0 \
                    else f"{node['parent']}.{name}" \
                    if name.count('.') == 1 else name
                tag_name = \
                    f"{parents[0]}.{parents[1]}.{name}" if name.count('.') == 0 \
                    else f"{parents[0]}.{name}" \
                    if name.count('.') == 1 else name
                # relay name / tag name
                node['id'] = \
                    node['id']\
                        .replace(
                            name, 
                            f"{relay_name}\\{tag_name}"
                        )
        return node

    def relay_tag_generater(node):
        node = name_setter(node, '#g\(')
        node = name_setter(node, '#h\(')
        node['id'] = node['id']\
            .replace('#', '')\
            .replace('&&', '&')\
            .replace('||', '|')
        return node

    def relay_name_extractor(names):
        if ',' in names:
            names = names.split(',')
        else:
            names = [names]

        now_name = []
        for n in names:
            if '\\' in n:
                now_name.append(n.split('\\')[0])
            else:
                now_name.append(n)
        return ','.join(now_name)

    def tag_name_extractor(names):
        if ',' in names:
            names = names.split(',')
        else:
            names = [names]

        now_name = []
        for n in names:
            if '\\' in n:
                now_name.append(n.split('\\')[1])
            else:
                now_name.append(n)
        return ','.join(now_name)

    def split_name_list(names):
        if ',' in names:
            return names.split(',')
        else:
            return [names]

    def relay_tag_selecter(tag_list, idx, relay_name, parent_name):
        final_name = relay_name, ds_object.Relay
        tag_name = tag_list[idx]
        if listener.nodes[parent_name].type == NodeType.flow or\
            listener.nodes[parent_name].type == NodeType.task:
            if tag_name in listener.nodes:
                if listener.nodes[tag_name].type == NodeType.call:
                    final_name = tag_name, ds_object.Tag

        return final_name

    def ds_relay_assigner(parent, relay):
        if listener.engine_builder.check_object_in(f"{relay}.Relay"):
            return

        p_obj = listener.engine_builder.get_object(parent)
        p_name, p_imp = p_obj.get_imparter()

        listener.engine_builder.assign_object(ds_relay(relay, p_name, p_imp))
        r_obj = listener.engine_builder.get_object(f"{relay}.Relay")
        r_name, _ = r_obj.get_imparter()

        if not type(p_obj) == ds_system_object:
            p_obj.assign_end_signal(r_name, ds_status.F)
            p_obj.assign_clear_signal(r_name, ds_status.R)
            r_obj.assign_start_signal(p_name, ds_status.G)
            r_obj.assign_reset_signal(p_name, ds_status.H)

    def ds_tag_segment_assigner(parent, tag, need_segment = True, need_signals = True):
        if listener.engine_builder.check_object_in(f"{tag}.Tag"):
            if not need_signals == True:
                return
            temp_tag = listener.engine_builder.get_object(f"{tag}.Tag")
            temp_tag.assign_start_signal(parent, ds_status.G)
            temp_tag.assign_reset_signal(parent, ds_status.H)
            temp_rel = listener.engine_builder.get_object(parent)
            temp_rel.assign_end_signal(f"{tag}.Tag", ds_status.F)
            temp_rel.assign_clear_signal(f"{tag}.Tag", ds_status.R)
            rel_name, rel_imp = temp_rel.get_imparter()
            tag_name, tag_exc = temp_tag.get_exchanger()
            tag_exc.connect(tag_name, rel_name, rel_imp)
            return

        p_obj = listener.engine_builder.get_object(parent)
        p_name, p_imp = p_obj.get_imparter()

        listener.engine_builder.assign_object(ds_tag(tag, p_name, p_imp))

        if not need_signals == True:
            return

        t_obj = listener.engine_builder.get_object(f"{tag}.Tag")
        t_name, t_imp = t_obj.get_imparter()
        if not type(p_obj) == ds_system_object:
            p_obj.assign_end_signal(t_name, ds_status.F)
            p_obj.assign_clear_signal(t_name, ds_status.R)
            t_obj.assign_start_signal(p_name, ds_status.G)
            t_obj.assign_reset_signal(p_name, ds_status.H)
            
        if not need_segment:
            return

        s_name = tag
        if listener.engine_builder.check_object_in(s_name):
            return

        listener.engine_builder.assign_object(ds_segment(s_name, t_name, t_imp))
        s_obj = listener.engine_builder.get_object(s_name)
        t_obj.assign_end_signal(s_name, ds_status.F)
        t_obj.assign_clear_signal(s_name, ds_status.R)
        s_obj.assign_start_signal(t_name, ds_status.G)
        s_obj.assign_reset_signal(t_name, ds_status.H)

    def ds_expression_tag_assigner(expression, target_obj):
        print(expression)
        tags = set(parse_expr(expression))
        for c in '!@%&|GH':
            if c in tags:
                tags.remove(c)

        for tag in tags:
            tag = tag.replace(".Tag", "")
            tag = f"{tag}.Tag"
            tag_obj = listener.engine_builder.get_object(tag)
            _, tag_exc = tag_obj.get_exchanger()
            _, tag_imp = tag_obj.get_imparter()
            target_exc = target_obj.get_exchanger()
            target_exc[1].connect(target_exc[0], tag, tag_imp)
            target_exc[1].white_list.append(tag)
            target_exc[1].initialize_process = True
            tag_exc.initialize_process = True

    # nodes = map(nodeMapper, listener.nodes.values())
    edges = map(linkMapper, listener.links)

    print("assign system flow/task imparters")
    for n, nc in listener.nodes.items():
        if NodeType.system == nc.type:
            print("system :", n)
        elif NodeType.flow == nc.type or NodeType.task == nc.type:
            print(f"system object - {nc.type} : {n}")
            listener.engine_builder.assign_object(ds_system_object(n))
            ds_tag_segment_assigner(n, f"{n}.virtual_test", False, False)
            if not  f"{n}.virtual_test.Tag" in listener.engine_builder.get_outer_object_list():
                listener.engine_builder.assign_outer_object(f"{n}.virtual_test.Tag")
                v_test = listener.engine_builder.get_object(f"{n}.virtual_test.Tag")
                v_test.toggle_pausable_switch(False)
            print("----", f"{n}.virtual_test.Tag")
        elif not NodeType.func == nc.type and \
            not NodeType.conjunction == nc.type:
            if ';' in n:
                n = n.replace(";", "")
                nc.id = nc.id.replace(";", "")

            node_id = add_parent({'id':nc.id, 'parent':nc.parentId})
            new_id = node_id
            now_id = relay_tag_generater(new_id)['id']
            now_id_r = relay_name_extractor(now_id)
            now_id_t = tag_name_extractor(now_id)
            if not nc.type == NodeType.call:
                print("----", now_id_r, "/", now_id_t, "-", nc.type)
                ds_relay_assigner(nc.parentId, now_id_r)
                ds_tag_segment_assigner(f"{now_id_r}.Relay", now_id_t)
            else:
                print("----", nc.label, "/", nc.id, "-", nc.type)
                ds_tag_segment_assigner(nc.parentId, now_id_t, need_segment=False)
                call_segments[now_id_t] = nc.label.split('~')

            id_cnt_map[n] = 0
            to_use_id_map[n] = [n]
            final_reset_map[n] = []
            variance_id_map[n] = []

    print('relaying edges:')
    for e in edges:
        edge = e['edge_type']
        if not edge == 'include':
            src = f"{e['source']}"
            node_src = add_parent({'id':src, 'parent':e['parent']})
            new_src = node_src
            src = relay_tag_generater(new_src)['id']
            src_r = relay_name_extractor(src)
            src_t = tag_name_extractor(src)

            tgt = f"{e['target']}"
            node_tgt = add_parent({'id':tgt, 'parent':e['parent']})
            new_tgt = node_tgt
            tgt = relay_tag_generater(new_tgt)['id']
            tgt_r = relay_name_extractor(tgt)
            tgt_t = tag_name_extractor(tgt)

            if not src in tgt:
                sr_list = split_name_list(src_r)
                st_list = split_name_list(src_t)
                tr_list = split_name_list(tgt_r)
                tt_list = split_name_list(tgt_t)

                for idx_t, tr in enumerate(tr_list):
                    if not edge == 'reset':
                        variance_id_map[tr].append(tr)
                        if id_cnt_map[tr] > 0:
                            target_relay = f"{tr}__{id_cnt_map[tr]}"
                            to_use_id_map[tr].append(target_relay)
                            variance_id_map[tr].append(target_relay)
                        else:
                            target_relay = tr
                        id_cnt_map[tr] += 1

                    target_tag = tt_list[idx_t]
                    for idx_s, sr in enumerate(sr_list):
                        source_relay = sr
                        source_tag = st_list[idx_s]
                        selected, nt = relay_tag_selecter(st_list, idx_s, sr, e['parent'])
                        if nt == ds_object.Relay:
                            if not '(' in selected:
                                # relays
                                if edge == 'reset':
                                    final_selected = copy.deepcopy(to_use_id_map[selected])
                                    ss = selected if id_cnt_map[selected] <= 1 else f"{selected}__{id_cnt_map[selected] - 1}"
                                    tt = tr if id_cnt_map[tr] <= 1 else f"{tr}__{id_cnt_map[tr] - 1}"

                                    if not tt in final_reset_map:
                                        final_reset_map[tt] = []
                                    if not ss in variance_id_map[selected] and\
                                        not tt in variance_id_map[tr]:
                                        final_reset_map[tr].append(copy.deepcopy(selected))
                                    elif ss in variance_id_map[selected] and\
                                        not tt in variance_id_map[tr]:
                                        final_reset_map[tr].append(copy.deepcopy(ss))
                                    elif not ss in variance_id_map[selected] and\
                                        tt in variance_id_map[tr]:
                                        final_reset_map[tt].append(copy.deepcopy(selected))
                                    elif ss in variance_id_map[selected] and\
                                        tt in variance_id_map[tr]:
                                        final_reset_map[tt].append(copy.deepcopy(ss))
                                    
                                    variance_id_map[selected].append(ss)
                                    variance_id_map[tr].append(tt)
                                else:
                                    if len(to_use_id_map[selected]) > 0:
                                        if id_cnt_map[selected] == 1:
                                            final_selected = copy.deepcopy(selected)
                                        else:
                                            final_selected = copy.deepcopy(to_use_id_map[selected])
                                            
                                        to_use_id_map[selected].clear()
                                    else:
                                        final_selected = copy.deepcopy(f"{selected}__{id_cnt_map[selected]}")
                                        if type(final_selected) == str and id_cnt_map[selected] <= 1:
                                            final_selected = final_selected.split("__")[0]

                            else:
                                # expressions
                                nt = ds_object.Expression
                                final_selected = copy.deepcopy(selected)
                        else:
                            # tags
                            final_selected = copy.deepcopy(selected)
                        
                        if type(final_selected) == list and len(final_selected) == 1:
                            final_selected = final_selected[0]
                        if not edge == 'reset':
                            print(f"# {final_selected} - [{edge}] - {target_relay} - in parent : {e['parent']}")
                            if nt == ds_object.Relay:
                                ds_relay_assigner(e['parent'], final_selected)
                                ds_tag_segment_assigner(f"{final_selected}.Relay", source_tag)
                                ds_relay_assigner(e['parent'], target_relay)
                                ds_tag_segment_assigner(f"{target_relay}.Relay", target_tag)
                                tr_obj = listener.engine_builder.get_object(f"{target_relay}.Relay")
                                tr_obj.assign_start_signal(f"{final_selected}.Relay", ds_status.F)
                            elif nt == ds_object.Tag:
                                ds_relay_assigner(e['parent'], source_relay)
                                ds_tag_segment_assigner(f"{source_relay}.Relay", final_selected)
                                ds_relay_assigner(e['parent'], target_relay)
                                ds_tag_segment_assigner(f"{target_relay}.Relay", target_tag)
                                tr_obj = listener.engine_builder.get_object(f"{target_relay}.Relay")
                                tr_obj.assign_start_signal(f"{final_selected}.Tag", ds_status.F)
                                tr_name, tr_exc = tr_obj.get_exchanger()
                                tag_obj = listener.engine_builder.get_object(f"{final_selected}.Tag")
                                tag_name, tag_imp = tag_obj.get_imparter()
                                tr_exc.connect(tr_name, tag_name, tag_imp)
                            else:
                                ds_relay_assigner(e['parent'], target_relay)
                                ds_tag_segment_assigner(f"{target_relay}.Relay", target_tag)
                                tr_obj = listener.engine_builder.get_object(f"{target_relay}.Relay")
                                tr_obj.assign_start_expression(final_selected)
                                ds_expression_tag_assigner(final_selected, tr_obj)

    for id, var_list in variance_id_map.items():
        var_list = [[vl] if type(vl) == str else vl for vl in var_list]
        variance_id_map[id] = list(set(sum(var_list, [])))

    for id, rst_list in final_reset_map.items():
        rst_list = [[rl] if type(rl) == str else rl for rl in rst_list]
        final_reset_map[id] = list(set(itertools.chain.from_iterable(rst_list)))
        if not len(final_reset_map[id]) == 0:
            mutual_reset = False
            for mi in final_reset_map[id]:
                if mi in final_reset_map and id in final_reset_map[mi]:
                    mutual_reset = True

            if mutual_reset == True:
                sources = sum(
                    [variance_id_map[i.split("__")[0]] 
                    if len(variance_id_map[i.split("__")[0]]) > 0 
                    else [i] 
                    for i in final_reset_map[id]], []
                )
                targets = \
                    variance_id_map[id.split("__")[0]] \
                    if len(variance_id_map[id.split("__")[0]]) > 0 \
                    else [id]

                print(f"$ {sources} - [reset] - {targets}")
                for t in targets:
                    tr_obj = listener.engine_builder.get_object(f"{t}.Relay")
                    for s in sources:
                        tr_obj.assign_reset_signal(f"{s}.Relay", ds_status.G)
            else:
                print(f"$ {final_reset_map[id]} - [reset] - {[id]}")
                targets = [id]
                for t in targets:
                    tr_obj = listener.engine_builder.get_object(f"{t}.Relay")
                    for s in final_reset_map[id]:
                        tr_obj.assign_reset_signal(f"{s}.Relay", ds_status.G)

    print("fill start signal blanks:")
    new_tag_set = []
    final_dict = listener.engine_builder.get_object_list()
    for name, obj in final_dict.items():
        if not type(obj) == ds_system_object:
            _, now_exc = obj.get_exchanger()
            name_spliter = name.split('.')
            name_spliter.reverse()
            obj_type = name_spliter.pop(0)
            if obj_type == "Relay":
                if len(now_exc.start_signals) == 0 and len(now_exc.start_expression) == 0:
                    name_spliter.reverse()
                    new_start_tag = f"{'.'.join(name_spliter)}_starter"
                    parent = f"{name_spliter[0]}.{name_spliter[1]}"
                    new_tag_set.append((parent, new_start_tag, name))
    
    for tag_set in new_tag_set:
        ds_tag_segment_assigner(tag_set[0], tag_set[1], need_segment = False)
        starter = listener.engine_builder.get_object(f"{tag_set[1]}.Tag")
        target_relay = listener.engine_builder.get_object(tag_set[2])
        target_relay.assign_start_signal(f"{tag_set[1]}.Tag", ds_status.F)
        starter.assign_reset_signal(tag_set[2], ds_status.G)
        starter.toggle_pausable_switch(False)
        listener.engine_builder.assign_outer_object(f"{tag_set[1]}.Tag")
    
    new_tag_set.clear()
    for name, obj in final_dict.items():
        if not type(obj) == ds_system_object:
            no_start = False
            _, now_exc = obj.get_exchanger()
            name_spliter = name.split('.')
            name_spliter.reverse()
            obj_type = name_spliter.pop(0)
            if obj_type == "Tag":
                name_spliter.reverse()
                now_tag = f"{'.'.join(name_spliter)}"
                if now_tag in call_segments:
                    tg_st, tg_ed = call_segments[now_tag]
                    v_test = f"{now_tag.split('.')[0]}.{now_tag.split('.')[1]}.virtual_test.Tag"
                    if not tg_st == '_':
                        for ts in tg_st.split(','):
                            st_tag = listener.engine_builder.get_object(f"{ts}_starter.Tag")
                            st_tag.assign_start_signal(f"{now_tag}.Tag", ds_status.G)
                            st_tag.assign_reset_signal(f"{ts}.Relay", ds_status.G)
                            remote = listener.engine_builder.get_object(f"{now_tag}.Tag")
                            rmt_name, rmt_imp = remote.get_imparter()
                            stt_name, stt_exc = st_tag.get_exchanger()
                            stt_exc.connect(stt_name, rmt_name, rmt_imp)
                    else:
                        no_start = True

                    if not tg_ed == '_':
                        # 공통:
                        # end tag의 부모 relay쪽 reset 받는 부분이 없으면 virtual_resetter tag 만들고
                        # end relay reset expression에
                        # (virtual_test & virtual_resetter%&) 넣는다
                        # 자기 자신의 going은 finish 판정에 필요없으니 지운다
                        remote = listener.engine_builder.get_object(f"{now_tag}.Tag")
                        for te in tg_ed.split(','):
                            remote.assign_end_signal(f"{te}.Tag", ds_status.F)
                            remote.assign_clear_signal(f"{te}.Tag", ds_status.R)
                            listener.engine_builder.assign_outer_object(f"{te}.Tag")
                            ed_tag = listener.engine_builder.get_object(f"{te}.Tag")

                            rmt_name, rmt_imp = remote.get_imparter()
                            _, rmt_exc = remote.get_exchanger()
                            edt_name, edt_exc = ed_tag.get_exchanger()
                            edt_exc.connect(edt_name, rmt_name, rmt_imp)

                            if no_start == True:
                                # start 없으면 end tag에 다이렉트 연결
                                # end tag의 start expression에
                                # (system.virtual_test & 현재_tag%G) 넣는다
                                # 자기 자신의 going은 finish 판정에 필요없으니 지운다
                                tv_test = f"{te.split('.')[0]}.{te.split('.')[1]}.virtual_test"
                                tv_test_obj = listener.engine_builder.get_object(f"{tv_test}.Tag")
                                _, tv_imp = tv_test_obj.get_imparter()
                                exp = f"({tv_test}&{now_tag}%G)"
                                ed_tag.assign_start_expression(exp)
                                ds_expression_tag_assigner(exp, ed_tag)
                                edt_exc.connect(edt_name, f"{tv_test}.Tag", tv_imp)
                                edt_exc.white_list.append(f"{tv_test}.Tag")
                                edt_exc.white_list.append(rmt_name)

                            et_rel = listener.engine_builder.get_object(f"{te}.Relay")
                            _, et_rel_exc = et_rel .get_exchanger()
                            if len(et_rel_exc.reset_signals) == 0:
                                new_tag_set.append(
                                    (
                                        f"{te.split('.')[0]}.{te.split('.')[1]}", 
                                        f"{te}_virtual_resetter", 
                                        te, 
                                        f"{now_tag}.Tag"
                                    )
                                )

                            rmt_exc.end_signals.pop(rmt_name)
                    else:
                        # end 없으면 현재 tag의 end signal에
                        # (virtual_test & start_tag%G) 넣는다
                        remote = listener.engine_builder.get_object(f"{now_tag}.Tag")
                        for ts in tg_st.split(','):
                            remote.assign_end_signal(f"{ts}.Tag", ds_status.G)
                            remote.assign_clear_signal(f"{ts}.Tag", ds_status.R)
                        remote.assign_end_signal(v_test, ds_status.F)

    for tag_set in new_tag_set:
        ds_tag_segment_assigner(tag_set[0], tag_set[1], need_segment = False)
        target_relay = listener.engine_builder.get_object(f"{tag_set[2]}.Relay")
        target_relay.assign_reset_signal(f"{tag_set[1]}.Tag", ds_status.F)
        target_tag = listener.engine_builder.get_object(f"{tag_set[2]}.Tag")
        resetter = listener.engine_builder.get_object(f"{tag_set[1]}.Tag")
        exp = f"({tag_set[0]}.virtual_test&{tag_set[3]}%H&{tag_set[2]})"
        resetter.assign_start_expression(exp)
        ds_expression_tag_assigner(exp, resetter)
        resetter.assign_reset_signal(f"{tag_set[2]}.Relay", ds_status.H)
        resetter.toggle_pausable_switch(False)
        listener.engine_builder.assign_outer_object(f"{tag_set[1]}.Tag")
        listener.engine_builder.assign_outer_object(tag_set[3])
        remote = listener.engine_builder.get_object(tag_set[3])
        tr_name, tr_exc = target_relay.get_exchanger()
        tt_name, tt_exc = target_tag.get_exchanger()
        _, tt_imp = target_tag.get_imparter()
        rmt_name, rmt_imp = remote.get_imparter()
        rst_name, rst_exc = resetter.get_exchanger()
        _, rst_imp = resetter.get_imparter()
        tr_exc.connect(tr_name, rst_name, rst_imp)
        tr_exc.white_list.append(rst_name)
        tt_exc.connect(tt_name, rst_name, rst_imp)
        rst_exc.connect(rst_name, rmt_name, rmt_imp)
        rst_exc.connect(rst_name, tt_name, tt_imp)
        v_test = listener.engine_builder.get_object(f"{tag_set[0]}.virtual_test.Tag")
        v_name, v_imp = v_test.get_imparter()
        rst_exc.connect(rst_name, v_name, v_imp)
        rst_exc.initialize_process = True
        rst_exc.white_list.append(rmt_name)
        rst_exc.white_list.append(f"{tag_set[0]}.virtual_test.Tag")
        rst_exc.white_list.append(f"{tag_set[2]}.Relay")
        rst_exc.white_list.append(f"{tag_set[2]}.Tag")

    print(" - done")

    print("ds_object list:")
    for name, obj in final_dict.items():
        if not type(obj) == ds_system_object:
            _, now_exc = obj.get_exchanger()

            print(f"[{name}]")
            if len(now_exc.start_signals) > 0:
                print(
                    f"   start signal - {now_exc.start_signals}"
                )
            if len(now_exc.start_expression) > 0:
                print(
                    f"   start expression - {now_exc.start_expression}"
                )
            if len(now_exc.start_signals) == 0 and len(now_exc.start_expression) == 0:
                print(
                    f"   start ...... - {{}}"
                )
            print(
                f"   end signal - {now_exc.end_signals}",
                f"\n   reset signal - {now_exc.reset_signals}"
            )
            if len(now_exc.reset_expression) > 0:
                print(f"   reset expression - {now_exc.reset_expression}")
            print(
                f"   clear signal - {now_exc.clear_signals}",
                f"\n   imparters - {now_exc.imparter}"
            )

    return listener.engine_builder
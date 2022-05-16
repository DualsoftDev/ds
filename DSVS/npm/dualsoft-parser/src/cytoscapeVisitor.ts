import { ParserRuleContext } from "antlr4ts";
import { ErrorNode, ParseTree, ParseTreeWalker, TerminalNode } from "antlr4ts/tree";
import { dsListener } from "./dsListener";
import { CallContext, CausalOperatorContext, CausalPhraseContext, CausalTokenContext,
         CausalTokensCNFContext, CausalTokensDNFContext, dsParser, FlowContext,
         ListingContext, SystemContext, TaskContext
        } from "./dsParser"
        ;
import { assert } from "console";
import { enumerateChildren } from "./clientParser";
import { getAllParseRules } from "./allVisitor";


export interface ParserResult
{
    rules: ParserRuleContext[];
    terminals: TerminalNode[];
    errors: ErrorNode[];
}



type NodeType = "system" | "task" | "call" | "proc" | "func" | "segment" | "expression" | "conjunction";
export interface Node {
	id:string,
	label:string,
	parentId?:string,
	type:NodeType,
}

/**
 * Causal 관계를 표현하는 Link.  'A > B' 일 때, left = A, right = B, operator = '>'
 */
export interface CausalLink {
	l: Node,
	r: Node,
	op: string
}

/**
 * array of Node or Nodes
 * DNF (e.g A, B ? C) -> [[A, B], C]
 */
type Nodes = (Node | Node[])[];


/**
 * Parse tree 전체 순회
 */
class ElementsListener implements dsListener
{
    /** causal operator 왼쪽 */
    private left:CausalTokensDNFContext;
    private op:CausalOperatorContext;

    private systemName:string;
    private taskName:string;
    private flowName:string;        // [flow of A]F={..} -> F
    private flowOfName:string;      // [flow of A]F={..} -> A
    private allParserRules:ParseTree[];
    private multipleSystems:boolean;

    nodes:Map<string, Node> = new Map();
    links:CausalLink[] = [];

    constructor(parser:dsParser) {
        this.allParserRules = getAllParseRules(parser);
        parser.reset();

        this.multipleSystems = this.allParserRules.filter(t => t instanceof SystemContext).length > 1;
    }


    enterSystem(ctx: SystemContext) {
        const n = ctx.id().text;
        this.systemName = n;
        if (this.multipleSystems) {
            this.nodes.set(n, {id: n, label: n, type: "system"});
        }
    }
    exitSystem(ctx: SystemContext) {this.systemName = null;}
    
    enterTask(ctx: TaskContext) {
        const name = ctx.id().text;
        this.taskName = name;
        const id = `${this.systemName}.${name}`;
        this.nodes.set(id, {id, label: name, parentId: this.systemName, type: "task"});
    }
    exitTask(ctx: TaskContext) {this.taskName = null;}

    enterListing(ctx: ListingContext) {
        const name = ctx.id().text;
        const id = `${this.systemName}.${this.taskName}.${name}`;
        const node = {"data": { id, "label": name, "background_color": "gray", parent: this.taskName }};
        const parentId = `${this.systemName}.${this.taskName}`;
        this.nodes.set(id, {id, label: name, parentId, type: "segment"});
    }

    enterCall(ctx: CallContext) {
        const name = ctx.id().text;
        const label = `${name}\n${ctx.callPhrase().text}`;
        const parentId = `${this.systemName}.${this.taskName}`;
        const id = `${parentId}.${name}`;
        this.nodes.set(id, {id, label, parentId, type:"call"});
    }

    enterFlow(ctx: FlowContext) {
        const flowOf = ctx.flowProp().id();
        this.flowName = ctx.id().text;
        this.flowOfName = flowOf ? flowOf.text : null;
    }
    exitFlow(ctx: FlowContext){
        this.flowName = null;
        this.flowOfName = null;
    }
    

    enterCausalPhrase(ctx: CausalPhraseContext) {
        this.left = null;
        this.op = null;
    }
    enterCausalTokensDNF(ctx: CausalTokensDNFContext) {
        if (this.left) {
            assert(this.op, 'operator expected');

            // process operator
            this.processCausal(this.left, this.op, ctx);
        }

        this.left = ctx;
    }
    enterCausalOperator(ctx: CausalOperatorContext) {this.op=ctx;}


    // ParseTreeListener<> method
    visitTerminal (node: TerminalNode)     { return; }
    visitErrorNode(node: ErrorNode)        { return; }
    enterEveryRule(ctx: ParserRuleContext) { return; }
    exitEveryRule (ctx: ParserRuleContext) { return; }


    _existings:Map<CausalTokensDNFContext, Nodes> = new Map();
    private addNodes(ctx:CausalTokensDNFContext) : Nodes {
        if (this._existings.has(ctx))
            return this._existings.get(ctx);

        const cnfs =
            enumerateChildren(ctx, false, t => t instanceof CausalTokensCNFContext)
            .map(t => t as CausalTokensCNFContext)
            ;

        const dnfNodes:Nodes = [];
        for (const cnf of cnfs) {
            const cnfNodes:Node[] = [];
            enumerateChildren(cnf, false, t => t instanceof CausalTokenContext)
            .map(t => t as CausalTokenContext)
            .forEach(t => {
                const text = t.text;
                if (text.startsWith('#')) {
                    const node:Node = { id:text, label:text, type: "func" };
                    cnfNodes.push(node);
                }
                else if (text.startsWith('@')) {
                    const node:Node = { id:text, label:text, type: "proc" };
                    cnfNodes.push(node);
                }
                else
                {
                    // count number of '.' from text
                    const dotCount = text.split('.').length - 1;
                    let id:string = text;
                    const taskId = `${this.systemName}.${this.flowOfName}`;
                    let parentId = taskId;
                    switch(dotCount) {
                        case 0: id = `${taskId}.${text}`; break;
                        case 1:
                                id = `${this.systemName}.${text}`;
                                parentId = `${this.systemName}.${text.split('.')[0]}`;
                                break;
                    }
    
                    const node:Node = { id, label:text, type: "segment", parentId:taskId };
                    cnfNodes.push(node);
                }
                cnfNodes.forEach(n => {
                    if (! this.nodes.has(n.id))
                        this.nodes.set(n.id, n);
                });
            });
            dnfNodes.push(cnfNodes);
        }

        this._existings.set(ctx, dnfNodes);
        
        return dnfNodes;
    }

    /**
     * 
     * @param nodes : DNF nodes
     * @param append true (==nodes 가 sink) 인 경우, conjuction 생성.  false: 개별 node 나열 생성
     * @returns 
     */
    private getCnfTokens(nodes:Nodes, append=false):string[] {
        const cnfTokens:string[] = [];
        for (const x of nodes) {
            const isArray = Array.isArray(x) && x.length > 1;

            if (append && isArray) {
                const id = x.map(n => n.id).join(',');
                cnfTokens.push(id);
    
                const conj:Node = {id, label: "", parentId: this.taskName, type: "conjunction"};
                this.nodes.set(id, conj);

                for(const src of x) {
                    const s = this.nodes.get(src.id);
                    this.links.push({l:s, r:conj, op:"-"});
                }
            }
            else {
                if (isArray)
                    x.flatMap(n => n.id).forEach(id => cnfTokens.push(id));
                else
                    cnfTokens.push(x[0].id);
            }
        }

        return cnfTokens;
    }

    /**
     * 복합 Operator 를 분해해서 개별 operator array 로 반환
     * @param operator 복합 operator.  e.g "<||>"
     * @returns e.g [ "<|", "|>" ]
     */
    private splitOperator(operator:string): string[] {
        let op = operator;
        function *split() {
            for (const o of ['|>', '<|']) {
                if (op.includes(o))
                {
                    yield o;
                    op = op.replace(o, '');
                }
            }
            for (const o of ['>', '<']) {
                if (op.includes(o))
                {
                    yield o;
                    op = op.replace(o, '');
                }
            }
            if (op.length > 0)
                console.error("Error on causal operator:", operator);
        }
            
        return Array.from(split());
    }

    /**
     * causal operator 를 처리해서 this.links 에 결과 누적
     * @param l operator 왼쪽의 DNF
     * @param opr (복합) operator
     * @param r operator 우측의 DNF
     */
    private processCausal(l:CausalTokensDNFContext, opr:CausalOperatorContext, r:CausalTokensDNFContext) {        
        console.log(`${l.text} ${opr.text} ${r.text}`);
        const nodes = this.nodes;

        const ls = this.addNodes(l);
        const rs = this.addNodes(r);
        // for (const n of this.nodes.keys())
        //     console.log(n);


        const ops = this.splitOperator(opr.text);

        for (const op of ops) {
            const sinkToRight = op === '>' || op === '|>';
            const lss = this.getCnfTokens(ls, sinkToRight);
            const rss = this.getCnfTokens(rs, !sinkToRight);
    
            for (const strL of lss) {
                for (const strR of rss) {
                    const l = this.nodes.get(strL);
                    const r = this.nodes.get(strR);
                    assert(l&&r, 'node not found');
                    switch(op)
                    {
                        case '|>':
                        case '>': this.links.push({l, r, op}); break;
            
                        case '<|':
                        case '<': this.links.push({l:r, r:l, op}); break;
                        
                        default:
                            assert(false, `invalid operator: ${op}`);
                            break;
                    }
            
                }
            }
        }

        console.log('-----------------');
    }
}

/**
 * 전체 모델을 분석하여 grpah 생성을 위한 node 및 edge 구조를 생성한다.
 * @param parser model parser tree
 * @returns Graph elements (string)
 */
export function getElements(parser:dsParser): string
{
    const listener = new ElementsListener(parser);
    ParseTreeWalker.DEFAULT.walk(listener, parser.program());

    const nodes =
        Array.from(listener.nodes.values())
        .map(n => {

            let bg = 'green';
            let style = null;   // style override
            const classes = [n.type];
            switch(n.type)
            {
                case 'func': bg = 'springgreen'; style = {"shape": "rectangle"}; break;
                case 'system': bg = 'transparent'; style = {"shape": "rectangle"}; break;
                case 'proc': bg = 'lightgreen'; break;
                case 'task': bg = 'grey'; break;
                case 'call': bg = 'purple'; break;
                //case 'segment': break;
                case 'conjunction':
                    bg = 'beige'; style = {"shape": "rectangle", "width": 3, "height" : 3}; break;
            }

            return {"data": { id: n.id, label: n.label, parent:n.parentId, "background_color": bg}, style, classes };
        });

    // {"data":{"id":"MyElevatorSystem.B>A,B","source":"MyElevatorSystem.B","target":"A,B","line-style":"solid"}}
    const edges =
        listener.links.map(conn => {
            const [l, op, r] = [conn.l, conn.op, conn.r];
            const id = l.id + op + r.id;
            const lineStyle = op.includes('|') ? 'dashed' : 'solid';
            return {"data": {"id":id,"source":l.id, "target":r.id, "line-style":lineStyle}};
        });


    console.log('nodes:');
    nodes.forEach(n => console.log(JSON.stringify(n)));
    console.log('edges:');
    edges.forEach(e => console.log(JSON.stringify(e)));

    const elements =
        JSON.stringify([nodes, edges].flat());
        
    return elements;
}

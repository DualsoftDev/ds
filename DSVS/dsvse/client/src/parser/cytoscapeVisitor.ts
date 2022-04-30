import { ParserRuleContext, RuleContext } from "antlr4ts";
import { AbstractParseTreeVisitor, ErrorNode, ParseTree, ParseTreeWalker, TerminalNode } from "antlr4ts/tree";
import { dsListener } from "../server-bundle/dsListener";
import { dsVisitor } from "../server-bundle/dsVisitor";
import { CallContext, CausalOperatorContext, CausalPhraseContext, CausalTokenContext, CausalTokensCNFContext, CausalTokensDNFContext, dsParser, FlowContext, ListingContext, SystemContext, TaskContext } from "../server-bundle/dsParser";
import { assert } from "console";
import { CausalLink, enumerateChildren, Node } from "./clientParser";


export interface ParserResult
{
    rules: ParserRuleContext[];
    terminals: TerminalNode[];
    errors: ErrorNode[];
}

type Nodes = (Node | Node[])[];


/**
 * Parse tree 전체 순회
 */
class ElementsListener implements dsListener
{
    /** causal operator 왼쪽 */
    left:CausalTokensDNFContext;
    op:CausalOperatorContext;

    systemName:string;
    taskName:string;
    flowOfName:string;      // [flow of A] -> A

    nodes:Map<string, any> = new Map();
    links:CausalLink[] = [];


    enterSystem(ctx: SystemContext) {this.systemName = ctx.id().text;}
    exitSystem(ctx: SystemContext) {this.systemName = null;}
    
    enterTask(ctx: TaskContext) {
        const name = ctx.id().text;
        this.taskName = name;
        const id = `${this.systemName}.${name}`;
        const node = {"data": { id, label:name, "background_color": "gray", parent: this.systemName }};
    }
    exitTask(ctx: TaskContext) {this.taskName = null;}

    enterListing(ctx: ListingContext) {
        const name = ctx.id().text;
        const id = `${this.systemName}.${this.taskName}.${name}`;
        const node = {"data": { id, "label": name, "background_color": "gray", parent: this.taskName }};
        this.nodes.set(id, node);
    }

    enterCall(ctx: CallContext) {
        const name = ctx.id().text;
        const label = `${name}\n${ctx.callPhrase().text}`;
        const id = `${this.systemName}.${this.taskName}.${name}`;
        const node = {"data": { id, label, "background_color": "gray", parent: this.taskName }};
        this.nodes.set(id, node);
    }

    enterFlow(ctx: FlowContext) {
        const flowOf = ctx.flowProp().id();        
        this.flowOfName = flowOf ? flowOf.text : null;
    }
    exitFlow(ctx: FlowContext){this.flowOfName = null;}
    

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
                    switch(dotCount) {
                        case 0: id = `${taskId}.${text}`; break;
                        case 1: id = `${this.systemName}.${text}`; break;
                    }
    
                    const node:Node = { id, label:text, type: "segment" };
                    cnfNodes.push(node);
                }
                console.log(t.text);
                // if (! nodes.has()) {
                //     const node = {"data": { id: t, label: t, "background_color": "gray" }};
                //     this.nodes.set(t, node);
                // }        
            });
            dnfNodes.push(cnfNodes);
        }


        this._existings.set(ctx, dnfNodes);
        // add added to this.nodes
        for (const n of dnfNodes.flatMap(n => n)) {
            let parentId = null;
            if (n.type === "segment")
                parentId = `${this.systemName}.${this.taskName}`;

            const node = {"data": { id:n.id, "label": n.label, "background_color": "gray", parent: parentId }};
            this.nodes.set(n.id, node);
        }
        
        console.log('test me');
        return dnfNodes;
    }

    private getCnfTokens(nodes:Nodes, append=false):string[] {
        const cnfTokens:string[] = [];
        for (const x of nodes) {
            if (Array.isArray(x) && x.length > 1) {
                const id = x.map(n => n.id).join(',');
                cnfTokens.push(id);
                if (append)
                {
                    const data = {"data": { id, "label": "", "background_color": "gray" }};
                    this.nodes.set(id, data);
                }
            }
            else {
                cnfTokens.push(x[0].id);
            }
        }

        return cnfTokens;
    }

    private processCausal(l:CausalTokensDNFContext, opr:CausalOperatorContext, r:CausalTokensDNFContext) {        
        console.log(`${l.text} ${opr.text} ${r.text}`);
        const nodes = this.nodes;

        const ls = this.addNodes(l);
        const rs = this.addNodes(r);
        for (const n of this.nodes.keys())
            console.log(n);

        const lss = this.getCnfTokens(ls, true);
        const rss = this.getCnfTokens(rs, false);

        for (const strL of lss) {
            for (const strR of rss) {
                const l = this.nodes[strL];
                const r = this.nodes[strR];
                const op = opr.text;
                switch(op)
                {
                    case '|>':
                    case '>': this.links.push({l, r, op}); break;
        
                    case '<|':
                    case '<': this.links.push({r, l, op}); break;

        
                    case '<||>':
                    case '|><|':
                        this.links.push({l, r, op:'|>'});
                        this.links.push({r, l, op:'|>'});
                        break;
        
                    case '><|':
                        this.links.push({l, r, op:'>'});
                        this.links.push({r, l, op:'|>'});
                        break;

                    case '|><':
                        this.links.push({l, r, op:'|>'});
                        this.links.push({l:r, r: l, op:'>'});
                        break;
                
                    default:
                        assert(false, `invalid operator: ${op}`);
                        break;
                }
        
            }
        }

        console.log('-----------------');
    }
}


export function getElements(parser:dsParser)
{
    const listener = new ElementsListener();
    ParseTreeWalker.DEFAULT.walk(listener, parser.program());
    console.log('a');
}

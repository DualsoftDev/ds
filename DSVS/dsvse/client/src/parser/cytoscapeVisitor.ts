import { ParserRuleContext, RuleContext } from "antlr4ts";
import { AbstractParseTreeVisitor, ErrorNode, ParseTree, ParseTreeWalker, TerminalNode } from "antlr4ts/tree";
import { dsListener } from "../server-bundle/dsListener";
import { dsVisitor } from "../server-bundle/dsVisitor";
import { CallContext, CausalOperatorContext, CausalPhraseContext, CausalTokenContext, CausalTokensCNFContext, CausalTokensDNFContext, dsParser, FlowContext, ListingContext, SystemContext, TaskContext } from "../server-bundle/dsParser";
import { assert } from "console";
import { enumerateChildren } from "./clientParser";


export interface ParserResult
{
    rules: ParserRuleContext[];
    terminals: TerminalNode[];
    errors: ErrorNode[];
}

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



    _nodesAlreadyAdded:CausalTokensDNFContext[] = [];
    private addNodes(ctx:CausalTokensDNFContext) {
        if (this._nodesAlreadyAdded.includes(ctx))
            return;
        this._nodesAlreadyAdded.push(ctx);

        const cnfs =
            enumerateChildren(ctx, false, t => t instanceof CausalTokensCNFContext)
            .map(t => t as CausalTokensCNFContext);
        const tokens =
            enumerateChildren(ctx, false, t => t instanceof CausalTokenContext)
            .map(t => t as CausalTokenContext);

        tokens.forEach(t => {
            const text = t.text;
            if (text.startsWith('#') || text.startsWith('@')) {
                const node = {"data": { id:text, label:text, "background_color": "gray" }};
                this.nodes.set(text, node);
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

                const node = {"data": { id, label:text, "background_color": "gray", parent:taskId }};
                this.nodes.set(id, node);
            }
            console.log(t.text);
            // if (! nodes.has()) {
            //     const node = {"data": { id: t, label: t, "background_color": "gray" }};
            //     this.nodes.set(t, node);
            // }
        });
    }

    private processCausal(l:CausalTokensDNFContext, op:CausalOperatorContext, r:CausalTokensDNFContext) {        
        console.log(`${l.text} ${op.text} ${r.text}`);
        const nodes = this.nodes;

        this.addNodes(l);
        this.addNodes(r);

        for (const n of this.nodes.keys())
            console.log(n);
        console.log('-----------------');
    }
}


export function getElements(parser:dsParser)
{
    const listener = new ElementsListener();
    ParseTreeWalker.DEFAULT.walk(listener, parser.program());
    console.log('a');
}

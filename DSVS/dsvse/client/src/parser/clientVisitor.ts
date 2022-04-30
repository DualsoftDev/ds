// https://abcdabcd987.com/notes-on-antlr4/

/*
	- ISSUE: parser 의 tree 를 visit 할 때, 최초 visit 은 잘 성공하나, 한번 visit/listen 한 tree 를 재 방문하면 잘 안됨.
	- 임시 방편으로 text 로부터 매번 parsing 해서 새로 visit 함.
 */

import { CausalLink, Node } from "./clientParser";
import { CallContext, CausalContext, CausalOperatorContext, CausalPhraseContext, CausalsContext, CausalTokenContext,
		CausalTokensCNFContext, CausalTokensDNFContext, dsParser, FlowContext, ListingContext, MacroContext,
		ProgramContext, SystemContext, TaskContext
} from '../server-bundle/dsParser';

import { dsVisitor } from '../server-bundle/dsVisitor';
import { AbstractParseTreeVisitor } from 'antlr4ts/tree/AbstractParseTreeVisitor';
import { enumerateChildren, findFirstAncestor, } from "./clientParser";
import { assert } from "console";
import { ParserRuleContext } from "antlr4ts";
import { dsListener } from "../server-bundle/dsListener";
import { ParseTreeWalker } from 'antlr4ts/tree/ParseTreeWalker';
import { getAllParseRules } from "./allVisitor";

// https://www.antlr.org/api/Java/org/antlr/v4/runtime/tree/AbstractParseTreeVisitor.html
class LinkWalker extends AbstractParseTreeVisitor<number> implements dsVisitor<number> {
	public links:CausalLink[] = [];
	public nodes:Node[] = [];	// appended node.  '&' junction

	protected addLinks(systemName:string, left:CausalTokensDNFContext, op:CausalOperatorContext, right:CausalTokensDNFContext)
	{
		function getTokens(x:CausalTokensDNFContext) : CausalTokensCNFContext[]
		{
			return enumerateChildren(x, true, t => t instanceof CausalTokensDNFContext)
				.flatMap(t =>
					enumerateChildren(x, false, t => t instanceof CausalTokensCNFContext)
					.map(t => t as CausalTokensCNFContext)
				);
		}

		function getNode(n:string) {
			const node:Node = {id:n, label:n, type:"segment"};
			if (n.startsWith("@"))
				node.type = "proc";
			else if (n.startsWith("#"))
				node.type = "func";
			else
			{
				node.type = "segment";
				if (n.includes('.'))
				{
					// duplicated...
					node.id = n;
					node.parentId = n.split('.')[0];
					node.label = n.split('.')[1];
				}
				else
				{
					node.id = `${systemName}.${n}`;
					node.parentId = systemName;	
				}
			}

			return node;
		}

			
		// e.g 'C, Z ? D > A, B'
		// l = 'C, Z ? D'
		// r = 'A, B'
		// lss = [C, Z], [D]	// CNFs
		// rss = [A, B]		// CNFs
		const [lss, rss] = [getTokens(left), getTokens(right)];

		const cnfLs:Node[] = [];
		for (const ls of lss) {		// ls = [C, Z]
			const tokens = enumerateChildren(ls, false, t => t instanceof CausalTokenContext);
			if (tokens.length > 1)
			{
				const id = ls.text;// ls.map(l => l.text).join(',');
				const junction:Node = {id, label:id, type:"conjunction"};
				this.nodes.push(junction);
				tokens.forEach(l => {
					this.links.push({l:getNode(l.text), op:op.text, r:junction});
				});
				cnfLs.push(junction);
			}
			else
				cnfLs.push(getNode(tokens[0].text));
		}

		console.log('.');
		const cnfRs:Node[] =
			rss.flatMap(rs =>
				enumerateChildren(rs, false, t => t instanceof CausalTokenContext))
				.map(r => getNode(r.text))
				;

		cnfLs.forEach(l => {
			cnfRs.forEach(r => {
				this.links.push({l, op:op.text, r});
			});
		});

		console.log('good');
	}

	protected defaultResult(): number {
		return 0;
	}

	protected aggregateResult(aggregate: number, nextResult: number): number {
		return aggregate + nextResult;
	}

	// visitProgram(context: ProgramContext): number {
	// 	return 1 + super.visitChildren(context);
	// }	
	// // @Override
	// visitSystem(ctx: SystemContext): number { super.visitChildren(ctx); return 0; }

	// @Override
	//visitCausals?: (ctx: CausalsContext) => number;
	visitCausalPhrase(ctx: CausalPhraseContext): number {
		assert(ctx.childCount >= 3);
		const c = ctx.children;
		const system = findFirstAncestor(ctx, t => t instanceof SystemContext, false);
		const systemName = (system instanceof SystemContext) ? system.children[1].text : null;

		for (let i = 0; i < ctx.childCount - 2; i+=2) {
			const [l, op, r] = [c[i], c[i+1], c[i+2]];
			console.log(`[${l.text}] ${op.text} [${r.text}]`);
			if (   l  instanceof CausalTokensDNFContext
				&& r  instanceof CausalTokensDNFContext
				&& op instanceof CausalOperatorContext)
				this.addLinks(systemName, l, op, r);
			else
				assert(false, 'failed');
		}
		return 0;
	}
}


// class ParseTreeListener implements dsListener
// {
// 	public Contexts:ParserRuleContext[] = [];

// 	enterProgram(ctx: ProgramContext):void { this.Contexts.push(ctx); }
// 	enterSystem(ctx: SystemContext):void { this.Contexts.push(ctx); }
// 	enterMacro(ctx: MacroContext):void { this.Contexts.push(ctx); }
// 	enterTask(ctx: TaskContext):void { this.Contexts.push(ctx); }
// 	enterFlow(ctx: FlowContext):void { this.Contexts.push(ctx); }
// 	enterCausalTokensDNF(ctx: CausalTokensDNFContext):void { this.Contexts.push(ctx); }
// 	enterCausalTokensCNF(ctx: CausalTokensCNFContext):void { this.Contexts.push(ctx); }
// 	enterCausalPhrase(ctx: CausalPhraseContext):void { this.Contexts.push(ctx); }
// 	enterCausalOperator(ctx: CausalOperatorContext):void { this.Contexts.push(ctx); }
// 	enterCausal(ctx: CausalContext):void { this.Contexts.push(ctx); }
// 	enterCausals(ctx: CausalsContext):void { this.Contexts.push(ctx); }
// }

// function getParseRuleContext(parser:dsParser) {
// 	parser.reset();
// 	const listener_ = new ParseTreeListener();
// 	const listener:dsListener = listener_;
// 	parser.removeParseListeners();
// 	ParseTreeWalker.DEFAULT.walk(listener, parser.program());
// 	return listener_.Contexts;
// }

class ProgramListener implements dsListener
{
	public ProgramContext:ProgramContext = null;
	enterProgram(ctx: ProgramContext):void { this.ProgramContext = ctx; }
}
function getProgramContext(parser:dsParser) {
	parser.reset();
	const listener_ = new ProgramListener();
	const listener:dsListener = listener_;
	parser.removeParseListeners();
	ParseTreeWalker.DEFAULT.walk(listener, parser.program());
	return listener_.ProgramContext;
}



/** A = { B ~ C ~ D } */
interface CallDetail {
	name:string,	// call name : A
	detail:string,	// { B ~ C ~ D }
}
export interface SystemGraphInfo {
	name:string,
	calls: CallDetail[],
	segmentListings: string[],		// node 정의만 되어 있고, 실체가 정의되지 않은 것.  [sys]A = {B; C; D} 에서 B; C; D 의 경우
}

export interface TaskGraphInfo {
	name:string,
	segmentListings: string[],		// node 정의만 되어 있고, 실체가 정의되지 않은 것.  [sys]A = {B; C; D} 에서 B; C; D 의 경우
}


export function enumerateSystemInfos(parser:dsParser) : SystemGraphInfo[]
{
	const sysContexts =
		getAllParseRules(parser)
		.filter(p => p instanceof SystemContext)
		;

	const systemNameAndCalls =
		sysContexts
		.map(sc => {
			const sys = sc as SystemContext;	// syskey systemname = sysblock
			const name = sys.children[1].text;	// systemName
			const sysBlock = sys.children[3];	// sysblock
			const segmentListings =
				enumerateChildren(sysBlock, false, t => t instanceof ListingContext)
				.map(l => (l as ListingContext).children[0].text)
				;

			const calls =
				enumerateChildren(sysBlock, false, t => t instanceof CallContext)
				.map(c => {
					const call = c as CallContext;
					const callName = call.children[0].text;
					const callDetail = call.children[3].text;
					return {name:callName, detail:callDetail};
				});
			return {name, calls, segmentListings};
		});

	return systemNameAndCalls;	
}





/** DS parser tree 에서 인과 link (e.g, A > B) 부분 추출을 위한 visitor */
export function visitLinks(parser:dsParser): CausalLink[]
{
	console.log('visiting graph...');
    const visitor = new LinkWalker();
	getAllParseRules(parser)
		.filter(p => p instanceof CausalPhraseContext)
		.forEach(c => visitor.visit(c))
	;

	return visitor.links;
}


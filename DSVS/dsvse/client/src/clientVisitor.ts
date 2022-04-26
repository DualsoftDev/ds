// https://abcdabcd987.com/notes-on-antlr4/

/*
	- ISSUE: parser 의 tree 를 visit 할 때, 최초 visit 은 잘 성공하나, 한번 visit/listen 한 tree 를 재 방문하면 잘 안됨.
	- 임시 방편으로 text 로부터 매번 parsing 해서 새로 visit 함.
 */

import { CausalLink, Node, parserFromDocument } from "./clientParser";
import { CausalContext, CausalOperatorContext, CausalPhraseContext, CausalsContext, CausalTokenContext, CausalTokensCNFContext, CausalTokensDNFContext, dsParser, MacroContext, ProgramContext, SystemContext } from './server-bundle/dsParser';
import { dsVisitor } from './server-bundle/dsVisitor';
import { AbstractParseTreeVisitor } from 'antlr4ts/tree/AbstractParseTreeVisitor';
import { enumerateChildren, findFirstAncestor } from "./parseCausal";
import { assert } from "console";
import { ParserRuleContext } from "antlr4ts";
import { notDeepStrictEqual } from "assert";
import { dsListener } from "./server-bundle/dsListener";
import { ParseTree } from "antlr4ts/tree";
import { ParseTreeWalker } from 'antlr4ts/tree/ParseTreeWalker';


// https://www.antlr.org/api/Java/org/antlr/v4/runtime/tree/AbstractParseTreeVisitor.html
class LinkWalker extends AbstractParseTreeVisitor<number> implements dsVisitor<number> {
	public links:CausalLink[] = [];

	protected addLinks(systemName:string, l:CausalTokensDNFContext, op:CausalOperatorContext, r:CausalTokensDNFContext)
	{
		function getTokens(x:CausalTokensDNFContext)
		{
			return enumerateChildren(x, false, t => t instanceof CausalTokenContext)
				.flatMap(c => c.text)
				;
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

			

		const [ls, rs] = [getTokens(l), getTokens(r)];

		ls.forEach(ll => {
			rs.forEach(rr => {
				const [lll, ooo, rrr] = [getNode(l.text), op.text, getNode(r.text)];
				this.links.push({l:lll, op:ooo, r:rrr});
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


class ParseTreeListener implements dsListener
{
	public Contexts:ParserRuleContext[] = [];

	enterProgram(ctx: ProgramContext):void { this.Contexts.push(ctx); }
	enterSystem(ctx: SystemContext):void { this.Contexts.push(ctx); }
	enterMacro(ctx: MacroContext):void { this.Contexts.push(ctx); }
	enterCausalTokensDNF(ctx: CausalTokensDNFContext):void { this.Contexts.push(ctx); }
	enterCausalTokensCNF(ctx: CausalTokensCNFContext):void { this.Contexts.push(ctx); }
	enterCausalPhrase(ctx: CausalPhraseContext):void { this.Contexts.push(ctx); }
	enterCausalOperator(ctx: CausalOperatorContext):void { this.Contexts.push(ctx); }
	enterCausal(ctx: CausalContext):void { this.Contexts.push(ctx); }
	enterCausals(ctx: CausalsContext):void { this.Contexts.push(ctx); }
}

function getParseRuleContext(text:string) {
	const parser = parserFromDocument(text);
	const listener_ = new ParseTreeListener();
	const listener:dsListener = listener_;
	parser.removeParseListeners();
	ParseTreeWalker.DEFAULT.walk(listener, parser.program());
	return listener_.Contexts;
}



export function enumerateSystemNames(text:string)
{
	return getParseRuleContext(text)
		.filter(p => p instanceof SystemContext)
		.map(p => (p as SystemContext).children[1].text)
		;
}


/** DS parser tree 에서 인과 link (e.g, A > B) 부분 추출을 위한 visitor */
export function visitLinks(text:string)
{
	//visitor.visit(parser.program());

    const visitor = new LinkWalker();
	//enumerateChildren(parser.program())
	getParseRuleContext(text)
		.filter(p => p instanceof CausalPhraseContext)
		.forEach(c => visitor.visit(c))
	;

	return visitor.links;
}


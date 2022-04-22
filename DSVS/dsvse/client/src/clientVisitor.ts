// https://abcdabcd987.com/notes-on-antlr4/

import { CausalLink, parserFromDocument } from "./clientParser";
import { CausalContext, CausalOperatorContext, CausalPhraseContext, CausalsContext, CausalTokensCNFContext, CausalTokensDNFContext, dsParser, MacroContext, ProgramContext, SystemContext } from './server-bundle/dsParser';
import { dsVisitor } from './server-bundle/dsVisitor';
import { AbstractParseTreeVisitor } from 'antlr4ts/tree/AbstractParseTreeVisitor';
import { enumerateChildren } from "./parseCausal";
import { assert } from "console";

// https://www.antlr.org/api/Java/org/antlr/v4/runtime/tree/AbstractParseTreeVisitor.html
class ProgramTreeWalker extends AbstractParseTreeVisitor<number> implements dsVisitor<number> {
	public links:CausalLink[] = [];

	protected addLinks(l:CausalTokensDNFContext, op:CausalOperatorContext, r:CausalTokensDNFContext)
	{
		const getTokens = (x:CausalTokensDNFContext) =>
			Array.from(enumerateChildren(x, false, t => t instanceof CausalTokensCNFContext))
			.flatMap(c => c.text)
			;

		getTokens(l).forEach(l => {
			getTokens(r).forEach(r => this.links.push({l, op:op.text, r}));
		});

		console.log('good');
	}

	protected defaultResult(): number {
		return 0;
	}

	protected aggregateResult(aggregate: number, nextResult: number): number {
		return aggregate + nextResult;
	}

	visitProgram(context: ProgramContext): number {
		return 1 + super.visitChildren(context);
	}	
	// @Override
	visitSystem(ctx: SystemContext): number { super.visitChildren(ctx); return 0; }

	// @Override
	//visitCausals?: (ctx: CausalsContext) => number;
	visitCausalPhrase(ctx: CausalPhraseContext): number {
		assert(ctx.childCount >= 3);
		const c = ctx.children;
		for (let i = 0; i < ctx.childCount - 2; i+=2) {
			const [l, op, r] = [c[i], c[i+1], c[i+2]];
			console.log(`[${l.text}] ${op.text} [${r.text}]`);
			if (   l  instanceof CausalTokensDNFContext
				&& r  instanceof CausalTokensDNFContext
				&& op instanceof CausalOperatorContext)
				this.addLinks(l, op, r);
			else
				assert(false, 'failed');
		}
		return 0;
	}
}


export function visitDSDocument(text:string)
{
	//visitor.visit(parser.program());

	const parser = parserFromDocument(text);
    const visitor = new ProgramTreeWalker();
	Array.from(enumerateChildren(parser.program()))
		.filter(p => p instanceof CausalPhraseContext)
		.forEach(c => visitor.visit(c))
	;

	return visitor.links;
}


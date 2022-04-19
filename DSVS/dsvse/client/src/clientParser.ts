/*
 * DS language parsing/traversing 을 위한 코드
 */

import { ANTLRInputStream, RecognitionException, Recognizer, CharStream, Parser, CommonTokenStream } from 'antlr4ts';
import { ParseTree, ParseTreeListener, TerminalNode, ErrorNode } from 'antlr4ts/tree';
import { assert } from 'console';
import { link } from 'fs';
import { dsLexer } from './server-bundle/dsLexer';
import { dsParser, MacroContext, CausalContext, CausalExpressionContext, ExpressionContext, ProgramContext, SystemContext } from './server-bundle/dsParser';
// import { dsVisitor } from './dsVisitor';
// import { AbstractParseTreeVisitor } from 'antlr4ts/tree/AbstractParseTreeVisitor';
//import { SimpleSysBlockContext, ComplexSysBlockContext } from './dsParser';
// import {ANTLRErrorListener} from 'antlr4ts/ANTLRErrorListener';
// import {RecognitionException, Recognizer} from 'antlr4ts';
// import { Diagnostic, DiagnosticSeverity,} from 'vscode-languageserver/node';
// import {
// 	TextDocument, Position
// } from 'vscode-languageserver-textdocument';

/**
 * Parse Macro contexts
 * @param macros Macro contexts defined in a system context.
 */
 function parseMacro(macros:MacroContext[]): void {
	for(const macro of macros) {
		console.log(`\theader: ${macro.macroHeader().text}`);
		for(const call of macro.call()) {
			console.log(`\tcall: ${call.text}`);
		}
	}
}

function parserFromDocument(text:string) {
	// Create the lexer and parser
	const inputStream = new ANTLRInputStream(text);
	const lexer = new dsLexer(inputStream);
	const tokenStream = new CommonTokenStream(lexer);
	return new dsParser(tokenStream);
}

interface Link {
	l: string,
	r: string,
	op: string
}

type Token = string;

function getTerminalTokens(exp: ParseTree): Token[]
{
	if (exp instanceof ExpressionContext)
	{
		const lexp = exp as ExpressionContext;
		return lexp.children
			.filter(c => ! (c instanceof TerminalNode))
			.map(c => c.text)
			;
	}
	else if (exp instanceof CausalExpressionContext)
	{
		const cexp = exp as CausalExpressionContext;
		assert(cexp.causalOperator() == null);
		return cexp.segments().children
			.filter(c => ! (c instanceof TerminalNode))
			.map(c => c.text)
			;
	}
}

function parseExpressionContext(exp: ParseTree, links:Link[]) : Token[]
{
	function getLeftOpRight(exp:CausalContext | CausalExpressionContext | ExpressionContext)
	{
		return [exp.children[0], exp.children[1], exp.children[2]];
		// const lexp = exp as ExpressionContext;
		// const cexp = exp as CausalExpressionContext;
		// const causal = exp as CausalContext;
		// if (causal)
		// 	return causal.children[0], causal.causalOperator().text, causal.children[2];	
	}


	function addLinks(ls:string[], op:string, rs:string[], links:Link[]) : void
	{
		ls.forEach(l => {
			rs.forEach(r => {
				links.push({l, r, op});
			});
		});
	}
	

	if (exp instanceof CausalExpressionContext || exp instanceof CausalContext)
	{
		const cexp = exp as CausalExpressionContext;
		if (exp instanceof CausalExpressionContext && cexp.segments() != null)
			return cexp.segments()
				.children
				.filter(c => ! (c instanceof TerminalNode))
				.map(c => c.text)
				;
		else
		{
			const [l, op_, r] = getLeftOpRight(exp);
			// const children = cexp.children;
			// const l = children[0];
			// const op = children[1].text;
			// const r = children[2];
			/// 왼쪽 트리의 마지막 token 들
			const lTokens = parseExpressionContext(l, links);
			const rTokens = getTerminalTokens(r);
			const op = op_.text;
			switch(op) {
				case '>':
				case '|>':
					addLinks(lTokens, op, rTokens, links);
					break;
				case '>|>':
				case '|>>':
					addLinks(lTokens, '>', rTokens, links);
					addLinks(lTokens, '|>', rTokens, links);
					break;

				case '<':
				case '<|':
					addLinks(rTokens, op, lTokens, links);
					break;

				case '<||>':
					addLinks(lTokens, '|>', rTokens, links);
					addLinks(rTokens, '<|', lTokens, links);
					break;
			}

			return rTokens;
		}
	}
	else if (exp instanceof ExpressionContext)
	{
		return [exp.text];
		// const lexp = exp as ExpressionContext;
		// if (exp.segment() != null)
		// 	return [exp.segment().text];

		// return exp.seg

	}
	else
		assert(false, "type match error");
}


/**
 * Parse DS model text
 * @param text DS model document obeying DS language rule.
 */
export function *parseDSDocument(text:string) {
	console.log('In client parsing module.', text);
	const parser = parserFromDocument(text);


	// Parse the input, where `compilationUnit` is whatever entry point you defined
	const tree = parser.program();
	for (const system of tree.system()) {
		const sysBlock = system.sysBlock();
		const simple = sysBlock.simpleSysBlock();
		const complex = sysBlock.complexSysBlock();

		if (simple)
			console.log(`system: ${system.text}`);
		else if (complex)
		{
			console.log('========');
			for (const acc of complex.acc())
				console.log(`\tacc: ${acc.text}`);

			parseMacro(complex.macro());
			for (const macro of complex.macro())
				console.log(`\tmacro: ${macro.text}`);
			for (const causal of complex.causal())
			{
				if (causal.exception != null)
				{
					console.error(`Exception: ${causal.text} : ${causal.exception.toString()}`);
					continue;
				}

				console.log(`\tcausal: ${causal.text}`);
				const links:Link[] = [];
				parseExpressionContext(causal, links);
				for(const l of links)
					yield l;				
			}

			console.log(`system: ${system.text}`);
		}			
	}
	// const visitor = new ProgramTreeWalker();
	// const count = visitor.visit(tree);
	// console.log(`${count} systems: ${tree.system()..text}`);
	// console.log(tree);
}


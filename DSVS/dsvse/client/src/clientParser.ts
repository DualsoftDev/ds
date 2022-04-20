/*
 * DS language parsing/traversing 을 위한 코드
 */

import { ANTLRInputStream, RecognitionException, Recognizer, CharStream, Parser, ParserRuleContext, CommonTokenStream } from 'antlr4ts';
import { ParseTree, ParseTreeListener, TerminalNode, ErrorNode } from 'antlr4ts/tree';
import { assert } from 'console';
import { link } from 'fs';
import { dsLexer } from './server-bundle/dsLexer';
import { dsParser, MacroContext, CausalContext, CausalExpressionContext, ExpressionContext, ProcContext, ProgramContext, SystemContext, ProcSleepMsContext } from './server-bundle/dsParser';
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

function isTerminalNode(ctx:ParseTree) {
	if (ctx instanceof TerminalNode || ctx instanceof ExpressionContext || ctx instanceof ProcContext)
		return true;
	if (ctx instanceof ParserRuleContext && ctx.childCount == 1)
		return isTerminalNode(ctx.getChild(0));

	return false;
}


type Token = string;
function getTerminalTokens(exp: ParseTree): Token[]
{
	assert(isTerminalNode(exp), 'Expected terminal node');
	if (exp instanceof ExpressionContext)
	{
		const lexp = exp as ExpressionContext;
		return lexp.children
			.filter(c => ! (c instanceof TerminalNode))
			.map(c => c.text)
			;
	}
	else if (exp instanceof CausalExpressionContext && exp.childCount == 1 && exp.children[0] instanceof ProcContext)
		return getSegmentTokens(exp.children[0].children[0]);

	return getSegmentTokens(exp);
}

function getSegmentTokens(exp: ParseTree)
{
	if (exp instanceof CausalExpressionContext && exp.segments() != null)
		return exp.segments()
			.children
			.filter(c => ! (c instanceof TerminalNode))
			.map(c => c.text)
			;
	else if (exp instanceof ProcSleepMsContext)
		return [exp.text];
	return null;
}


function _getDeepTokens(exp: ParseTree, leftmost:boolean)
{
	if (exp instanceof CausalExpressionContext || exp instanceof CausalContext)
	{
		const segments = getSegmentTokens(exp);
		if (segments)
			return segments;
	
		const [l, op_, r] = [exp.children[0], exp.children[1], exp.children[2]];
		return _getDeepTokens(leftmost ? l : r, leftmost);
	}
	else if (exp instanceof ExpressionContext)
		return [exp.text];
	else
		return null;
}

const getLeftmostTokens = (exp) => _getDeepTokens(exp, true);
const getRightmostTokens = (exp) => _getDeepTokens(exp, false);


function parseExpressionContext(exp: ParseTree, links:Link[]) : Token[]
{
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
		const segments = getSegmentTokens(exp);
		if (segments)
			return segments;
		else
		{
			const [l, op_, r] = [exp.children[0], exp.children[1], exp.children[2]];
			const x = isTerminalNode(l);
			const y = isTerminalNode(r);
			let lTokens:string[] = null;
			let rTokens:string[] = null;

			if (isTerminalNode(l))
			{
				/// 왼쪽 트리의 마지막 token 들
				lTokens = getTerminalTokens(l);
				parseExpressionContext(r, links);
				rTokens = getLeftmostTokens(r);
			}
			else
			{
				/// 왼쪽 트리의 마지막 token 들
				lTokens = getRightmostTokens(l);
				parseExpressionContext(l, links);
				rTokens = getTerminalTokens(r);
			}
			const op = op_.text;
			switch(op) {
				case '>':
				case '<':
				case '|>':
				case '<|':
					addLinks(lTokens, op, rTokens, links);
					break;

				case '>|>':
				case '|>>':
					addLinks(lTokens, '>', rTokens, links);
					addLinks(lTokens, '|>', rTokens, links);
					break;


				case '<|<':
				case '<<|':
					addLinks(lTokens, '<', rTokens, links);
					addLinks(lTokens, '<|', rTokens, links);
					break;


				case '<||>':
					addLinks(lTokens, '|>', rTokens, links);
					addLinks(lTokens, '<|', rTokens, links);
					break;
			}
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


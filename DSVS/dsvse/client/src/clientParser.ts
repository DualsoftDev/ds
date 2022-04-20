/*
 * DS language parsing/traversing 을 위한 코드
 */

import { ANTLRInputStream, RecognitionException, Recognizer, CharStream, Parser, ParserRuleContext, CommonTokenStream } from 'antlr4ts';
import { ParseTree, ParseTreeListener, TerminalNode, ErrorNode } from 'antlr4ts/tree';
import { assert } from 'console';
import { link } from 'fs';
import { dsLexer } from './server-bundle/dsLexer';
import { dsParser, MacroContext, CausalContext, CausalExpressionContext, ExpressionContext, ProcContext, ProgramContext, SystemContext, ProcSleepMsContext } from './server-bundle/dsParser';


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

/**
 * DS 문서로부터 parser 객체를 생성해서 반환
 * @param text DS 문서
 */
function parserFromDocument(text:string) {
	// Create the lexer and parser
	const inputStream = new ANTLRInputStream(text);
	const lexer = new dsLexer(inputStream);
	const tokenStream = new CommonTokenStream(lexer);
	return new dsParser(tokenStream);
}

/**
 * Causal 관계를 표현하는 Link.  'A > B' 일 때, left = A, right = B, operator = '>'
 */
interface CausalLink {
	l: string,
	r: string,
	op: string
}

/** 주어진 context 가 terminal ({ TerminalNode, ExpressionContext, ProcContext } 중 하나)인지 여부 */
function isTerminalNode(ctx:ParseTree) {
	if (ctx instanceof TerminalNode || ctx instanceof ExpressionContext || ctx instanceof ProcContext)
		return true;
	if (ctx instanceof ParserRuleContext && ctx.childCount == 1)
		return isTerminalNode(ctx.getChild(0));

	return false;
}


type Token = string;
/**
 * Expression 의 terminal 에 해당하는 부분의 token 을 반환
 * @param exp (Causal)Expression or Proc(@)
 */
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


/** 주어진 expression 에서 segment 이름을 추출하여 배열로 반환 */
function getSegmentTokens(exp: ParseTree)
{
	if (exp instanceof CausalExpressionContext && exp.segments())
		return exp.segments()
			.children
			.filter(c => ! (c instanceof TerminalNode))
			.map(c => c.text)
			;
	else if (exp instanceof ProcSleepMsContext)
		return [exp.text];
	return null;
}


/**
 * 주어진 expression 에서 operator 를 기준으로 왼쪽/오른쪽 방향으로 가장 깊은 terminal 의 tokens 문자 배열 검색해서 반환
 * - operator 가 존재하지 않는 terminal 일 경우, termianl 자체를 반환
 * @param exp 탐색 대상 expression
 * @param leftmost 탐색 방향
 * @returns 검색된 token 의 문자 배열
 */
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

/**
 * 주어진 expression 에서 operator 를 기준으로 왼쪽으로 가장 깊은 terminal 의 tokens 문자 배열 검색해서 반환.
 * - @see _getDeepTokens
 */
const getLeftmostTokens = (exp) => _getDeepTokens(exp, true);
/**
 * 주어진 expression 에서 operator 를 기준으로 오른쪽으로 가장 깊은 terminal 의 tokens 문자 배열 검색해서 반환.
 * - @see _getDeepTokens
 */
 const getRightmostTokens = (exp) => _getDeepTokens(exp, false);

/**
 * causal expression 을 분석해서, CausalLink[] 를 반환
 * @param exp : {Causal and/or Expression} Context
 */
function parseCausalExpressionContext(exp: ParseTree) : CausalLink[]
{
	const links:CausalLink[] = [];
	parseCausalExpressionContext_(exp, links);
	return links;
}

function parseCausalExpressionContext_(exp: ParseTree, links:CausalLink[]) : void
{
	function addLinks(ls:string[], op:string, rs:string[], links:CausalLink[]) : void
	{
		ls.forEach(l => {
			rs.forEach(r => {
				links.push({l, r, op});
			});
		});
	}


	if (exp instanceof ExpressionContext)
		return;

	if (exp instanceof CausalExpressionContext || exp instanceof CausalContext)
	{
		const segments = getSegmentTokens(exp);
		if (! segments)
		{
			const [l, op_, r] = [exp.children[0], exp.children[1], exp.children[2]];
			let lTokens:string[] = null;
			let rTokens:string[] = null;

			if (isTerminalNode(l))
			{
				lTokens = getTerminalTokens(l);	// 좌측이 terminal
				parseCausalExpressionContext_(r, links);
				rTokens = getLeftmostTokens(r);
			}
			else
			{
				lTokens = getRightmostTokens(l);
				parseCausalExpressionContext_(l, links);
				rTokens = getTerminalTokens(r);	// 우측이 terminal
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
	else
		assert(false, "type match error");
}


/**
 * Parse DS model text.  Causal 분석해서 Link 를 생성하는 generator
 * @param text DS model document obeying DS language rule.
 * @see CausalLink
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
				yield* parseCausalExpressionContext(causal);
			}

			console.log(`system: ${system.text}`);
		}			
	}
	// const visitor = new ProgramTreeWalker();
	// const count = visitor.visit(tree);
	// console.log(`${count} systems: ${tree.system()..text}`);
	// console.log(tree);
}


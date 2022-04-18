/*
 * DS language parsing/traversing 을 위한 코드
 */

import { ANTLRInputStream, RecognitionException, Recognizer, CharStream, CommonTokenStream } from 'antlr4ts';
import { dsLexer } from './server-bundle/dsLexer';
import { dsParser, MacroContext, ProgramContext, SystemContext } from './server-bundle/dsParser';
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


/**
 * Parse DS model text
 * @param text DS model document obeying DS language rule.
 */
export function parseDSDocument(text:string) {
	console.log('In parsing module.');
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
				console.log(`\tcausal: ${causal.text}`);

			console.log(`system: ${system.text}`);
		}			
	}
	// const visitor = new ProgramTreeWalker();
	// const count = visitor.visit(tree);
	// console.log(`${count} systems: ${tree.system()..text}`);
	// console.log(tree);
}


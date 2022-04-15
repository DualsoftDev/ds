/*
 * DS language parsing/traversing 을 위한 코드
 */

import { ANTLRInputStream, CharStream, CommonTokenStream } from 'antlr4ts';
import { dsLexer } from './dsLexer';
import { dsParser, MacroContext, ProgramContext, SystemContext } from './dsParser';
// import { dsVisitor } from './dsVisitor';
// import { AbstractParseTreeVisitor } from 'antlr4ts/tree/AbstractParseTreeVisitor';
//import { SimpleSysBlockContext, ComplexSysBlockContext } from './dsParser';
import {ANTLRErrorListener} from 'antlr4ts/ANTLRErrorListener';
import {RecognitionException, Recognizer} from 'antlr4ts';
import { Diagnostic, DiagnosticSeverity,} from 'vscode-languageserver/node';
import {
	TextDocument, Position
} from 'vscode-languageserver-textdocument';

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


// { https://segmentfault.com/a/1190000040176753/en
export default class DsErrorListener implements ANTLRErrorListener<any>{
    private errors: Diagnostic[] = [];
    syntaxError(recognizer: Recognizer<any, any>, offendingSymbol: any, line: number, charPositionInLine: number, message: string, e: RecognitionException | undefined): void {
        
        this.errors.push(
            {
				severity: DiagnosticSeverity.Warning,
				range: {
					start: {line:line, character:charPositionInLine},
					end: {line:line, character:charPositionInLine + 5},
				},
				message,
				source: 'ex'	
            }
        );
    }

    getErrors() { return this.errors; }
}


export function diagnoseDSDocument(text:string, onError:(diagnostic:{line:number, position:number, length:number, message:string}) => void) {
	const parser = parserFromDocument(text);

	// https://github.com/tunnelvisionlabs/antlr4ts/issues/430
	parser.removeErrorListeners();
	// parser.addErrorListener(dsErrorsListner);

	parser.addErrorListener({
        syntaxError: <Token>(
			recognizer: Recognizer<Token, any>,
			offendingSymbol: Token | undefined,
			line: number, charPositionInLine: number,
			msg: string, e: RecognitionException | undefined): void =>
		{
			console.log(e);
			console.log(`${offendingSymbol} in ${line}:${charPositionInLine} : ${msg}`);
			const offending:any = offendingSymbol;
			const pseudoDiagnostic = {
				line: line-1,
				position: charPositionInLine,
				length: offending.text.length,
				message: msg
			};
		
			console.log(pseudoDiagnostic);
			onError(pseudoDiagnostic);
        },
    });


	/// force span all the parser to collect errors
	const _ = parser.program();
	console.log('Parsed..');
}
/**
 * Parse DS model text
 * @param text DS model document obeying DS language rule.
 */
export function parseDSDocument(text:string) {
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


// class ProgramTreeWalker extends AbstractParseTreeVisitor<number> implements dsVisitor<number> {
// 	protected defaultResult(): number {
// 		return 0;
// 	}

// 	protected aggregateResult(aggregate: number, nextResult: number): number {
// 		return aggregate + nextResult;
// 	}

// 	visitProgram(context: ProgramContext): number {
// 		return 1 + super.visitChildren(context);
// 	}	
// 	//visitSystem?: ((ctx: SystemContext) => number) | undefined;
// }
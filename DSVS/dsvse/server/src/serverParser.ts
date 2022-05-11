/*
 * DS language parsing/traversing 을 위한 코드
 */

import { ANTLRInputStream, CharStream, CommonTokenStream } from 'antlr4ts';
import { dsLexer } from '@dualsoft/parser/dsLexer';
import { dsParser, MacroContext, ProgramContext, SystemContext } from '@dualsoft/parser/dsParser';
// import { dsVisitor } from './dsVisitor';
// import { AbstractParseTreeVisitor } from 'antlr4ts/tree/AbstractParseTreeVisitor';
//import { SimpleSysBlockContext, ComplexSysBlockContext } from './dsParser';
// import {ANTLRErrorListener} from 'antlr4ts/ANTLRErrorListener';
import {RecognitionException, Recognizer} from 'antlr4ts';

/*
 * 이 파일은 client 에서도 공용으로 code 복사해서 사용할 것이므로, language server 와 관련된 내용은 
 * 추가하면, client 쪽 build 가 되지 않음.
 */
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


// // { https://segmentfault.com/a/1190000040176753/en
// export default class DsErrorListener implements ANTLRErrorListener<any>{
//     private errors: Diagnostic[] = [];
//     syntaxError(recognizer: Recognizer<any, any>, offendingSymbol: any, line: number, charPositionInLine: number, message: string, e: RecognitionException | undefined): void {
        
//         this.errors.push(
//             {
// 				severity: DiagnosticSeverity.Warning,
// 				range: {
// 					start: {line:line, character:charPositionInLine},
// 					end: {line:line, character:charPositionInLine + 5},
// 				},
// 				message,
// 				source: 'ex'	
//             }
//         );
//     }

//     getErrors() { return this.errors; }
// }


/**
 * 
 * @param text DS document contents
 * @param onError Error 발생시 수행할 함수
 */
export function diagnoseDSDocument(text:string, onError:(diagnostic:any) => void) {
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
}


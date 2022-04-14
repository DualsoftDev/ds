import { ANTLRInputStream, CharStream, CommonTokenStream } from 'antlr4ts';
import { dsLexer } from './dsLexer';
import { dsParser, ProgramContext, SystemContext } from './dsParser';
import { dsVisitor } from './dsVisitor';
import { AbstractParseTreeVisitor } from 'antlr4ts/tree/AbstractParseTreeVisitor';

class ProgramTreeWalker extends AbstractParseTreeVisitor<number> implements dsVisitor<number> {
	protected defaultResult(): number {
		return 0;
	}

	protected aggregateResult(aggregate: number, nextResult: number): number {
		return aggregate + nextResult;
	}

	visitProgram(context: ProgramContext): number {
		return 1 + super.visitChildren(context);
	}	
	//visitSystem?: ((ctx: SystemContext) => number) | undefined;
}

export function testParseText(text:string) {
	// Create the lexer and parser
	const inputStream = new ANTLRInputStream(text);
	const lexer = new dsLexer(inputStream);
	const tokenStream = new CommonTokenStream(lexer);
	const parser = new dsParser(tokenStream);


	// Parse the input, where `compilationUnit` is whatever entry point you defined
	const tree = parser.program();
	for (const system of tree.system()) {
		console.log(`system: ${system.text}`);
	}
	// const visitor = new ProgramTreeWalker();
	// const count = visitor.visit(tree);
	// console.log(`${count} systems: ${tree.system()..text}`);
	// console.log(tree);
}



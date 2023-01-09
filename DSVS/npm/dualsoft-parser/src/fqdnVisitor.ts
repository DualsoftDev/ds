// Generated from ../../../Grammar/g4s/fqdn.g4 by ANTLR 4.9.0-SNAPSHOT


import { ParseTreeVisitor } from "antlr4ts/tree/ParseTreeVisitor";

import { FqdnsContext } from "./fqdnParser";
import { FqdnContext } from "./fqdnParser";
import { IdContext } from "./fqdnParser";
import { QidContext } from "./fqdnParser";
import { NameComponentContext } from "./fqdnParser";


/**
 * This interface defines a complete generic visitor for a parse tree produced
 * by `fqdnParser`.
 *
 * @param <Result> The return type of the visit operation. Use `void` for
 * operations with no return type.
 */
export interface fqdnVisitor<Result> extends ParseTreeVisitor<Result> {
	/**
	 * Visit a parse tree produced by `fqdnParser.fqdns`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitFqdns?: (ctx: FqdnsContext) => Result;

	/**
	 * Visit a parse tree produced by `fqdnParser.fqdn`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitFqdn?: (ctx: FqdnContext) => Result;

	/**
	 * Visit a parse tree produced by `fqdnParser.id`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitId?: (ctx: IdContext) => Result;

	/**
	 * Visit a parse tree produced by `fqdnParser.qid`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitQid?: (ctx: QidContext) => Result;

	/**
	 * Visit a parse tree produced by `fqdnParser.nameComponent`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitNameComponent?: (ctx: NameComponentContext) => Result;
}


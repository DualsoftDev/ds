// Generated from ../../../Grammar/g4s/fqdn.g4 by ANTLR 4.9.0-SNAPSHOT


import { ParseTreeListener } from "antlr4ts/tree/ParseTreeListener";

import { FqdnsContext } from "./fqdnParser";
import { FqdnContext } from "./fqdnParser";
import { IdContext } from "./fqdnParser";
import { QidContext } from "./fqdnParser";
import { NameComponentContext } from "./fqdnParser";


/**
 * This interface defines a complete listener for a parse tree produced by
 * `fqdnParser`.
 */
export interface fqdnListener extends ParseTreeListener {
	/**
	 * Enter a parse tree produced by `fqdnParser.fqdns`.
	 * @param ctx the parse tree
	 */
	enterFqdns?: (ctx: FqdnsContext) => void;
	/**
	 * Exit a parse tree produced by `fqdnParser.fqdns`.
	 * @param ctx the parse tree
	 */
	exitFqdns?: (ctx: FqdnsContext) => void;

	/**
	 * Enter a parse tree produced by `fqdnParser.fqdn`.
	 * @param ctx the parse tree
	 */
	enterFqdn?: (ctx: FqdnContext) => void;
	/**
	 * Exit a parse tree produced by `fqdnParser.fqdn`.
	 * @param ctx the parse tree
	 */
	exitFqdn?: (ctx: FqdnContext) => void;

	/**
	 * Enter a parse tree produced by `fqdnParser.id`.
	 * @param ctx the parse tree
	 */
	enterId?: (ctx: IdContext) => void;
	/**
	 * Exit a parse tree produced by `fqdnParser.id`.
	 * @param ctx the parse tree
	 */
	exitId?: (ctx: IdContext) => void;

	/**
	 * Enter a parse tree produced by `fqdnParser.qid`.
	 * @param ctx the parse tree
	 */
	enterQid?: (ctx: QidContext) => void;
	/**
	 * Exit a parse tree produced by `fqdnParser.qid`.
	 * @param ctx the parse tree
	 */
	exitQid?: (ctx: QidContext) => void;

	/**
	 * Enter a parse tree produced by `fqdnParser.nameComponent`.
	 * @param ctx the parse tree
	 */
	enterNameComponent?: (ctx: NameComponentContext) => void;
	/**
	 * Exit a parse tree produced by `fqdnParser.nameComponent`.
	 * @param ctx the parse tree
	 */
	exitNameComponent?: (ctx: NameComponentContext) => void;
}


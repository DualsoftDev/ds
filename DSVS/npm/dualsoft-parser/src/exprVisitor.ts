// Generated from ../../../Grammar/g4s/expr.g4 by ANTLR 4.9.0-SNAPSHOT


import { ParseTreeVisitor } from "antlr4ts/tree/ParseTreeVisitor";

import { FunctionCallExprContext } from "./exprParser";
import { CastingExprContext } from "./exprParser";
import { ArrayReferenceExprContext } from "./exprParser";
import { UnaryExprContext } from "./exprParser";
import { BinaryExprMultiplicativeContext } from "./exprParser";
import { BinaryExprAdditiveContext } from "./exprParser";
import { BinaryExprBitwiseShiftContext } from "./exprParser";
import { BinaryExprRelationalContext } from "./exprParser";
import { BinaryExprBitwiseAndContext } from "./exprParser";
import { BinaryExprBitwiseXorContext } from "./exprParser";
import { BinaryExprBitwiseOrContext } from "./exprParser";
import { BinaryExprLogicalAndContext } from "./exprParser";
import { BinaryExprLogicalOrContext } from "./exprParser";
import { BinaryExprEqualityContext } from "./exprParser";
import { TerminalExprContext } from "./exprParser";
import { ParenthesysExprContext } from "./exprParser";
import { CommentContext } from "./exprParser";
import { IdentifierContext } from "./exprParser";
import { TagContext } from "./exprParser";
import { StorageContext } from "./exprParser";
import { FunctionNameContext } from "./exprParser";
import { TerminalContext } from "./exprParser";
import { LiteralContext } from "./exprParser";
import { LiteralSingleContext } from "./exprParser";
import { LiteralDoubleContext } from "./exprParser";
import { LiteralSbyteContext } from "./exprParser";
import { LiteralByteContext } from "./exprParser";
import { LiteralInt16Context } from "./exprParser";
import { LiteralUint16Context } from "./exprParser";
import { LiteralInt32Context } from "./exprParser";
import { LiteralUint32Context } from "./exprParser";
import { LiteralInt64Context } from "./exprParser";
import { LiteralUint64Context } from "./exprParser";
import { LiteralCharContext } from "./exprParser";
import { LiteralStringContext } from "./exprParser";
import { LiteralBoolContext } from "./exprParser";
import { ToplevelsContext } from "./exprParser";
import { ToplevelContext } from "./exprParser";
import { StatementContext } from "./exprParser";
import { AssignContext } from "./exprParser";
import { NormalAssignContext } from "./exprParser";
import { RisingAssignContext } from "./exprParser";
import { FallingAssignContext } from "./exprParser";
import { VarDeclContext } from "./exprParser";
import { StorageNameContext } from "./exprParser";
import { TypeContext } from "./exprParser";
import { TimerDeclContext } from "./exprParser";
import { TimerTypeContext } from "./exprParser";
import { TimerNameContext } from "./exprParser";
import { CounterDeclContext } from "./exprParser";
import { CounterTypeContext } from "./exprParser";
import { CounterNameContext } from "./exprParser";
import { CopyStatementContext } from "./exprParser";
import { CopyConditionContext } from "./exprParser";
import { CopySourceContext } from "./exprParser";
import { CopyTargetContext } from "./exprParser";
import { ExprContext } from "./exprParser";
import { ArgumentsContext } from "./exprParser";
import { ExprListContext } from "./exprParser";
import { UnaryOperatorContext } from "./exprParser";
import { BinaryOperatorMultiplicativeContext } from "./exprParser";
import { BinaryOperatorAdditiveContext } from "./exprParser";
import { BinaryOperatorBitwiseShiftContext } from "./exprParser";
import { BinaryOperatorRelationalContext } from "./exprParser";
import { BinaryOperatorEqualityContext } from "./exprParser";
import { BinaryOperatorBitwiseAndContext } from "./exprParser";
import { BinaryOperatorBitwiseXorContext } from "./exprParser";
import { BinaryOperatorBitwiseOrContext } from "./exprParser";
import { BinaryOperatorLogicalAndContext } from "./exprParser";
import { BinaryOperatorLogicalOrContext } from "./exprParser";
import { BinaryOperatorContext } from "./exprParser";


/**
 * This interface defines a complete generic visitor for a parse tree produced
 * by `exprParser`.
 *
 * @param <Result> The return type of the visit operation. Use `void` for
 * operations with no return type.
 */
export interface exprVisitor<Result> extends ParseTreeVisitor<Result> {
	/**
	 * Visit a parse tree produced by the `FunctionCallExpr`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitFunctionCallExpr?: (ctx: FunctionCallExprContext) => Result;

	/**
	 * Visit a parse tree produced by the `CastingExpr`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitCastingExpr?: (ctx: CastingExprContext) => Result;

	/**
	 * Visit a parse tree produced by the `ArrayReferenceExpr`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitArrayReferenceExpr?: (ctx: ArrayReferenceExprContext) => Result;

	/**
	 * Visit a parse tree produced by the `UnaryExpr`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitUnaryExpr?: (ctx: UnaryExprContext) => Result;

	/**
	 * Visit a parse tree produced by the `BinaryExprMultiplicative`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitBinaryExprMultiplicative?: (ctx: BinaryExprMultiplicativeContext) => Result;

	/**
	 * Visit a parse tree produced by the `BinaryExprAdditive`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitBinaryExprAdditive?: (ctx: BinaryExprAdditiveContext) => Result;

	/**
	 * Visit a parse tree produced by the `BinaryExprBitwiseShift`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitBinaryExprBitwiseShift?: (ctx: BinaryExprBitwiseShiftContext) => Result;

	/**
	 * Visit a parse tree produced by the `BinaryExprRelational`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitBinaryExprRelational?: (ctx: BinaryExprRelationalContext) => Result;

	/**
	 * Visit a parse tree produced by the `BinaryExprBitwiseAnd`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitBinaryExprBitwiseAnd?: (ctx: BinaryExprBitwiseAndContext) => Result;

	/**
	 * Visit a parse tree produced by the `BinaryExprBitwiseXor`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitBinaryExprBitwiseXor?: (ctx: BinaryExprBitwiseXorContext) => Result;

	/**
	 * Visit a parse tree produced by the `BinaryExprBitwiseOr`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitBinaryExprBitwiseOr?: (ctx: BinaryExprBitwiseOrContext) => Result;

	/**
	 * Visit a parse tree produced by the `BinaryExprLogicalAnd`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitBinaryExprLogicalAnd?: (ctx: BinaryExprLogicalAndContext) => Result;

	/**
	 * Visit a parse tree produced by the `BinaryExprLogicalOr`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitBinaryExprLogicalOr?: (ctx: BinaryExprLogicalOrContext) => Result;

	/**
	 * Visit a parse tree produced by the `BinaryExprEquality`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitBinaryExprEquality?: (ctx: BinaryExprEqualityContext) => Result;

	/**
	 * Visit a parse tree produced by the `TerminalExpr`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitTerminalExpr?: (ctx: TerminalExprContext) => Result;

	/**
	 * Visit a parse tree produced by the `ParenthesysExpr`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitParenthesysExpr?: (ctx: ParenthesysExprContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.comment`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitComment?: (ctx: CommentContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.identifier`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitIdentifier?: (ctx: IdentifierContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.tag`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitTag?: (ctx: TagContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.storage`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitStorage?: (ctx: StorageContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.functionName`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitFunctionName?: (ctx: FunctionNameContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.terminal`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitTerminal?: (ctx: TerminalContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.literal`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitLiteral?: (ctx: LiteralContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.literalSingle`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitLiteralSingle?: (ctx: LiteralSingleContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.literalDouble`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitLiteralDouble?: (ctx: LiteralDoubleContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.literalSbyte`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitLiteralSbyte?: (ctx: LiteralSbyteContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.literalByte`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitLiteralByte?: (ctx: LiteralByteContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.literalInt16`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitLiteralInt16?: (ctx: LiteralInt16Context) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.literalUint16`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitLiteralUint16?: (ctx: LiteralUint16Context) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.literalInt32`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitLiteralInt32?: (ctx: LiteralInt32Context) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.literalUint32`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitLiteralUint32?: (ctx: LiteralUint32Context) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.literalInt64`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitLiteralInt64?: (ctx: LiteralInt64Context) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.literalUint64`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitLiteralUint64?: (ctx: LiteralUint64Context) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.literalChar`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitLiteralChar?: (ctx: LiteralCharContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.literalString`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitLiteralString?: (ctx: LiteralStringContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.literalBool`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitLiteralBool?: (ctx: LiteralBoolContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.toplevels`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitToplevels?: (ctx: ToplevelsContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.toplevel`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitToplevel?: (ctx: ToplevelContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.statement`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitStatement?: (ctx: StatementContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.assign`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitAssign?: (ctx: AssignContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.normalAssign`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitNormalAssign?: (ctx: NormalAssignContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.risingAssign`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitRisingAssign?: (ctx: RisingAssignContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.fallingAssign`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitFallingAssign?: (ctx: FallingAssignContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.varDecl`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitVarDecl?: (ctx: VarDeclContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.storageName`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitStorageName?: (ctx: StorageNameContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.type`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitType?: (ctx: TypeContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.timerDecl`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitTimerDecl?: (ctx: TimerDeclContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.timerType`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitTimerType?: (ctx: TimerTypeContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.timerName`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitTimerName?: (ctx: TimerNameContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.counterDecl`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitCounterDecl?: (ctx: CounterDeclContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.counterType`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitCounterType?: (ctx: CounterTypeContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.counterName`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitCounterName?: (ctx: CounterNameContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.copyStatement`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitCopyStatement?: (ctx: CopyStatementContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.copyCondition`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitCopyCondition?: (ctx: CopyConditionContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.copySource`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitCopySource?: (ctx: CopySourceContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.copyTarget`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitCopyTarget?: (ctx: CopyTargetContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.expr`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitExpr?: (ctx: ExprContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.arguments`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitArguments?: (ctx: ArgumentsContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.exprList`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitExprList?: (ctx: ExprListContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.unaryOperator`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitUnaryOperator?: (ctx: UnaryOperatorContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.binaryOperatorMultiplicative`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitBinaryOperatorMultiplicative?: (ctx: BinaryOperatorMultiplicativeContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.binaryOperatorAdditive`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitBinaryOperatorAdditive?: (ctx: BinaryOperatorAdditiveContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.binaryOperatorBitwiseShift`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitBinaryOperatorBitwiseShift?: (ctx: BinaryOperatorBitwiseShiftContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.binaryOperatorRelational`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitBinaryOperatorRelational?: (ctx: BinaryOperatorRelationalContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.binaryOperatorEquality`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitBinaryOperatorEquality?: (ctx: BinaryOperatorEqualityContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.binaryOperatorBitwiseAnd`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitBinaryOperatorBitwiseAnd?: (ctx: BinaryOperatorBitwiseAndContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.binaryOperatorBitwiseXor`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitBinaryOperatorBitwiseXor?: (ctx: BinaryOperatorBitwiseXorContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.binaryOperatorBitwiseOr`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitBinaryOperatorBitwiseOr?: (ctx: BinaryOperatorBitwiseOrContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.binaryOperatorLogicalAnd`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitBinaryOperatorLogicalAnd?: (ctx: BinaryOperatorLogicalAndContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.binaryOperatorLogicalOr`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitBinaryOperatorLogicalOr?: (ctx: BinaryOperatorLogicalOrContext) => Result;

	/**
	 * Visit a parse tree produced by `exprParser.binaryOperator`.
	 * @param ctx the parse tree
	 * @return the visitor result
	 */
	visitBinaryOperator?: (ctx: BinaryOperatorContext) => Result;
}


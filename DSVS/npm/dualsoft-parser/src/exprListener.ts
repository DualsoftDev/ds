// Generated from ../../../Grammar/g4s/expr.g4 by ANTLR 4.9.0-SNAPSHOT


import { ParseTreeListener } from "antlr4ts/tree/ParseTreeListener";

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
 * This interface defines a complete listener for a parse tree produced by
 * `exprParser`.
 */
export interface exprListener extends ParseTreeListener {
	/**
	 * Enter a parse tree produced by the `FunctionCallExpr`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 */
	enterFunctionCallExpr?: (ctx: FunctionCallExprContext) => void;
	/**
	 * Exit a parse tree produced by the `FunctionCallExpr`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 */
	exitFunctionCallExpr?: (ctx: FunctionCallExprContext) => void;

	/**
	 * Enter a parse tree produced by the `CastingExpr`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 */
	enterCastingExpr?: (ctx: CastingExprContext) => void;
	/**
	 * Exit a parse tree produced by the `CastingExpr`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 */
	exitCastingExpr?: (ctx: CastingExprContext) => void;

	/**
	 * Enter a parse tree produced by the `ArrayReferenceExpr`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 */
	enterArrayReferenceExpr?: (ctx: ArrayReferenceExprContext) => void;
	/**
	 * Exit a parse tree produced by the `ArrayReferenceExpr`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 */
	exitArrayReferenceExpr?: (ctx: ArrayReferenceExprContext) => void;

	/**
	 * Enter a parse tree produced by the `UnaryExpr`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 */
	enterUnaryExpr?: (ctx: UnaryExprContext) => void;
	/**
	 * Exit a parse tree produced by the `UnaryExpr`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 */
	exitUnaryExpr?: (ctx: UnaryExprContext) => void;

	/**
	 * Enter a parse tree produced by the `BinaryExprMultiplicative`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 */
	enterBinaryExprMultiplicative?: (ctx: BinaryExprMultiplicativeContext) => void;
	/**
	 * Exit a parse tree produced by the `BinaryExprMultiplicative`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 */
	exitBinaryExprMultiplicative?: (ctx: BinaryExprMultiplicativeContext) => void;

	/**
	 * Enter a parse tree produced by the `BinaryExprAdditive`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 */
	enterBinaryExprAdditive?: (ctx: BinaryExprAdditiveContext) => void;
	/**
	 * Exit a parse tree produced by the `BinaryExprAdditive`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 */
	exitBinaryExprAdditive?: (ctx: BinaryExprAdditiveContext) => void;

	/**
	 * Enter a parse tree produced by the `BinaryExprBitwiseShift`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 */
	enterBinaryExprBitwiseShift?: (ctx: BinaryExprBitwiseShiftContext) => void;
	/**
	 * Exit a parse tree produced by the `BinaryExprBitwiseShift`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 */
	exitBinaryExprBitwiseShift?: (ctx: BinaryExprBitwiseShiftContext) => void;

	/**
	 * Enter a parse tree produced by the `BinaryExprRelational`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 */
	enterBinaryExprRelational?: (ctx: BinaryExprRelationalContext) => void;
	/**
	 * Exit a parse tree produced by the `BinaryExprRelational`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 */
	exitBinaryExprRelational?: (ctx: BinaryExprRelationalContext) => void;

	/**
	 * Enter a parse tree produced by the `BinaryExprBitwiseAnd`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 */
	enterBinaryExprBitwiseAnd?: (ctx: BinaryExprBitwiseAndContext) => void;
	/**
	 * Exit a parse tree produced by the `BinaryExprBitwiseAnd`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 */
	exitBinaryExprBitwiseAnd?: (ctx: BinaryExprBitwiseAndContext) => void;

	/**
	 * Enter a parse tree produced by the `BinaryExprBitwiseXor`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 */
	enterBinaryExprBitwiseXor?: (ctx: BinaryExprBitwiseXorContext) => void;
	/**
	 * Exit a parse tree produced by the `BinaryExprBitwiseXor`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 */
	exitBinaryExprBitwiseXor?: (ctx: BinaryExprBitwiseXorContext) => void;

	/**
	 * Enter a parse tree produced by the `BinaryExprBitwiseOr`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 */
	enterBinaryExprBitwiseOr?: (ctx: BinaryExprBitwiseOrContext) => void;
	/**
	 * Exit a parse tree produced by the `BinaryExprBitwiseOr`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 */
	exitBinaryExprBitwiseOr?: (ctx: BinaryExprBitwiseOrContext) => void;

	/**
	 * Enter a parse tree produced by the `BinaryExprLogicalAnd`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 */
	enterBinaryExprLogicalAnd?: (ctx: BinaryExprLogicalAndContext) => void;
	/**
	 * Exit a parse tree produced by the `BinaryExprLogicalAnd`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 */
	exitBinaryExprLogicalAnd?: (ctx: BinaryExprLogicalAndContext) => void;

	/**
	 * Enter a parse tree produced by the `BinaryExprLogicalOr`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 */
	enterBinaryExprLogicalOr?: (ctx: BinaryExprLogicalOrContext) => void;
	/**
	 * Exit a parse tree produced by the `BinaryExprLogicalOr`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 */
	exitBinaryExprLogicalOr?: (ctx: BinaryExprLogicalOrContext) => void;

	/**
	 * Enter a parse tree produced by the `BinaryExprEquality`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 */
	enterBinaryExprEquality?: (ctx: BinaryExprEqualityContext) => void;
	/**
	 * Exit a parse tree produced by the `BinaryExprEquality`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 */
	exitBinaryExprEquality?: (ctx: BinaryExprEqualityContext) => void;

	/**
	 * Enter a parse tree produced by the `TerminalExpr`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 */
	enterTerminalExpr?: (ctx: TerminalExprContext) => void;
	/**
	 * Exit a parse tree produced by the `TerminalExpr`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 */
	exitTerminalExpr?: (ctx: TerminalExprContext) => void;

	/**
	 * Enter a parse tree produced by the `ParenthesysExpr`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 */
	enterParenthesysExpr?: (ctx: ParenthesysExprContext) => void;
	/**
	 * Exit a parse tree produced by the `ParenthesysExpr`
	 * labeled alternative in `exprParser.expr`.
	 * @param ctx the parse tree
	 */
	exitParenthesysExpr?: (ctx: ParenthesysExprContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.comment`.
	 * @param ctx the parse tree
	 */
	enterComment?: (ctx: CommentContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.comment`.
	 * @param ctx the parse tree
	 */
	exitComment?: (ctx: CommentContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.identifier`.
	 * @param ctx the parse tree
	 */
	enterIdentifier?: (ctx: IdentifierContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.identifier`.
	 * @param ctx the parse tree
	 */
	exitIdentifier?: (ctx: IdentifierContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.tag`.
	 * @param ctx the parse tree
	 */
	enterTag?: (ctx: TagContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.tag`.
	 * @param ctx the parse tree
	 */
	exitTag?: (ctx: TagContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.storage`.
	 * @param ctx the parse tree
	 */
	enterStorage?: (ctx: StorageContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.storage`.
	 * @param ctx the parse tree
	 */
	exitStorage?: (ctx: StorageContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.functionName`.
	 * @param ctx the parse tree
	 */
	enterFunctionName?: (ctx: FunctionNameContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.functionName`.
	 * @param ctx the parse tree
	 */
	exitFunctionName?: (ctx: FunctionNameContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.terminal`.
	 * @param ctx the parse tree
	 */
	enterTerminal?: (ctx: TerminalContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.terminal`.
	 * @param ctx the parse tree
	 */
	exitTerminal?: (ctx: TerminalContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.literal`.
	 * @param ctx the parse tree
	 */
	enterLiteral?: (ctx: LiteralContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.literal`.
	 * @param ctx the parse tree
	 */
	exitLiteral?: (ctx: LiteralContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.literalSingle`.
	 * @param ctx the parse tree
	 */
	enterLiteralSingle?: (ctx: LiteralSingleContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.literalSingle`.
	 * @param ctx the parse tree
	 */
	exitLiteralSingle?: (ctx: LiteralSingleContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.literalDouble`.
	 * @param ctx the parse tree
	 */
	enterLiteralDouble?: (ctx: LiteralDoubleContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.literalDouble`.
	 * @param ctx the parse tree
	 */
	exitLiteralDouble?: (ctx: LiteralDoubleContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.literalSbyte`.
	 * @param ctx the parse tree
	 */
	enterLiteralSbyte?: (ctx: LiteralSbyteContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.literalSbyte`.
	 * @param ctx the parse tree
	 */
	exitLiteralSbyte?: (ctx: LiteralSbyteContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.literalByte`.
	 * @param ctx the parse tree
	 */
	enterLiteralByte?: (ctx: LiteralByteContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.literalByte`.
	 * @param ctx the parse tree
	 */
	exitLiteralByte?: (ctx: LiteralByteContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.literalInt16`.
	 * @param ctx the parse tree
	 */
	enterLiteralInt16?: (ctx: LiteralInt16Context) => void;
	/**
	 * Exit a parse tree produced by `exprParser.literalInt16`.
	 * @param ctx the parse tree
	 */
	exitLiteralInt16?: (ctx: LiteralInt16Context) => void;

	/**
	 * Enter a parse tree produced by `exprParser.literalUint16`.
	 * @param ctx the parse tree
	 */
	enterLiteralUint16?: (ctx: LiteralUint16Context) => void;
	/**
	 * Exit a parse tree produced by `exprParser.literalUint16`.
	 * @param ctx the parse tree
	 */
	exitLiteralUint16?: (ctx: LiteralUint16Context) => void;

	/**
	 * Enter a parse tree produced by `exprParser.literalInt32`.
	 * @param ctx the parse tree
	 */
	enterLiteralInt32?: (ctx: LiteralInt32Context) => void;
	/**
	 * Exit a parse tree produced by `exprParser.literalInt32`.
	 * @param ctx the parse tree
	 */
	exitLiteralInt32?: (ctx: LiteralInt32Context) => void;

	/**
	 * Enter a parse tree produced by `exprParser.literalUint32`.
	 * @param ctx the parse tree
	 */
	enterLiteralUint32?: (ctx: LiteralUint32Context) => void;
	/**
	 * Exit a parse tree produced by `exprParser.literalUint32`.
	 * @param ctx the parse tree
	 */
	exitLiteralUint32?: (ctx: LiteralUint32Context) => void;

	/**
	 * Enter a parse tree produced by `exprParser.literalInt64`.
	 * @param ctx the parse tree
	 */
	enterLiteralInt64?: (ctx: LiteralInt64Context) => void;
	/**
	 * Exit a parse tree produced by `exprParser.literalInt64`.
	 * @param ctx the parse tree
	 */
	exitLiteralInt64?: (ctx: LiteralInt64Context) => void;

	/**
	 * Enter a parse tree produced by `exprParser.literalUint64`.
	 * @param ctx the parse tree
	 */
	enterLiteralUint64?: (ctx: LiteralUint64Context) => void;
	/**
	 * Exit a parse tree produced by `exprParser.literalUint64`.
	 * @param ctx the parse tree
	 */
	exitLiteralUint64?: (ctx: LiteralUint64Context) => void;

	/**
	 * Enter a parse tree produced by `exprParser.literalChar`.
	 * @param ctx the parse tree
	 */
	enterLiteralChar?: (ctx: LiteralCharContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.literalChar`.
	 * @param ctx the parse tree
	 */
	exitLiteralChar?: (ctx: LiteralCharContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.literalString`.
	 * @param ctx the parse tree
	 */
	enterLiteralString?: (ctx: LiteralStringContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.literalString`.
	 * @param ctx the parse tree
	 */
	exitLiteralString?: (ctx: LiteralStringContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.literalBool`.
	 * @param ctx the parse tree
	 */
	enterLiteralBool?: (ctx: LiteralBoolContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.literalBool`.
	 * @param ctx the parse tree
	 */
	exitLiteralBool?: (ctx: LiteralBoolContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.toplevels`.
	 * @param ctx the parse tree
	 */
	enterToplevels?: (ctx: ToplevelsContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.toplevels`.
	 * @param ctx the parse tree
	 */
	exitToplevels?: (ctx: ToplevelsContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.toplevel`.
	 * @param ctx the parse tree
	 */
	enterToplevel?: (ctx: ToplevelContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.toplevel`.
	 * @param ctx the parse tree
	 */
	exitToplevel?: (ctx: ToplevelContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.statement`.
	 * @param ctx the parse tree
	 */
	enterStatement?: (ctx: StatementContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.statement`.
	 * @param ctx the parse tree
	 */
	exitStatement?: (ctx: StatementContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.assign`.
	 * @param ctx the parse tree
	 */
	enterAssign?: (ctx: AssignContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.assign`.
	 * @param ctx the parse tree
	 */
	exitAssign?: (ctx: AssignContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.normalAssign`.
	 * @param ctx the parse tree
	 */
	enterNormalAssign?: (ctx: NormalAssignContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.normalAssign`.
	 * @param ctx the parse tree
	 */
	exitNormalAssign?: (ctx: NormalAssignContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.risingAssign`.
	 * @param ctx the parse tree
	 */
	enterRisingAssign?: (ctx: RisingAssignContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.risingAssign`.
	 * @param ctx the parse tree
	 */
	exitRisingAssign?: (ctx: RisingAssignContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.fallingAssign`.
	 * @param ctx the parse tree
	 */
	enterFallingAssign?: (ctx: FallingAssignContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.fallingAssign`.
	 * @param ctx the parse tree
	 */
	exitFallingAssign?: (ctx: FallingAssignContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.varDecl`.
	 * @param ctx the parse tree
	 */
	enterVarDecl?: (ctx: VarDeclContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.varDecl`.
	 * @param ctx the parse tree
	 */
	exitVarDecl?: (ctx: VarDeclContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.storageName`.
	 * @param ctx the parse tree
	 */
	enterStorageName?: (ctx: StorageNameContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.storageName`.
	 * @param ctx the parse tree
	 */
	exitStorageName?: (ctx: StorageNameContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.type`.
	 * @param ctx the parse tree
	 */
	enterType?: (ctx: TypeContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.type`.
	 * @param ctx the parse tree
	 */
	exitType?: (ctx: TypeContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.timerDecl`.
	 * @param ctx the parse tree
	 */
	enterTimerDecl?: (ctx: TimerDeclContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.timerDecl`.
	 * @param ctx the parse tree
	 */
	exitTimerDecl?: (ctx: TimerDeclContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.timerType`.
	 * @param ctx the parse tree
	 */
	enterTimerType?: (ctx: TimerTypeContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.timerType`.
	 * @param ctx the parse tree
	 */
	exitTimerType?: (ctx: TimerTypeContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.timerName`.
	 * @param ctx the parse tree
	 */
	enterTimerName?: (ctx: TimerNameContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.timerName`.
	 * @param ctx the parse tree
	 */
	exitTimerName?: (ctx: TimerNameContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.counterDecl`.
	 * @param ctx the parse tree
	 */
	enterCounterDecl?: (ctx: CounterDeclContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.counterDecl`.
	 * @param ctx the parse tree
	 */
	exitCounterDecl?: (ctx: CounterDeclContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.counterType`.
	 * @param ctx the parse tree
	 */
	enterCounterType?: (ctx: CounterTypeContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.counterType`.
	 * @param ctx the parse tree
	 */
	exitCounterType?: (ctx: CounterTypeContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.counterName`.
	 * @param ctx the parse tree
	 */
	enterCounterName?: (ctx: CounterNameContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.counterName`.
	 * @param ctx the parse tree
	 */
	exitCounterName?: (ctx: CounterNameContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.copyStatement`.
	 * @param ctx the parse tree
	 */
	enterCopyStatement?: (ctx: CopyStatementContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.copyStatement`.
	 * @param ctx the parse tree
	 */
	exitCopyStatement?: (ctx: CopyStatementContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.copyCondition`.
	 * @param ctx the parse tree
	 */
	enterCopyCondition?: (ctx: CopyConditionContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.copyCondition`.
	 * @param ctx the parse tree
	 */
	exitCopyCondition?: (ctx: CopyConditionContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.copySource`.
	 * @param ctx the parse tree
	 */
	enterCopySource?: (ctx: CopySourceContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.copySource`.
	 * @param ctx the parse tree
	 */
	exitCopySource?: (ctx: CopySourceContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.copyTarget`.
	 * @param ctx the parse tree
	 */
	enterCopyTarget?: (ctx: CopyTargetContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.copyTarget`.
	 * @param ctx the parse tree
	 */
	exitCopyTarget?: (ctx: CopyTargetContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.expr`.
	 * @param ctx the parse tree
	 */
	enterExpr?: (ctx: ExprContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.expr`.
	 * @param ctx the parse tree
	 */
	exitExpr?: (ctx: ExprContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.arguments`.
	 * @param ctx the parse tree
	 */
	enterArguments?: (ctx: ArgumentsContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.arguments`.
	 * @param ctx the parse tree
	 */
	exitArguments?: (ctx: ArgumentsContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.exprList`.
	 * @param ctx the parse tree
	 */
	enterExprList?: (ctx: ExprListContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.exprList`.
	 * @param ctx the parse tree
	 */
	exitExprList?: (ctx: ExprListContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.unaryOperator`.
	 * @param ctx the parse tree
	 */
	enterUnaryOperator?: (ctx: UnaryOperatorContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.unaryOperator`.
	 * @param ctx the parse tree
	 */
	exitUnaryOperator?: (ctx: UnaryOperatorContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.binaryOperatorMultiplicative`.
	 * @param ctx the parse tree
	 */
	enterBinaryOperatorMultiplicative?: (ctx: BinaryOperatorMultiplicativeContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.binaryOperatorMultiplicative`.
	 * @param ctx the parse tree
	 */
	exitBinaryOperatorMultiplicative?: (ctx: BinaryOperatorMultiplicativeContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.binaryOperatorAdditive`.
	 * @param ctx the parse tree
	 */
	enterBinaryOperatorAdditive?: (ctx: BinaryOperatorAdditiveContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.binaryOperatorAdditive`.
	 * @param ctx the parse tree
	 */
	exitBinaryOperatorAdditive?: (ctx: BinaryOperatorAdditiveContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.binaryOperatorBitwiseShift`.
	 * @param ctx the parse tree
	 */
	enterBinaryOperatorBitwiseShift?: (ctx: BinaryOperatorBitwiseShiftContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.binaryOperatorBitwiseShift`.
	 * @param ctx the parse tree
	 */
	exitBinaryOperatorBitwiseShift?: (ctx: BinaryOperatorBitwiseShiftContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.binaryOperatorRelational`.
	 * @param ctx the parse tree
	 */
	enterBinaryOperatorRelational?: (ctx: BinaryOperatorRelationalContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.binaryOperatorRelational`.
	 * @param ctx the parse tree
	 */
	exitBinaryOperatorRelational?: (ctx: BinaryOperatorRelationalContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.binaryOperatorEquality`.
	 * @param ctx the parse tree
	 */
	enterBinaryOperatorEquality?: (ctx: BinaryOperatorEqualityContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.binaryOperatorEquality`.
	 * @param ctx the parse tree
	 */
	exitBinaryOperatorEquality?: (ctx: BinaryOperatorEqualityContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.binaryOperatorBitwiseAnd`.
	 * @param ctx the parse tree
	 */
	enterBinaryOperatorBitwiseAnd?: (ctx: BinaryOperatorBitwiseAndContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.binaryOperatorBitwiseAnd`.
	 * @param ctx the parse tree
	 */
	exitBinaryOperatorBitwiseAnd?: (ctx: BinaryOperatorBitwiseAndContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.binaryOperatorBitwiseXor`.
	 * @param ctx the parse tree
	 */
	enterBinaryOperatorBitwiseXor?: (ctx: BinaryOperatorBitwiseXorContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.binaryOperatorBitwiseXor`.
	 * @param ctx the parse tree
	 */
	exitBinaryOperatorBitwiseXor?: (ctx: BinaryOperatorBitwiseXorContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.binaryOperatorBitwiseOr`.
	 * @param ctx the parse tree
	 */
	enterBinaryOperatorBitwiseOr?: (ctx: BinaryOperatorBitwiseOrContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.binaryOperatorBitwiseOr`.
	 * @param ctx the parse tree
	 */
	exitBinaryOperatorBitwiseOr?: (ctx: BinaryOperatorBitwiseOrContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.binaryOperatorLogicalAnd`.
	 * @param ctx the parse tree
	 */
	enterBinaryOperatorLogicalAnd?: (ctx: BinaryOperatorLogicalAndContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.binaryOperatorLogicalAnd`.
	 * @param ctx the parse tree
	 */
	exitBinaryOperatorLogicalAnd?: (ctx: BinaryOperatorLogicalAndContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.binaryOperatorLogicalOr`.
	 * @param ctx the parse tree
	 */
	enterBinaryOperatorLogicalOr?: (ctx: BinaryOperatorLogicalOrContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.binaryOperatorLogicalOr`.
	 * @param ctx the parse tree
	 */
	exitBinaryOperatorLogicalOr?: (ctx: BinaryOperatorLogicalOrContext) => void;

	/**
	 * Enter a parse tree produced by `exprParser.binaryOperator`.
	 * @param ctx the parse tree
	 */
	enterBinaryOperator?: (ctx: BinaryOperatorContext) => void;
	/**
	 * Exit a parse tree produced by `exprParser.binaryOperator`.
	 * @param ctx the parse tree
	 */
	exitBinaryOperator?: (ctx: BinaryOperatorContext) => void;
}


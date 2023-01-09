// Generated from ../../../Grammar/g4s/expr.g4 by ANTLR 4.9.0-SNAPSHOT


import { ATN } from "antlr4ts/atn/ATN";
import { ATNDeserializer } from "antlr4ts/atn/ATNDeserializer";
import { FailedPredicateException } from "antlr4ts/FailedPredicateException";
import { NotNull } from "antlr4ts/Decorators";
import { NoViableAltException } from "antlr4ts/NoViableAltException";
import { Override } from "antlr4ts/Decorators";
import { Parser } from "antlr4ts/Parser";
import { ParserRuleContext } from "antlr4ts/ParserRuleContext";
import { ParserATNSimulator } from "antlr4ts/atn/ParserATNSimulator";
import { ParseTreeListener } from "antlr4ts/tree/ParseTreeListener";
import { ParseTreeVisitor } from "antlr4ts/tree/ParseTreeVisitor";
import { RecognitionException } from "antlr4ts/RecognitionException";
import { RuleContext } from "antlr4ts/RuleContext";
//import { RuleVersion } from "antlr4ts/RuleVersion";
import { TerminalNode } from "antlr4ts/tree/TerminalNode";
import { Token } from "antlr4ts/Token";
import { TokenStream } from "antlr4ts/TokenStream";
import { Vocabulary } from "antlr4ts/Vocabulary";
import { VocabularyImpl } from "antlr4ts/VocabularyImpl";

import * as Utils from "antlr4ts/misc/Utils";

import { exprListener } from "./exprListener";
import { exprVisitor } from "./exprVisitor";


export class exprParser extends Parser {
	public static readonly T__0 = 1;
	public static readonly T__1 = 2;
	public static readonly T__2 = 3;
	public static readonly T__3 = 4;
	public static readonly T__4 = 5;
	public static readonly T__5 = 6;
	public static readonly T__6 = 7;
	public static readonly T__7 = 8;
	public static readonly T__8 = 9;
	public static readonly T__9 = 10;
	public static readonly T__10 = 11;
	public static readonly T__11 = 12;
	public static readonly T__12 = 13;
	public static readonly T__13 = 14;
	public static readonly T__14 = 15;
	public static readonly T__15 = 16;
	public static readonly T__16 = 17;
	public static readonly T__17 = 18;
	public static readonly T__18 = 19;
	public static readonly T__19 = 20;
	public static readonly T__20 = 21;
	public static readonly T__21 = 22;
	public static readonly T__22 = 23;
	public static readonly T__23 = 24;
	public static readonly T__24 = 25;
	public static readonly T__25 = 26;
	public static readonly T__26 = 27;
	public static readonly T__27 = 28;
	public static readonly T__28 = 29;
	public static readonly T__29 = 30;
	public static readonly T__30 = 31;
	public static readonly T__31 = 32;
	public static readonly T__32 = 33;
	public static readonly T__33 = 34;
	public static readonly T__34 = 35;
	public static readonly T__35 = 36;
	public static readonly T__36 = 37;
	public static readonly T__37 = 38;
	public static readonly T__38 = 39;
	public static readonly T__39 = 40;
	public static readonly T__40 = 41;
	public static readonly T__41 = 42;
	public static readonly T__42 = 43;
	public static readonly T__43 = 44;
	public static readonly T__44 = 45;
	public static readonly T__45 = 46;
	public static readonly T__46 = 47;
	public static readonly T__47 = 48;
	public static readonly T__48 = 49;
	public static readonly T__49 = 50;
	public static readonly T__50 = 51;
	public static readonly T__51 = 52;
	public static readonly T__52 = 53;
	public static readonly T__53 = 54;
	public static readonly T__54 = 55;
	public static readonly T__55 = 56;
	public static readonly T__56 = 57;
	public static readonly T__57 = 58;
	public static readonly T__58 = 59;
	public static readonly T__59 = 60;
	public static readonly T__60 = 61;
	public static readonly T__61 = 62;
	public static readonly WS = 63;
	public static readonly BLOCK_COMMENT = 64;
	public static readonly LINE_COMMENT = 65;
	public static readonly TAG = 66;
	public static readonly IDENTIFIER = 67;
	public static readonly SINGLE = 68;
	public static readonly DOUBLE = 69;
	public static readonly SBYTE = 70;
	public static readonly BYTE = 71;
	public static readonly INT16 = 72;
	public static readonly UINT16 = 73;
	public static readonly INT32 = 74;
	public static readonly UINT32 = 75;
	public static readonly INT64 = 76;
	public static readonly UINT64 = 77;
	public static readonly CHAR = 78;
	public static readonly STRING = 79;
	public static readonly LPAREN = 80;
	public static readonly RPAREN = 81;
	public static readonly LBACKET = 82;
	public static readonly RBACKET = 83;
	public static readonly PLUS = 84;
	public static readonly MINUS = 85;
	public static readonly TIMES = 86;
	public static readonly DIV = 87;
	public static readonly GT = 88;
	public static readonly LT = 89;
	public static readonly EQ = 90;
	public static readonly POINT = 91;
	public static readonly POW = 92;
	public static readonly RULE_comment = 0;
	public static readonly RULE_identifier = 1;
	public static readonly RULE_tag = 2;
	public static readonly RULE_storage = 3;
	public static readonly RULE_functionName = 4;
	public static readonly RULE_terminal = 5;
	public static readonly RULE_literal = 6;
	public static readonly RULE_literalSingle = 7;
	public static readonly RULE_literalDouble = 8;
	public static readonly RULE_literalSbyte = 9;
	public static readonly RULE_literalByte = 10;
	public static readonly RULE_literalInt16 = 11;
	public static readonly RULE_literalUint16 = 12;
	public static readonly RULE_literalInt32 = 13;
	public static readonly RULE_literalUint32 = 14;
	public static readonly RULE_literalInt64 = 15;
	public static readonly RULE_literalUint64 = 16;
	public static readonly RULE_literalChar = 17;
	public static readonly RULE_literalString = 18;
	public static readonly RULE_literalBool = 19;
	public static readonly RULE_toplevels = 20;
	public static readonly RULE_toplevel = 21;
	public static readonly RULE_statement = 22;
	public static readonly RULE_assign = 23;
	public static readonly RULE_normalAssign = 24;
	public static readonly RULE_risingAssign = 25;
	public static readonly RULE_fallingAssign = 26;
	public static readonly RULE_varDecl = 27;
	public static readonly RULE_storageName = 28;
	public static readonly RULE_type = 29;
	public static readonly RULE_timerDecl = 30;
	public static readonly RULE_timerType = 31;
	public static readonly RULE_timerName = 32;
	public static readonly RULE_counterDecl = 33;
	public static readonly RULE_counterType = 34;
	public static readonly RULE_counterName = 35;
	public static readonly RULE_copyStatement = 36;
	public static readonly RULE_copyCondition = 37;
	public static readonly RULE_copySource = 38;
	public static readonly RULE_copyTarget = 39;
	public static readonly RULE_expr = 40;
	public static readonly RULE_arguments = 41;
	public static readonly RULE_exprList = 42;
	public static readonly RULE_unaryOperator = 43;
	public static readonly RULE_binaryOperatorMultiplicative = 44;
	public static readonly RULE_binaryOperatorAdditive = 45;
	public static readonly RULE_binaryOperatorBitwiseShift = 46;
	public static readonly RULE_binaryOperatorRelational = 47;
	public static readonly RULE_binaryOperatorEquality = 48;
	public static readonly RULE_binaryOperatorBitwiseAnd = 49;
	public static readonly RULE_binaryOperatorBitwiseXor = 50;
	public static readonly RULE_binaryOperatorBitwiseOr = 51;
	public static readonly RULE_binaryOperatorLogicalAnd = 52;
	public static readonly RULE_binaryOperatorLogicalOr = 53;
	public static readonly RULE_binaryOperator = 54;
	// tslint:disable:no-trailing-whitespace
	public static readonly ruleNames: string[] = [
		"comment", "identifier", "tag", "storage", "functionName", "terminal", 
		"literal", "literalSingle", "literalDouble", "literalSbyte", "literalByte", 
		"literalInt16", "literalUint16", "literalInt32", "literalUint32", "literalInt64", 
		"literalUint64", "literalChar", "literalString", "literalBool", "toplevels", 
		"toplevel", "statement", "assign", "normalAssign", "risingAssign", "fallingAssign", 
		"varDecl", "storageName", "type", "timerDecl", "timerType", "timerName", 
		"counterDecl", "counterType", "counterName", "copyStatement", "copyCondition", 
		"copySource", "copyTarget", "expr", "arguments", "exprList", "unaryOperator", 
		"binaryOperatorMultiplicative", "binaryOperatorAdditive", "binaryOperatorBitwiseShift", 
		"binaryOperatorRelational", "binaryOperatorEquality", "binaryOperatorBitwiseAnd", 
		"binaryOperatorBitwiseXor", "binaryOperatorBitwiseOr", "binaryOperatorLogicalAnd", 
		"binaryOperatorLogicalOr", "binaryOperator",
	];

	private static readonly _LITERAL_NAMES: Array<string | undefined> = [
		undefined, "'$'", "'true'", "'false'", "';'", "':='", "'ppulse'", "'npulse'", 
		"'int8'", "'sbyte'", "'uint8'", "'byte'", "'int16'", "'short'", "'word'", 
		"'uint16'", "'ushort'", "'int32'", "'int'", "'dword'", "'uint32'", "'uint'", 
		"'int64'", "'long'", "'uint64'", "'ulong'", "'double'", "'float64'", "'single'", 
		"'float32'", "'float'", "'char'", "'string'", "'bool'", "'boolean'", "'ton'", 
		"'tof'", "'rto'", "'ctu'", "'ctd'", "'ctud'", "'ctr'", "'copyIf'", "','", 
		"'!'", "'~'", "'~~~'", "'%'", "'<<'", "'<<<'", "'>>'", "'>>>'", "'>='", 
		"'<='", "'!='", "'<>'", "'&'", "'&&&'", "'^^^'", "'|'", "'|||'", "'&&'", 
		"'||'", undefined, undefined, undefined, undefined, undefined, undefined, 
		undefined, undefined, undefined, undefined, undefined, undefined, undefined, 
		undefined, undefined, undefined, undefined, "'('", "')'", "'['", "']'", 
		"'+'", "'-'", "'*'", "'/'", "'>'", "'<'", "'='", "'.'", "'^'",
	];
	private static readonly _SYMBOLIC_NAMES: Array<string | undefined> = [
		undefined, undefined, undefined, undefined, undefined, undefined, undefined, 
		undefined, undefined, undefined, undefined, undefined, undefined, undefined, 
		undefined, undefined, undefined, undefined, undefined, undefined, undefined, 
		undefined, undefined, undefined, undefined, undefined, undefined, undefined, 
		undefined, undefined, undefined, undefined, undefined, undefined, undefined, 
		undefined, undefined, undefined, undefined, undefined, undefined, undefined, 
		undefined, undefined, undefined, undefined, undefined, undefined, undefined, 
		undefined, undefined, undefined, undefined, undefined, undefined, undefined, 
		undefined, undefined, undefined, undefined, undefined, undefined, undefined, 
		"WS", "BLOCK_COMMENT", "LINE_COMMENT", "TAG", "IDENTIFIER", "SINGLE", 
		"DOUBLE", "SBYTE", "BYTE", "INT16", "UINT16", "INT32", "UINT32", "INT64", 
		"UINT64", "CHAR", "STRING", "LPAREN", "RPAREN", "LBACKET", "RBACKET", 
		"PLUS", "MINUS", "TIMES", "DIV", "GT", "LT", "EQ", "POINT", "POW",
	];
	public static readonly VOCABULARY: Vocabulary = new VocabularyImpl(exprParser._LITERAL_NAMES, exprParser._SYMBOLIC_NAMES, []);

	// @Override
	// @NotNull
	public get vocabulary(): Vocabulary {
		return exprParser.VOCABULARY;
	}
	// tslint:enable:no-trailing-whitespace

	// @Override
	public get grammarFileName(): string { return "expr.g4"; }

	// @Override
	public get ruleNames(): string[] { return exprParser.ruleNames; }

	// @Override
	public get serializedATN(): string { return exprParser._serializedATN; }

	protected createFailedPredicateException(predicate?: string, message?: string): FailedPredicateException {
		return new FailedPredicateException(this, predicate, message);
	}

	constructor(input: TokenStream) {
		super(input);
		this._interp = new ParserATNSimulator(exprParser._ATN, this);
	}
	// @RuleVersion(0)
	public comment(): CommentContext {
		let _localctx: CommentContext = new CommentContext(this._ctx, this.state);
		this.enterRule(_localctx, 0, exprParser.RULE_comment);
		let _la: number;
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 110;
			_la = this._input.LA(1);
			if (!(_la === exprParser.BLOCK_COMMENT || _la === exprParser.LINE_COMMENT)) {
			this._errHandler.recoverInline(this);
			} else {
				if (this._input.LA(1) === Token.EOF) {
					this.matchedEOF = true;
				}

				this._errHandler.reportMatch(this);
				this.consume();
			}
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public identifier(): IdentifierContext {
		let _localctx: IdentifierContext = new IdentifierContext(this._ctx, this.state);
		this.enterRule(_localctx, 2, exprParser.RULE_identifier);
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 112;
			this.match(exprParser.IDENTIFIER);
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public tag(): TagContext {
		let _localctx: TagContext = new TagContext(this._ctx, this.state);
		this.enterRule(_localctx, 4, exprParser.RULE_tag);
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 114;
			this.match(exprParser.TAG);
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public storage(): StorageContext {
		let _localctx: StorageContext = new StorageContext(this._ctx, this.state);
		this.enterRule(_localctx, 6, exprParser.RULE_storage);
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 116;
			this.match(exprParser.T__0);
			this.state = 117;
			this.storageName();
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public functionName(): FunctionNameContext {
		let _localctx: FunctionNameContext = new FunctionNameContext(this._ctx, this.state);
		this.enterRule(_localctx, 8, exprParser.RULE_functionName);
		try {
			this.state = 121;
			this._errHandler.sync(this);
			switch (this._input.LA(1)) {
			case exprParser.IDENTIFIER:
				this.enterOuterAlt(_localctx, 1);
				{
				this.state = 119;
				this.identifier();
				}
				break;
			case exprParser.T__46:
			case exprParser.T__47:
			case exprParser.T__48:
			case exprParser.T__49:
			case exprParser.T__50:
			case exprParser.T__51:
			case exprParser.T__52:
			case exprParser.T__53:
			case exprParser.T__54:
			case exprParser.T__55:
			case exprParser.T__56:
			case exprParser.T__57:
			case exprParser.T__58:
			case exprParser.T__59:
			case exprParser.T__60:
			case exprParser.T__61:
			case exprParser.PLUS:
			case exprParser.MINUS:
			case exprParser.TIMES:
			case exprParser.DIV:
			case exprParser.GT:
			case exprParser.LT:
			case exprParser.EQ:
			case exprParser.POW:
				this.enterOuterAlt(_localctx, 2);
				{
				this.state = 120;
				this.binaryOperator();
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public terminal(): TerminalContext {
		let _localctx: TerminalContext = new TerminalContext(this._ctx, this.state);
		this.enterRule(_localctx, 10, exprParser.RULE_terminal);
		try {
			this.state = 126;
			this._errHandler.sync(this);
			switch (this._input.LA(1)) {
			case exprParser.T__0:
				this.enterOuterAlt(_localctx, 1);
				{
				this.state = 123;
				this.storage();
				}
				break;
			case exprParser.TAG:
				this.enterOuterAlt(_localctx, 2);
				{
				this.state = 124;
				this.tag();
				}
				break;
			case exprParser.T__1:
			case exprParser.T__2:
			case exprParser.SINGLE:
			case exprParser.DOUBLE:
			case exprParser.SBYTE:
			case exprParser.BYTE:
			case exprParser.INT16:
			case exprParser.UINT16:
			case exprParser.INT32:
			case exprParser.UINT32:
			case exprParser.INT64:
			case exprParser.UINT64:
			case exprParser.CHAR:
			case exprParser.STRING:
				this.enterOuterAlt(_localctx, 3);
				{
				this.state = 125;
				this.literal();
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public literal(): LiteralContext {
		let _localctx: LiteralContext = new LiteralContext(this._ctx, this.state);
		this.enterRule(_localctx, 12, exprParser.RULE_literal);
		try {
			this.state = 141;
			this._errHandler.sync(this);
			switch (this._input.LA(1)) {
			case exprParser.SINGLE:
				this.enterOuterAlt(_localctx, 1);
				{
				this.state = 128;
				this.literalSingle();
				}
				break;
			case exprParser.DOUBLE:
				this.enterOuterAlt(_localctx, 2);
				{
				this.state = 129;
				this.literalDouble();
				}
				break;
			case exprParser.SBYTE:
				this.enterOuterAlt(_localctx, 3);
				{
				this.state = 130;
				this.literalSbyte();
				}
				break;
			case exprParser.BYTE:
				this.enterOuterAlt(_localctx, 4);
				{
				this.state = 131;
				this.literalByte();
				}
				break;
			case exprParser.INT16:
				this.enterOuterAlt(_localctx, 5);
				{
				this.state = 132;
				this.literalInt16();
				}
				break;
			case exprParser.UINT16:
				this.enterOuterAlt(_localctx, 6);
				{
				this.state = 133;
				this.literalUint16();
				}
				break;
			case exprParser.INT32:
				this.enterOuterAlt(_localctx, 7);
				{
				this.state = 134;
				this.literalInt32();
				}
				break;
			case exprParser.UINT32:
				this.enterOuterAlt(_localctx, 8);
				{
				this.state = 135;
				this.literalUint32();
				}
				break;
			case exprParser.INT64:
				this.enterOuterAlt(_localctx, 9);
				{
				this.state = 136;
				this.literalInt64();
				}
				break;
			case exprParser.UINT64:
				this.enterOuterAlt(_localctx, 10);
				{
				this.state = 137;
				this.literalUint64();
				}
				break;
			case exprParser.CHAR:
				this.enterOuterAlt(_localctx, 11);
				{
				this.state = 138;
				this.literalChar();
				}
				break;
			case exprParser.STRING:
				this.enterOuterAlt(_localctx, 12);
				{
				this.state = 139;
				this.literalString();
				}
				break;
			case exprParser.T__1:
			case exprParser.T__2:
				this.enterOuterAlt(_localctx, 13);
				{
				this.state = 140;
				this.literalBool();
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public literalSingle(): LiteralSingleContext {
		let _localctx: LiteralSingleContext = new LiteralSingleContext(this._ctx, this.state);
		this.enterRule(_localctx, 14, exprParser.RULE_literalSingle);
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 143;
			this.match(exprParser.SINGLE);
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public literalDouble(): LiteralDoubleContext {
		let _localctx: LiteralDoubleContext = new LiteralDoubleContext(this._ctx, this.state);
		this.enterRule(_localctx, 16, exprParser.RULE_literalDouble);
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 145;
			this.match(exprParser.DOUBLE);
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public literalSbyte(): LiteralSbyteContext {
		let _localctx: LiteralSbyteContext = new LiteralSbyteContext(this._ctx, this.state);
		this.enterRule(_localctx, 18, exprParser.RULE_literalSbyte);
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 147;
			this.match(exprParser.SBYTE);
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public literalByte(): LiteralByteContext {
		let _localctx: LiteralByteContext = new LiteralByteContext(this._ctx, this.state);
		this.enterRule(_localctx, 20, exprParser.RULE_literalByte);
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 149;
			this.match(exprParser.BYTE);
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public literalInt16(): LiteralInt16Context {
		let _localctx: LiteralInt16Context = new LiteralInt16Context(this._ctx, this.state);
		this.enterRule(_localctx, 22, exprParser.RULE_literalInt16);
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 151;
			this.match(exprParser.INT16);
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public literalUint16(): LiteralUint16Context {
		let _localctx: LiteralUint16Context = new LiteralUint16Context(this._ctx, this.state);
		this.enterRule(_localctx, 24, exprParser.RULE_literalUint16);
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 153;
			this.match(exprParser.UINT16);
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public literalInt32(): LiteralInt32Context {
		let _localctx: LiteralInt32Context = new LiteralInt32Context(this._ctx, this.state);
		this.enterRule(_localctx, 26, exprParser.RULE_literalInt32);
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 155;
			this.match(exprParser.INT32);
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public literalUint32(): LiteralUint32Context {
		let _localctx: LiteralUint32Context = new LiteralUint32Context(this._ctx, this.state);
		this.enterRule(_localctx, 28, exprParser.RULE_literalUint32);
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 157;
			this.match(exprParser.UINT32);
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public literalInt64(): LiteralInt64Context {
		let _localctx: LiteralInt64Context = new LiteralInt64Context(this._ctx, this.state);
		this.enterRule(_localctx, 30, exprParser.RULE_literalInt64);
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 159;
			this.match(exprParser.INT64);
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public literalUint64(): LiteralUint64Context {
		let _localctx: LiteralUint64Context = new LiteralUint64Context(this._ctx, this.state);
		this.enterRule(_localctx, 32, exprParser.RULE_literalUint64);
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 161;
			this.match(exprParser.UINT64);
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public literalChar(): LiteralCharContext {
		let _localctx: LiteralCharContext = new LiteralCharContext(this._ctx, this.state);
		this.enterRule(_localctx, 34, exprParser.RULE_literalChar);
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 163;
			this.match(exprParser.CHAR);
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public literalString(): LiteralStringContext {
		let _localctx: LiteralStringContext = new LiteralStringContext(this._ctx, this.state);
		this.enterRule(_localctx, 36, exprParser.RULE_literalString);
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 165;
			this.match(exprParser.STRING);
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public literalBool(): LiteralBoolContext {
		let _localctx: LiteralBoolContext = new LiteralBoolContext(this._ctx, this.state);
		this.enterRule(_localctx, 38, exprParser.RULE_literalBool);
		let _la: number;
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 167;
			_la = this._input.LA(1);
			if (!(_la === exprParser.T__1 || _la === exprParser.T__2)) {
			this._errHandler.recoverInline(this);
			} else {
				if (this._input.LA(1) === Token.EOF) {
					this.matchedEOF = true;
				}

				this._errHandler.reportMatch(this);
				this.consume();
			}
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public toplevels(): ToplevelsContext {
		let _localctx: ToplevelsContext = new ToplevelsContext(this._ctx, this.state);
		this.enterRule(_localctx, 40, exprParser.RULE_toplevels);
		try {
			let _alt: number;
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 169;
			this.toplevel();
			this.state = 174;
			this._errHandler.sync(this);
			_alt = this.interpreter.adaptivePredict(this._input, 3, this._ctx);
			while (_alt !== 2 && _alt !== ATN.INVALID_ALT_NUMBER) {
				if (_alt === 1) {
					{
					{
					this.state = 170;
					this.match(exprParser.T__3);
					this.state = 171;
					this.toplevel();
					}
					}
				}
				this.state = 176;
				this._errHandler.sync(this);
				_alt = this.interpreter.adaptivePredict(this._input, 3, this._ctx);
			}
			this.state = 177;
			this.match(exprParser.T__3);
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public toplevel(): ToplevelContext {
		let _localctx: ToplevelContext = new ToplevelContext(this._ctx, this.state);
		this.enterRule(_localctx, 42, exprParser.RULE_toplevel);
		try {
			this.state = 181;
			this._errHandler.sync(this);
			switch ( this.interpreter.adaptivePredict(this._input, 4, this._ctx) ) {
			case 1:
				this.enterOuterAlt(_localctx, 1);
				{
				this.state = 179;
				this.expr(0);
				}
				break;

			case 2:
				this.enterOuterAlt(_localctx, 2);
				{
				this.state = 180;
				this.statement();
				}
				break;
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public statement(): StatementContext {
		let _localctx: StatementContext = new StatementContext(this._ctx, this.state);
		this.enterRule(_localctx, 44, exprParser.RULE_statement);
		try {
			this.state = 188;
			this._errHandler.sync(this);
			switch (this._input.LA(1)) {
			case exprParser.T__0:
			case exprParser.T__5:
			case exprParser.T__6:
				this.enterOuterAlt(_localctx, 1);
				{
				this.state = 183;
				this.assign();
				}
				break;
			case exprParser.T__7:
			case exprParser.T__8:
			case exprParser.T__9:
			case exprParser.T__10:
			case exprParser.T__11:
			case exprParser.T__12:
			case exprParser.T__13:
			case exprParser.T__14:
			case exprParser.T__15:
			case exprParser.T__16:
			case exprParser.T__17:
			case exprParser.T__18:
			case exprParser.T__19:
			case exprParser.T__20:
			case exprParser.T__21:
			case exprParser.T__22:
			case exprParser.T__23:
			case exprParser.T__24:
			case exprParser.T__25:
			case exprParser.T__26:
			case exprParser.T__27:
			case exprParser.T__28:
			case exprParser.T__29:
			case exprParser.T__30:
			case exprParser.T__31:
			case exprParser.T__32:
			case exprParser.T__33:
				this.enterOuterAlt(_localctx, 2);
				{
				this.state = 184;
				this.varDecl();
				}
				break;
			case exprParser.T__34:
			case exprParser.T__35:
			case exprParser.T__36:
				this.enterOuterAlt(_localctx, 3);
				{
				this.state = 185;
				this.timerDecl();
				}
				break;
			case exprParser.T__37:
			case exprParser.T__38:
			case exprParser.T__39:
			case exprParser.T__40:
				this.enterOuterAlt(_localctx, 4);
				{
				this.state = 186;
				this.counterDecl();
				}
				break;
			case exprParser.T__41:
				this.enterOuterAlt(_localctx, 5);
				{
				this.state = 187;
				this.copyStatement();
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public assign(): AssignContext {
		let _localctx: AssignContext = new AssignContext(this._ctx, this.state);
		this.enterRule(_localctx, 46, exprParser.RULE_assign);
		try {
			this.state = 193;
			this._errHandler.sync(this);
			switch (this._input.LA(1)) {
			case exprParser.T__0:
				this.enterOuterAlt(_localctx, 1);
				{
				this.state = 190;
				this.normalAssign();
				}
				break;
			case exprParser.T__5:
				this.enterOuterAlt(_localctx, 2);
				{
				this.state = 191;
				this.risingAssign();
				}
				break;
			case exprParser.T__6:
				this.enterOuterAlt(_localctx, 3);
				{
				this.state = 192;
				this.fallingAssign();
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public normalAssign(): NormalAssignContext {
		let _localctx: NormalAssignContext = new NormalAssignContext(this._ctx, this.state);
		this.enterRule(_localctx, 48, exprParser.RULE_normalAssign);
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 195;
			this.match(exprParser.T__0);
			this.state = 196;
			this.storageName();
			this.state = 197;
			this.match(exprParser.T__4);
			this.state = 198;
			this.expr(0);
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public risingAssign(): RisingAssignContext {
		let _localctx: RisingAssignContext = new RisingAssignContext(this._ctx, this.state);
		this.enterRule(_localctx, 50, exprParser.RULE_risingAssign);
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 200;
			this.match(exprParser.T__5);
			this.state = 201;
			this.match(exprParser.LPAREN);
			this.state = 202;
			this.match(exprParser.T__0);
			this.state = 203;
			this.storageName();
			this.state = 204;
			this.match(exprParser.RPAREN);
			this.state = 205;
			this.match(exprParser.T__4);
			this.state = 206;
			this.expr(0);
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public fallingAssign(): FallingAssignContext {
		let _localctx: FallingAssignContext = new FallingAssignContext(this._ctx, this.state);
		this.enterRule(_localctx, 52, exprParser.RULE_fallingAssign);
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 208;
			this.match(exprParser.T__6);
			this.state = 209;
			this.match(exprParser.LPAREN);
			this.state = 210;
			this.match(exprParser.T__0);
			this.state = 211;
			this.storageName();
			this.state = 212;
			this.match(exprParser.RPAREN);
			this.state = 213;
			this.match(exprParser.T__4);
			this.state = 214;
			this.expr(0);
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public varDecl(): VarDeclContext {
		let _localctx: VarDeclContext = new VarDeclContext(this._ctx, this.state);
		this.enterRule(_localctx, 54, exprParser.RULE_varDecl);
		let _la: number;
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 216;
			this.type();
			this.state = 217;
			this.storageName();
			this.state = 220;
			this._errHandler.sync(this);
			_la = this._input.LA(1);
			if (_la === exprParser.EQ) {
				{
				this.state = 218;
				this.match(exprParser.EQ);
				this.state = 219;
				this.expr(0);
				}
			}

			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public storageName(): StorageNameContext {
		let _localctx: StorageNameContext = new StorageNameContext(this._ctx, this.state);
		this.enterRule(_localctx, 56, exprParser.RULE_storageName);
		try {
			let _alt: number;
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 222;
			this.match(exprParser.IDENTIFIER);
			this.state = 227;
			this._errHandler.sync(this);
			_alt = this.interpreter.adaptivePredict(this._input, 8, this._ctx);
			while (_alt !== 2 && _alt !== ATN.INVALID_ALT_NUMBER) {
				if (_alt === 1) {
					{
					{
					this.state = 223;
					this.match(exprParser.POINT);
					this.state = 224;
					this.match(exprParser.IDENTIFIER);
					}
					}
				}
				this.state = 229;
				this._errHandler.sync(this);
				_alt = this.interpreter.adaptivePredict(this._input, 8, this._ctx);
			}
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public type(): TypeContext {
		let _localctx: TypeContext = new TypeContext(this._ctx, this.state);
		this.enterRule(_localctx, 58, exprParser.RULE_type);
		let _la: number;
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 230;
			_la = this._input.LA(1);
			if (!(((((_la - 8)) & ~0x1F) === 0 && ((1 << (_la - 8)) & ((1 << (exprParser.T__7 - 8)) | (1 << (exprParser.T__8 - 8)) | (1 << (exprParser.T__9 - 8)) | (1 << (exprParser.T__10 - 8)) | (1 << (exprParser.T__11 - 8)) | (1 << (exprParser.T__12 - 8)) | (1 << (exprParser.T__13 - 8)) | (1 << (exprParser.T__14 - 8)) | (1 << (exprParser.T__15 - 8)) | (1 << (exprParser.T__16 - 8)) | (1 << (exprParser.T__17 - 8)) | (1 << (exprParser.T__18 - 8)) | (1 << (exprParser.T__19 - 8)) | (1 << (exprParser.T__20 - 8)) | (1 << (exprParser.T__21 - 8)) | (1 << (exprParser.T__22 - 8)) | (1 << (exprParser.T__23 - 8)) | (1 << (exprParser.T__24 - 8)) | (1 << (exprParser.T__25 - 8)) | (1 << (exprParser.T__26 - 8)) | (1 << (exprParser.T__27 - 8)) | (1 << (exprParser.T__28 - 8)) | (1 << (exprParser.T__29 - 8)) | (1 << (exprParser.T__30 - 8)) | (1 << (exprParser.T__31 - 8)) | (1 << (exprParser.T__32 - 8)) | (1 << (exprParser.T__33 - 8)))) !== 0))) {
			this._errHandler.recoverInline(this);
			} else {
				if (this._input.LA(1) === Token.EOF) {
					this.matchedEOF = true;
				}

				this._errHandler.reportMatch(this);
				this.consume();
			}
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public timerDecl(): TimerDeclContext {
		let _localctx: TimerDeclContext = new TimerDeclContext(this._ctx, this.state);
		this.enterRule(_localctx, 60, exprParser.RULE_timerDecl);
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 232;
			this.timerType();
			this.state = 233;
			this.storageName();
			this.state = 234;
			this.match(exprParser.EQ);
			this.state = 235;
			this.expr(0);
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public timerType(): TimerTypeContext {
		let _localctx: TimerTypeContext = new TimerTypeContext(this._ctx, this.state);
		this.enterRule(_localctx, 62, exprParser.RULE_timerType);
		let _la: number;
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 237;
			_la = this._input.LA(1);
			if (!(((((_la - 35)) & ~0x1F) === 0 && ((1 << (_la - 35)) & ((1 << (exprParser.T__34 - 35)) | (1 << (exprParser.T__35 - 35)) | (1 << (exprParser.T__36 - 35)))) !== 0))) {
			this._errHandler.recoverInline(this);
			} else {
				if (this._input.LA(1) === Token.EOF) {
					this.matchedEOF = true;
				}

				this._errHandler.reportMatch(this);
				this.consume();
			}
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public timerName(): TimerNameContext {
		let _localctx: TimerNameContext = new TimerNameContext(this._ctx, this.state);
		this.enterRule(_localctx, 64, exprParser.RULE_timerName);
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 239;
			this.match(exprParser.IDENTIFIER);
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public counterDecl(): CounterDeclContext {
		let _localctx: CounterDeclContext = new CounterDeclContext(this._ctx, this.state);
		this.enterRule(_localctx, 66, exprParser.RULE_counterDecl);
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 241;
			this.counterType();
			this.state = 242;
			this.storageName();
			this.state = 243;
			this.match(exprParser.EQ);
			this.state = 244;
			this.expr(0);
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public counterType(): CounterTypeContext {
		let _localctx: CounterTypeContext = new CounterTypeContext(this._ctx, this.state);
		this.enterRule(_localctx, 68, exprParser.RULE_counterType);
		let _la: number;
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 246;
			_la = this._input.LA(1);
			if (!(((((_la - 38)) & ~0x1F) === 0 && ((1 << (_la - 38)) & ((1 << (exprParser.T__37 - 38)) | (1 << (exprParser.T__38 - 38)) | (1 << (exprParser.T__39 - 38)) | (1 << (exprParser.T__40 - 38)))) !== 0))) {
			this._errHandler.recoverInline(this);
			} else {
				if (this._input.LA(1) === Token.EOF) {
					this.matchedEOF = true;
				}

				this._errHandler.reportMatch(this);
				this.consume();
			}
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public counterName(): CounterNameContext {
		let _localctx: CounterNameContext = new CounterNameContext(this._ctx, this.state);
		this.enterRule(_localctx, 70, exprParser.RULE_counterName);
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 248;
			this.match(exprParser.IDENTIFIER);
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public copyStatement(): CopyStatementContext {
		let _localctx: CopyStatementContext = new CopyStatementContext(this._ctx, this.state);
		this.enterRule(_localctx, 72, exprParser.RULE_copyStatement);
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 250;
			this.match(exprParser.T__41);
			this.state = 251;
			this.match(exprParser.LPAREN);
			this.state = 252;
			this.copyCondition();
			this.state = 253;
			this.match(exprParser.T__42);
			this.state = 254;
			this.copySource();
			this.state = 255;
			this.match(exprParser.T__42);
			this.state = 256;
			this.copyTarget();
			this.state = 257;
			this.match(exprParser.RPAREN);
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public copyCondition(): CopyConditionContext {
		let _localctx: CopyConditionContext = new CopyConditionContext(this._ctx, this.state);
		this.enterRule(_localctx, 74, exprParser.RULE_copyCondition);
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 259;
			this.expr(0);
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public copySource(): CopySourceContext {
		let _localctx: CopySourceContext = new CopySourceContext(this._ctx, this.state);
		this.enterRule(_localctx, 76, exprParser.RULE_copySource);
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 261;
			this.expr(0);
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public copyTarget(): CopyTargetContext {
		let _localctx: CopyTargetContext = new CopyTargetContext(this._ctx, this.state);
		this.enterRule(_localctx, 78, exprParser.RULE_copyTarget);
		try {
			this.state = 265;
			this._errHandler.sync(this);
			switch (this._input.LA(1)) {
			case exprParser.T__0:
				this.enterOuterAlt(_localctx, 1);
				{
				this.state = 263;
				this.storage();
				}
				break;
			case exprParser.TAG:
				this.enterOuterAlt(_localctx, 2);
				{
				this.state = 264;
				this.tag();
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}

	public expr(): ExprContext;
	public expr(_p: number): ExprContext;
	// @RuleVersion(0)
	public expr(_p?: number): ExprContext {
		if (_p === undefined) {
			_p = 0;
		}

		let _parentctx: ParserRuleContext = this._ctx;
		let _parentState: number = this.state;
		let _localctx: ExprContext = new ExprContext(this._ctx, _parentState);
		let _prevctx: ExprContext = _localctx;
		let _startState: number = 80;
		this.enterRecursionRule(_localctx, 80, exprParser.RULE_expr, _p);
		let _la: number;
		try {
			let _alt: number;
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 297;
			this._errHandler.sync(this);
			switch ( this.interpreter.adaptivePredict(this._input, 12, this._ctx) ) {
			case 1:
				{
				_localctx = new FunctionCallExprContext(_localctx);
				this._ctx = _localctx;
				_prevctx = _localctx;

				this.state = 268;
				this.functionName();
				this.state = 269;
				this.match(exprParser.LPAREN);
				this.state = 271;
				this._errHandler.sync(this);
				_la = this._input.LA(1);
				if ((((_la) & ~0x1F) === 0 && ((1 << _la) & ((1 << exprParser.T__0) | (1 << exprParser.T__1) | (1 << exprParser.T__2))) !== 0) || ((((_la - 44)) & ~0x1F) === 0 && ((1 << (_la - 44)) & ((1 << (exprParser.T__43 - 44)) | (1 << (exprParser.T__44 - 44)) | (1 << (exprParser.T__45 - 44)) | (1 << (exprParser.T__46 - 44)) | (1 << (exprParser.T__47 - 44)) | (1 << (exprParser.T__48 - 44)) | (1 << (exprParser.T__49 - 44)) | (1 << (exprParser.T__50 - 44)) | (1 << (exprParser.T__51 - 44)) | (1 << (exprParser.T__52 - 44)) | (1 << (exprParser.T__53 - 44)) | (1 << (exprParser.T__54 - 44)) | (1 << (exprParser.T__55 - 44)) | (1 << (exprParser.T__56 - 44)) | (1 << (exprParser.T__57 - 44)) | (1 << (exprParser.T__58 - 44)) | (1 << (exprParser.T__59 - 44)) | (1 << (exprParser.T__60 - 44)) | (1 << (exprParser.T__61 - 44)) | (1 << (exprParser.TAG - 44)) | (1 << (exprParser.IDENTIFIER - 44)) | (1 << (exprParser.SINGLE - 44)) | (1 << (exprParser.DOUBLE - 44)) | (1 << (exprParser.SBYTE - 44)) | (1 << (exprParser.BYTE - 44)) | (1 << (exprParser.INT16 - 44)) | (1 << (exprParser.UINT16 - 44)) | (1 << (exprParser.INT32 - 44)) | (1 << (exprParser.UINT32 - 44)))) !== 0) || ((((_la - 76)) & ~0x1F) === 0 && ((1 << (_la - 76)) & ((1 << (exprParser.INT64 - 76)) | (1 << (exprParser.UINT64 - 76)) | (1 << (exprParser.CHAR - 76)) | (1 << (exprParser.STRING - 76)) | (1 << (exprParser.LPAREN - 76)) | (1 << (exprParser.PLUS - 76)) | (1 << (exprParser.MINUS - 76)) | (1 << (exprParser.TIMES - 76)) | (1 << (exprParser.DIV - 76)) | (1 << (exprParser.GT - 76)) | (1 << (exprParser.LT - 76)) | (1 << (exprParser.EQ - 76)) | (1 << (exprParser.POW - 76)))) !== 0)) {
					{
					this.state = 270;
					this.arguments();
					}
				}

				this.state = 273;
				this.match(exprParser.RPAREN);
				}
				break;

			case 2:
				{
				_localctx = new CastingExprContext(_localctx);
				this._ctx = _localctx;
				_prevctx = _localctx;
				this.state = 275;
				this.match(exprParser.LPAREN);
				this.state = 276;
				this.type();
				this.state = 277;
				this.match(exprParser.RPAREN);
				this.state = 278;
				this.expr(15);
				}
				break;

			case 3:
				{
				_localctx = new ArrayReferenceExprContext(_localctx);
				this._ctx = _localctx;
				_prevctx = _localctx;
				this.state = 280;
				this.storage();
				this.state = 285;
				this._errHandler.sync(this);
				_alt = 1;
				do {
					switch (_alt) {
					case 1:
						{
						{
						this.state = 281;
						this.match(exprParser.LBACKET);
						this.state = 282;
						this.expr(0);
						this.state = 283;
						this.match(exprParser.RBACKET);
						}
						}
						break;
					default:
						throw new NoViableAltException(this);
					}
					this.state = 287;
					this._errHandler.sync(this);
					_alt = this.interpreter.adaptivePredict(this._input, 11, this._ctx);
				} while (_alt !== 2 && _alt !== ATN.INVALID_ALT_NUMBER);
				}
				break;

			case 4:
				{
				_localctx = new UnaryExprContext(_localctx);
				this._ctx = _localctx;
				_prevctx = _localctx;
				this.state = 289;
				this.unaryOperator();
				this.state = 290;
				this.expr(13);
				}
				break;

			case 5:
				{
				_localctx = new TerminalExprContext(_localctx);
				this._ctx = _localctx;
				_prevctx = _localctx;
				this.state = 292;
				this.terminal();
				}
				break;

			case 6:
				{
				_localctx = new ParenthesysExprContext(_localctx);
				this._ctx = _localctx;
				_prevctx = _localctx;
				this.state = 293;
				this.match(exprParser.LPAREN);
				this.state = 294;
				this.expr(0);
				this.state = 295;
				this.match(exprParser.RPAREN);
				}
				break;
			}
			this._ctx._stop = this._input.tryLT(-1);
			this.state = 341;
			this._errHandler.sync(this);
			_alt = this.interpreter.adaptivePredict(this._input, 14, this._ctx);
			while (_alt !== 2 && _alt !== ATN.INVALID_ALT_NUMBER) {
				if (_alt === 1) {
					if (this._parseListeners != null) {
						this.triggerExitRuleEvent();
					}
					_prevctx = _localctx;
					{
					this.state = 339;
					this._errHandler.sync(this);
					switch ( this.interpreter.adaptivePredict(this._input, 13, this._ctx) ) {
					case 1:
						{
						_localctx = new BinaryExprMultiplicativeContext(new ExprContext(_parentctx, _parentState));
						this.pushNewRecursionContext(_localctx, _startState, exprParser.RULE_expr);
						this.state = 299;
						if (!(this.precpred(this._ctx, 12))) {
							throw this.createFailedPredicateException("this.precpred(this._ctx, 12)");
						}
						this.state = 300;
						this.binaryOperatorMultiplicative();
						this.state = 301;
						this.expr(13);
						}
						break;

					case 2:
						{
						_localctx = new BinaryExprAdditiveContext(new ExprContext(_parentctx, _parentState));
						this.pushNewRecursionContext(_localctx, _startState, exprParser.RULE_expr);
						this.state = 303;
						if (!(this.precpred(this._ctx, 11))) {
							throw this.createFailedPredicateException("this.precpred(this._ctx, 11)");
						}
						this.state = 304;
						this.binaryOperatorAdditive();
						this.state = 305;
						this.expr(12);
						}
						break;

					case 3:
						{
						_localctx = new BinaryExprBitwiseShiftContext(new ExprContext(_parentctx, _parentState));
						this.pushNewRecursionContext(_localctx, _startState, exprParser.RULE_expr);
						this.state = 307;
						if (!(this.precpred(this._ctx, 10))) {
							throw this.createFailedPredicateException("this.precpred(this._ctx, 10)");
						}
						this.state = 308;
						this.binaryOperatorBitwiseShift();
						this.state = 309;
						this.expr(11);
						}
						break;

					case 4:
						{
						_localctx = new BinaryExprRelationalContext(new ExprContext(_parentctx, _parentState));
						this.pushNewRecursionContext(_localctx, _startState, exprParser.RULE_expr);
						this.state = 311;
						if (!(this.precpred(this._ctx, 9))) {
							throw this.createFailedPredicateException("this.precpred(this._ctx, 9)");
						}
						this.state = 312;
						this.binaryOperatorRelational();
						this.state = 313;
						this.expr(10);
						}
						break;

					case 5:
						{
						_localctx = new BinaryExprBitwiseAndContext(new ExprContext(_parentctx, _parentState));
						this.pushNewRecursionContext(_localctx, _startState, exprParser.RULE_expr);
						this.state = 315;
						if (!(this.precpred(this._ctx, 8))) {
							throw this.createFailedPredicateException("this.precpred(this._ctx, 8)");
						}
						this.state = 316;
						this.binaryOperatorBitwiseAnd();
						this.state = 317;
						this.expr(9);
						}
						break;

					case 6:
						{
						_localctx = new BinaryExprBitwiseXorContext(new ExprContext(_parentctx, _parentState));
						this.pushNewRecursionContext(_localctx, _startState, exprParser.RULE_expr);
						this.state = 319;
						if (!(this.precpred(this._ctx, 7))) {
							throw this.createFailedPredicateException("this.precpred(this._ctx, 7)");
						}
						this.state = 320;
						this.binaryOperatorBitwiseXor();
						this.state = 321;
						this.expr(8);
						}
						break;

					case 7:
						{
						_localctx = new BinaryExprBitwiseOrContext(new ExprContext(_parentctx, _parentState));
						this.pushNewRecursionContext(_localctx, _startState, exprParser.RULE_expr);
						this.state = 323;
						if (!(this.precpred(this._ctx, 6))) {
							throw this.createFailedPredicateException("this.precpred(this._ctx, 6)");
						}
						this.state = 324;
						this.binaryOperatorBitwiseOr();
						this.state = 325;
						this.expr(7);
						}
						break;

					case 8:
						{
						_localctx = new BinaryExprLogicalAndContext(new ExprContext(_parentctx, _parentState));
						this.pushNewRecursionContext(_localctx, _startState, exprParser.RULE_expr);
						this.state = 327;
						if (!(this.precpred(this._ctx, 5))) {
							throw this.createFailedPredicateException("this.precpred(this._ctx, 5)");
						}
						this.state = 328;
						this.binaryOperatorLogicalAnd();
						this.state = 329;
						this.expr(6);
						}
						break;

					case 9:
						{
						_localctx = new BinaryExprLogicalOrContext(new ExprContext(_parentctx, _parentState));
						this.pushNewRecursionContext(_localctx, _startState, exprParser.RULE_expr);
						this.state = 331;
						if (!(this.precpred(this._ctx, 4))) {
							throw this.createFailedPredicateException("this.precpred(this._ctx, 4)");
						}
						this.state = 332;
						this.binaryOperatorLogicalOr();
						this.state = 333;
						this.expr(5);
						}
						break;

					case 10:
						{
						_localctx = new BinaryExprEqualityContext(new ExprContext(_parentctx, _parentState));
						this.pushNewRecursionContext(_localctx, _startState, exprParser.RULE_expr);
						this.state = 335;
						if (!(this.precpred(this._ctx, 3))) {
							throw this.createFailedPredicateException("this.precpred(this._ctx, 3)");
						}
						this.state = 336;
						this.binaryOperatorEquality();
						this.state = 337;
						this.expr(4);
						}
						break;
					}
					}
				}
				this.state = 343;
				this._errHandler.sync(this);
				_alt = this.interpreter.adaptivePredict(this._input, 14, this._ctx);
			}
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.unrollRecursionContexts(_parentctx);
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public arguments(): ArgumentsContext {
		let _localctx: ArgumentsContext = new ArgumentsContext(this._ctx, this.state);
		this.enterRule(_localctx, 82, exprParser.RULE_arguments);
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 344;
			this.exprList();
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public exprList(): ExprListContext {
		let _localctx: ExprListContext = new ExprListContext(this._ctx, this.state);
		this.enterRule(_localctx, 84, exprParser.RULE_exprList);
		let _la: number;
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 346;
			this.expr(0);
			this.state = 351;
			this._errHandler.sync(this);
			_la = this._input.LA(1);
			while (_la === exprParser.T__42) {
				{
				{
				this.state = 347;
				this.match(exprParser.T__42);
				this.state = 348;
				this.expr(0);
				}
				}
				this.state = 353;
				this._errHandler.sync(this);
				_la = this._input.LA(1);
			}
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public unaryOperator(): UnaryOperatorContext {
		let _localctx: UnaryOperatorContext = new UnaryOperatorContext(this._ctx, this.state);
		this.enterRule(_localctx, 86, exprParser.RULE_unaryOperator);
		let _la: number;
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 354;
			_la = this._input.LA(1);
			if (!(((((_la - 44)) & ~0x1F) === 0 && ((1 << (_la - 44)) & ((1 << (exprParser.T__43 - 44)) | (1 << (exprParser.T__44 - 44)) | (1 << (exprParser.T__45 - 44)))) !== 0))) {
			this._errHandler.recoverInline(this);
			} else {
				if (this._input.LA(1) === Token.EOF) {
					this.matchedEOF = true;
				}

				this._errHandler.reportMatch(this);
				this.consume();
			}
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public binaryOperatorMultiplicative(): BinaryOperatorMultiplicativeContext {
		let _localctx: BinaryOperatorMultiplicativeContext = new BinaryOperatorMultiplicativeContext(this._ctx, this.state);
		this.enterRule(_localctx, 88, exprParser.RULE_binaryOperatorMultiplicative);
		let _la: number;
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 356;
			_la = this._input.LA(1);
			if (!(_la === exprParser.T__46 || _la === exprParser.TIMES || _la === exprParser.DIV)) {
			this._errHandler.recoverInline(this);
			} else {
				if (this._input.LA(1) === Token.EOF) {
					this.matchedEOF = true;
				}

				this._errHandler.reportMatch(this);
				this.consume();
			}
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public binaryOperatorAdditive(): BinaryOperatorAdditiveContext {
		let _localctx: BinaryOperatorAdditiveContext = new BinaryOperatorAdditiveContext(this._ctx, this.state);
		this.enterRule(_localctx, 90, exprParser.RULE_binaryOperatorAdditive);
		let _la: number;
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 358;
			_la = this._input.LA(1);
			if (!(_la === exprParser.PLUS || _la === exprParser.MINUS)) {
			this._errHandler.recoverInline(this);
			} else {
				if (this._input.LA(1) === Token.EOF) {
					this.matchedEOF = true;
				}

				this._errHandler.reportMatch(this);
				this.consume();
			}
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public binaryOperatorBitwiseShift(): BinaryOperatorBitwiseShiftContext {
		let _localctx: BinaryOperatorBitwiseShiftContext = new BinaryOperatorBitwiseShiftContext(this._ctx, this.state);
		this.enterRule(_localctx, 92, exprParser.RULE_binaryOperatorBitwiseShift);
		let _la: number;
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 360;
			_la = this._input.LA(1);
			if (!(((((_la - 48)) & ~0x1F) === 0 && ((1 << (_la - 48)) & ((1 << (exprParser.T__47 - 48)) | (1 << (exprParser.T__48 - 48)) | (1 << (exprParser.T__49 - 48)) | (1 << (exprParser.T__50 - 48)))) !== 0))) {
			this._errHandler.recoverInline(this);
			} else {
				if (this._input.LA(1) === Token.EOF) {
					this.matchedEOF = true;
				}

				this._errHandler.reportMatch(this);
				this.consume();
			}
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public binaryOperatorRelational(): BinaryOperatorRelationalContext {
		let _localctx: BinaryOperatorRelationalContext = new BinaryOperatorRelationalContext(this._ctx, this.state);
		this.enterRule(_localctx, 94, exprParser.RULE_binaryOperatorRelational);
		let _la: number;
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 362;
			_la = this._input.LA(1);
			if (!(_la === exprParser.T__51 || _la === exprParser.T__52 || _la === exprParser.GT || _la === exprParser.LT)) {
			this._errHandler.recoverInline(this);
			} else {
				if (this._input.LA(1) === Token.EOF) {
					this.matchedEOF = true;
				}

				this._errHandler.reportMatch(this);
				this.consume();
			}
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public binaryOperatorEquality(): BinaryOperatorEqualityContext {
		let _localctx: BinaryOperatorEqualityContext = new BinaryOperatorEqualityContext(this._ctx, this.state);
		this.enterRule(_localctx, 96, exprParser.RULE_binaryOperatorEquality);
		let _la: number;
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 364;
			_la = this._input.LA(1);
			if (!(_la === exprParser.T__53 || _la === exprParser.T__54 || _la === exprParser.EQ)) {
			this._errHandler.recoverInline(this);
			} else {
				if (this._input.LA(1) === Token.EOF) {
					this.matchedEOF = true;
				}

				this._errHandler.reportMatch(this);
				this.consume();
			}
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public binaryOperatorBitwiseAnd(): BinaryOperatorBitwiseAndContext {
		let _localctx: BinaryOperatorBitwiseAndContext = new BinaryOperatorBitwiseAndContext(this._ctx, this.state);
		this.enterRule(_localctx, 98, exprParser.RULE_binaryOperatorBitwiseAnd);
		let _la: number;
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 366;
			_la = this._input.LA(1);
			if (!(_la === exprParser.T__55 || _la === exprParser.T__56)) {
			this._errHandler.recoverInline(this);
			} else {
				if (this._input.LA(1) === Token.EOF) {
					this.matchedEOF = true;
				}

				this._errHandler.reportMatch(this);
				this.consume();
			}
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public binaryOperatorBitwiseXor(): BinaryOperatorBitwiseXorContext {
		let _localctx: BinaryOperatorBitwiseXorContext = new BinaryOperatorBitwiseXorContext(this._ctx, this.state);
		this.enterRule(_localctx, 100, exprParser.RULE_binaryOperatorBitwiseXor);
		let _la: number;
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 368;
			_la = this._input.LA(1);
			if (!(_la === exprParser.T__57 || _la === exprParser.POW)) {
			this._errHandler.recoverInline(this);
			} else {
				if (this._input.LA(1) === Token.EOF) {
					this.matchedEOF = true;
				}

				this._errHandler.reportMatch(this);
				this.consume();
			}
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public binaryOperatorBitwiseOr(): BinaryOperatorBitwiseOrContext {
		let _localctx: BinaryOperatorBitwiseOrContext = new BinaryOperatorBitwiseOrContext(this._ctx, this.state);
		this.enterRule(_localctx, 102, exprParser.RULE_binaryOperatorBitwiseOr);
		let _la: number;
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 370;
			_la = this._input.LA(1);
			if (!(_la === exprParser.T__58 || _la === exprParser.T__59)) {
			this._errHandler.recoverInline(this);
			} else {
				if (this._input.LA(1) === Token.EOF) {
					this.matchedEOF = true;
				}

				this._errHandler.reportMatch(this);
				this.consume();
			}
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public binaryOperatorLogicalAnd(): BinaryOperatorLogicalAndContext {
		let _localctx: BinaryOperatorLogicalAndContext = new BinaryOperatorLogicalAndContext(this._ctx, this.state);
		this.enterRule(_localctx, 104, exprParser.RULE_binaryOperatorLogicalAnd);
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 372;
			this.match(exprParser.T__60);
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public binaryOperatorLogicalOr(): BinaryOperatorLogicalOrContext {
		let _localctx: BinaryOperatorLogicalOrContext = new BinaryOperatorLogicalOrContext(this._ctx, this.state);
		this.enterRule(_localctx, 106, exprParser.RULE_binaryOperatorLogicalOr);
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 374;
			this.match(exprParser.T__61);
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}
	// @RuleVersion(0)
	public binaryOperator(): BinaryOperatorContext {
		let _localctx: BinaryOperatorContext = new BinaryOperatorContext(this._ctx, this.state);
		this.enterRule(_localctx, 108, exprParser.RULE_binaryOperator);
		try {
			this.state = 386;
			this._errHandler.sync(this);
			switch (this._input.LA(1)) {
			case exprParser.T__46:
			case exprParser.TIMES:
			case exprParser.DIV:
				this.enterOuterAlt(_localctx, 1);
				{
				this.state = 376;
				this.binaryOperatorMultiplicative();
				}
				break;
			case exprParser.PLUS:
			case exprParser.MINUS:
				this.enterOuterAlt(_localctx, 2);
				{
				this.state = 377;
				this.binaryOperatorAdditive();
				}
				break;
			case exprParser.T__47:
			case exprParser.T__48:
			case exprParser.T__49:
			case exprParser.T__50:
				this.enterOuterAlt(_localctx, 3);
				{
				this.state = 378;
				this.binaryOperatorBitwiseShift();
				}
				break;
			case exprParser.T__51:
			case exprParser.T__52:
			case exprParser.GT:
			case exprParser.LT:
				this.enterOuterAlt(_localctx, 4);
				{
				this.state = 379;
				this.binaryOperatorRelational();
				}
				break;
			case exprParser.T__53:
			case exprParser.T__54:
			case exprParser.EQ:
				this.enterOuterAlt(_localctx, 5);
				{
				this.state = 380;
				this.binaryOperatorEquality();
				}
				break;
			case exprParser.T__55:
			case exprParser.T__56:
				this.enterOuterAlt(_localctx, 6);
				{
				this.state = 381;
				this.binaryOperatorBitwiseAnd();
				}
				break;
			case exprParser.T__57:
			case exprParser.POW:
				this.enterOuterAlt(_localctx, 7);
				{
				this.state = 382;
				this.binaryOperatorBitwiseXor();
				}
				break;
			case exprParser.T__58:
			case exprParser.T__59:
				this.enterOuterAlt(_localctx, 8);
				{
				this.state = 383;
				this.binaryOperatorBitwiseOr();
				}
				break;
			case exprParser.T__60:
				this.enterOuterAlt(_localctx, 9);
				{
				this.state = 384;
				this.binaryOperatorLogicalAnd();
				}
				break;
			case exprParser.T__61:
				this.enterOuterAlt(_localctx, 10);
				{
				this.state = 385;
				this.binaryOperatorLogicalOr();
				}
				break;
			default:
				throw new NoViableAltException(this);
			}
		}
		catch (re) {
			if (re instanceof RecognitionException) {
				_localctx.exception = re;
				this._errHandler.reportError(this, re);
				this._errHandler.recover(this, re);
			} else {
				throw re;
			}
		}
		finally {
			this.exitRule();
		}
		return _localctx;
	}

	public sempred(_localctx: RuleContext, ruleIndex: number, predIndex: number): boolean {
		switch (ruleIndex) {
		case 40:
			return this.expr_sempred(_localctx as ExprContext, predIndex);
		}
		return true;
	}
	private expr_sempred(_localctx: ExprContext, predIndex: number): boolean {
		switch (predIndex) {
		case 0:
			return this.precpred(this._ctx, 12);

		case 1:
			return this.precpred(this._ctx, 11);

		case 2:
			return this.precpred(this._ctx, 10);

		case 3:
			return this.precpred(this._ctx, 9);

		case 4:
			return this.precpred(this._ctx, 8);

		case 5:
			return this.precpred(this._ctx, 7);

		case 6:
			return this.precpred(this._ctx, 6);

		case 7:
			return this.precpred(this._ctx, 5);

		case 8:
			return this.precpred(this._ctx, 4);

		case 9:
			return this.precpred(this._ctx, 3);
		}
		return true;
	}

	public static readonly _serializedATN: string =
		"\x03\uC91D\uCABA\u058D\uAFBA\u4F53\u0607\uEA8B\uC241\x03^\u0187\x04\x02" +
		"\t\x02\x04\x03\t\x03\x04\x04\t\x04\x04\x05\t\x05\x04\x06\t\x06\x04\x07" +
		"\t\x07\x04\b\t\b\x04\t\t\t\x04\n\t\n\x04\v\t\v\x04\f\t\f\x04\r\t\r\x04" +
		"\x0E\t\x0E\x04\x0F\t\x0F\x04\x10\t\x10\x04\x11\t\x11\x04\x12\t\x12\x04" +
		"\x13\t\x13\x04\x14\t\x14\x04\x15\t\x15\x04\x16\t\x16\x04\x17\t\x17\x04" +
		"\x18\t\x18\x04\x19\t\x19\x04\x1A\t\x1A\x04\x1B\t\x1B\x04\x1C\t\x1C\x04" +
		"\x1D\t\x1D\x04\x1E\t\x1E\x04\x1F\t\x1F\x04 \t \x04!\t!\x04\"\t\"\x04#" +
		"\t#\x04$\t$\x04%\t%\x04&\t&\x04\'\t\'\x04(\t(\x04)\t)\x04*\t*\x04+\t+" +
		"\x04,\t,\x04-\t-\x04.\t.\x04/\t/\x040\t0\x041\t1\x042\t2\x043\t3\x044" +
		"\t4\x045\t5\x046\t6\x047\t7\x048\t8\x03\x02\x03\x02\x03\x03\x03\x03\x03" +
		"\x04\x03\x04\x03\x05\x03\x05\x03\x05\x03\x06\x03\x06\x05\x06|\n\x06\x03" +
		"\x07\x03\x07\x03\x07\x05\x07\x81\n\x07\x03\b\x03\b\x03\b\x03\b\x03\b\x03" +
		"\b\x03\b\x03\b\x03\b\x03\b\x03\b\x03\b\x03\b\x05\b\x90\n\b\x03\t\x03\t" +
		"\x03\n\x03\n\x03\v\x03\v\x03\f\x03\f\x03\r\x03\r\x03\x0E\x03\x0E\x03\x0F" +
		"\x03\x0F\x03\x10\x03\x10\x03\x11\x03\x11\x03\x12\x03\x12\x03\x13\x03\x13" +
		"\x03\x14\x03\x14\x03\x15\x03\x15\x03\x16\x03\x16\x03\x16\x07\x16\xAF\n" +
		"\x16\f\x16\x0E\x16\xB2\v\x16\x03\x16\x03\x16\x03\x17\x03\x17\x05\x17\xB8" +
		"\n\x17\x03\x18\x03\x18\x03\x18\x03\x18\x03\x18\x05\x18\xBF\n\x18\x03\x19" +
		"\x03\x19\x03\x19\x05\x19\xC4\n\x19\x03\x1A\x03\x1A\x03\x1A\x03\x1A\x03" +
		"\x1A\x03\x1B\x03\x1B\x03\x1B\x03\x1B\x03\x1B\x03\x1B\x03\x1B\x03\x1B\x03" +
		"\x1C\x03\x1C\x03\x1C\x03\x1C\x03\x1C\x03\x1C\x03\x1C\x03\x1C\x03\x1D\x03" +
		"\x1D\x03\x1D\x03\x1D\x05\x1D\xDF\n\x1D\x03\x1E\x03\x1E\x03\x1E\x07\x1E" +
		"\xE4\n\x1E\f\x1E\x0E\x1E\xE7\v\x1E\x03\x1F\x03\x1F\x03 \x03 \x03 \x03" +
		" \x03 \x03!\x03!\x03\"\x03\"\x03#\x03#\x03#\x03#\x03#\x03$\x03$\x03%\x03" +
		"%\x03&\x03&\x03&\x03&\x03&\x03&\x03&\x03&\x03&\x03\'\x03\'\x03(\x03(\x03" +
		")\x03)\x05)\u010C\n)\x03*\x03*\x03*\x03*\x05*\u0112\n*\x03*\x03*\x03*" +
		"\x03*\x03*\x03*\x03*\x03*\x03*\x03*\x03*\x03*\x06*\u0120\n*\r*\x0E*\u0121" +
		"\x03*\x03*\x03*\x03*\x03*\x03*\x03*\x03*\x05*\u012C\n*\x03*\x03*\x03*" +
		"\x03*\x03*\x03*\x03*\x03*\x03*\x03*\x03*\x03*\x03*\x03*\x03*\x03*\x03" +
		"*\x03*\x03*\x03*\x03*\x03*\x03*\x03*\x03*\x03*\x03*\x03*\x03*\x03*\x03" +
		"*\x03*\x03*\x03*\x03*\x03*\x03*\x03*\x03*\x03*\x07*\u0156\n*\f*\x0E*\u0159" +
		"\v*\x03+\x03+\x03,\x03,\x03,\x07,\u0160\n,\f,\x0E,\u0163\v,\x03-\x03-" +
		"\x03.\x03.\x03/\x03/\x030\x030\x031\x031\x032\x032\x033\x033\x034\x03" +
		"4\x035\x035\x036\x036\x037\x037\x038\x038\x038\x038\x038\x038\x038\x03" +
		"8\x038\x038\x058\u0185\n8\x038\x02\x02\x03R9\x02\x02\x04\x02\x06\x02\b" +
		"\x02\n\x02\f\x02\x0E\x02\x10\x02\x12\x02\x14\x02\x16\x02\x18\x02\x1A\x02" +
		"\x1C\x02\x1E\x02 \x02\"\x02$\x02&\x02(\x02*\x02,\x02.\x020\x022\x024\x02" +
		"6\x028\x02:\x02<\x02>\x02@\x02B\x02D\x02F\x02H\x02J\x02L\x02N\x02P\x02" +
		"R\x02T\x02V\x02X\x02Z\x02\\\x02^\x02`\x02b\x02d\x02f\x02h\x02j\x02l\x02" +
		"n\x02\x02\x10\x03\x02BC\x03\x02\x04\x05\x03\x02\n$\x03\x02%\'\x03\x02" +
		"(+\x03\x02.0\x04\x0211XY\x03\x02VW\x03\x0225\x04\x0267Z[\x04\x0289\\\\" +
		"\x03\x02:;\x04\x02<<^^\x03\x02=>\x02\u0184\x02p\x03\x02\x02\x02\x04r\x03" +
		"\x02\x02\x02\x06t\x03\x02\x02\x02\bv\x03\x02\x02\x02\n{\x03\x02\x02\x02" +
		"\f\x80\x03\x02\x02\x02\x0E\x8F\x03\x02\x02\x02\x10\x91\x03\x02\x02\x02" +
		"\x12\x93\x03\x02\x02\x02\x14\x95\x03\x02\x02\x02\x16\x97\x03\x02\x02\x02" +
		"\x18\x99\x03\x02\x02\x02\x1A\x9B\x03\x02\x02\x02\x1C\x9D\x03\x02\x02\x02" +
		"\x1E\x9F\x03\x02\x02\x02 \xA1\x03\x02\x02\x02\"\xA3\x03\x02\x02\x02$\xA5" +
		"\x03\x02\x02\x02&\xA7\x03\x02\x02\x02(\xA9\x03\x02\x02\x02*\xAB\x03\x02" +
		"\x02\x02,\xB7\x03\x02\x02\x02.\xBE\x03\x02\x02\x020\xC3\x03\x02\x02\x02" +
		"2\xC5\x03\x02\x02\x024\xCA\x03\x02\x02\x026\xD2\x03\x02\x02\x028\xDA\x03" +
		"\x02\x02\x02:\xE0\x03\x02\x02\x02<\xE8\x03\x02\x02\x02>\xEA\x03\x02\x02" +
		"\x02@\xEF\x03\x02\x02\x02B\xF1\x03\x02\x02\x02D\xF3\x03\x02\x02\x02F\xF8" +
		"\x03\x02\x02\x02H\xFA\x03\x02\x02\x02J\xFC\x03\x02\x02\x02L\u0105\x03" +
		"\x02\x02\x02N\u0107\x03\x02\x02\x02P\u010B\x03\x02\x02\x02R\u012B\x03" +
		"\x02\x02\x02T\u015A\x03\x02\x02\x02V\u015C\x03\x02\x02\x02X\u0164\x03" +
		"\x02\x02\x02Z\u0166\x03\x02\x02\x02\\\u0168\x03\x02\x02\x02^\u016A\x03" +
		"\x02\x02\x02`\u016C\x03\x02\x02\x02b\u016E\x03\x02\x02\x02d\u0170\x03" +
		"\x02\x02\x02f\u0172\x03\x02\x02\x02h\u0174\x03\x02\x02\x02j\u0176\x03" +
		"\x02\x02\x02l\u0178\x03\x02\x02\x02n\u0184\x03\x02\x02\x02pq\t\x02\x02" +
		"\x02q\x03\x03\x02\x02\x02rs\x07E\x02\x02s\x05\x03\x02\x02\x02tu\x07D\x02" +
		"\x02u\x07\x03\x02\x02\x02vw\x07\x03\x02\x02wx\x05:\x1E\x02x\t\x03\x02" +
		"\x02\x02y|\x05\x04\x03\x02z|\x05n8\x02{y\x03\x02\x02\x02{z\x03\x02\x02" +
		"\x02|\v\x03\x02\x02\x02}\x81\x05\b\x05\x02~\x81\x05\x06\x04\x02\x7F\x81" +
		"\x05\x0E\b\x02\x80}\x03\x02\x02\x02\x80~\x03\x02\x02\x02\x80\x7F\x03\x02" +
		"\x02\x02\x81\r\x03\x02\x02\x02\x82\x90\x05\x10\t\x02\x83\x90\x05\x12\n" +
		"\x02\x84\x90\x05\x14\v\x02\x85\x90\x05\x16\f\x02\x86\x90\x05\x18\r\x02" +
		"\x87\x90\x05\x1A\x0E\x02\x88\x90\x05\x1C\x0F\x02\x89\x90\x05\x1E\x10\x02" +
		"\x8A\x90\x05 \x11\x02\x8B\x90\x05\"\x12\x02\x8C\x90\x05$\x13\x02\x8D\x90" +
		"\x05&\x14\x02\x8E\x90\x05(\x15\x02\x8F\x82\x03\x02\x02\x02\x8F\x83\x03" +
		"\x02\x02\x02\x8F\x84\x03\x02\x02\x02\x8F\x85\x03\x02\x02\x02\x8F\x86\x03" +
		"\x02\x02\x02\x8F\x87\x03\x02\x02\x02\x8F\x88\x03\x02\x02\x02\x8F\x89\x03" +
		"\x02\x02\x02\x8F\x8A\x03\x02\x02\x02\x8F\x8B\x03\x02\x02\x02\x8F\x8C\x03" +
		"\x02\x02\x02\x8F\x8D\x03\x02\x02\x02\x8F\x8E\x03\x02\x02\x02\x90\x0F\x03" +
		"\x02\x02\x02\x91\x92\x07F\x02\x02\x92\x11\x03\x02\x02\x02\x93\x94\x07" +
		"G\x02\x02\x94\x13\x03\x02\x02\x02\x95\x96\x07H\x02\x02\x96\x15\x03\x02" +
		"\x02\x02\x97\x98\x07I\x02\x02\x98\x17\x03\x02\x02\x02\x99\x9A\x07J\x02" +
		"\x02\x9A\x19\x03\x02\x02\x02\x9B\x9C\x07K\x02\x02\x9C\x1B\x03\x02\x02" +
		"\x02\x9D\x9E\x07L\x02\x02\x9E\x1D\x03\x02\x02\x02\x9F\xA0\x07M\x02\x02" +
		"\xA0\x1F\x03\x02\x02\x02\xA1\xA2\x07N\x02\x02\xA2!\x03\x02\x02\x02\xA3" +
		"\xA4\x07O\x02\x02\xA4#\x03\x02\x02\x02\xA5\xA6\x07P\x02\x02\xA6%\x03\x02" +
		"\x02\x02\xA7\xA8\x07Q\x02\x02\xA8\'\x03\x02\x02\x02\xA9\xAA\t\x03\x02" +
		"\x02\xAA)\x03\x02\x02\x02\xAB\xB0\x05,\x17\x02\xAC\xAD\x07\x06\x02\x02" +
		"\xAD\xAF\x05,\x17\x02\xAE\xAC\x03\x02\x02\x02\xAF\xB2\x03\x02\x02\x02" +
		"\xB0\xAE\x03\x02\x02\x02\xB0\xB1\x03\x02\x02\x02\xB1\xB3\x03\x02\x02\x02" +
		"\xB2\xB0\x03\x02\x02\x02\xB3\xB4\x07\x06\x02\x02\xB4+\x03\x02\x02\x02" +
		"\xB5\xB8\x05R*\x02\xB6\xB8\x05.\x18\x02\xB7\xB5\x03\x02\x02\x02\xB7\xB6" +
		"\x03\x02\x02\x02\xB8-\x03\x02\x02\x02\xB9\xBF\x050\x19\x02\xBA\xBF\x05" +
		"8\x1D\x02\xBB\xBF\x05> \x02\xBC\xBF\x05D#\x02\xBD\xBF\x05J&\x02\xBE\xB9" +
		"\x03\x02\x02\x02\xBE\xBA\x03\x02\x02\x02\xBE\xBB\x03\x02\x02\x02\xBE\xBC" +
		"\x03\x02\x02\x02\xBE\xBD\x03\x02\x02\x02\xBF/\x03\x02\x02\x02\xC0\xC4" +
		"\x052\x1A\x02\xC1\xC4\x054\x1B\x02\xC2\xC4\x056\x1C\x02\xC3\xC0\x03\x02" +
		"\x02\x02\xC3\xC1\x03\x02\x02\x02\xC3\xC2\x03\x02\x02\x02\xC41\x03\x02" +
		"\x02\x02\xC5\xC6\x07\x03\x02\x02\xC6\xC7\x05:\x1E\x02\xC7\xC8\x07\x07" +
		"\x02\x02\xC8\xC9\x05R*\x02\xC93\x03\x02\x02\x02\xCA\xCB\x07\b\x02\x02" +
		"\xCB\xCC\x07R\x02\x02\xCC\xCD\x07\x03\x02\x02\xCD\xCE\x05:\x1E\x02\xCE" +
		"\xCF\x07S\x02\x02\xCF\xD0\x07\x07\x02\x02\xD0\xD1\x05R*\x02\xD15\x03\x02" +
		"\x02\x02\xD2\xD3\x07\t\x02\x02\xD3\xD4\x07R\x02\x02\xD4\xD5\x07\x03\x02" +
		"\x02\xD5\xD6\x05:\x1E\x02\xD6\xD7\x07S\x02\x02\xD7\xD8\x07\x07\x02\x02" +
		"\xD8\xD9\x05R*\x02\xD97\x03\x02\x02\x02\xDA\xDB\x05<\x1F\x02\xDB\xDE\x05" +
		":\x1E\x02\xDC\xDD\x07\\\x02\x02\xDD\xDF\x05R*\x02\xDE\xDC\x03\x02\x02" +
		"\x02\xDE\xDF\x03\x02\x02\x02\xDF9\x03\x02\x02\x02\xE0\xE5\x07E\x02\x02" +
		"\xE1\xE2\x07]\x02\x02\xE2\xE4\x07E\x02\x02\xE3\xE1\x03\x02\x02\x02\xE4" +
		"\xE7\x03\x02\x02\x02\xE5\xE3\x03\x02\x02\x02\xE5\xE6\x03\x02\x02\x02\xE6" +
		";\x03\x02\x02\x02\xE7\xE5\x03\x02\x02\x02\xE8\xE9\t\x04\x02\x02\xE9=\x03" +
		"\x02\x02\x02\xEA\xEB\x05@!\x02\xEB\xEC\x05:\x1E\x02\xEC\xED\x07\\\x02" +
		"\x02\xED\xEE\x05R*\x02\xEE?\x03\x02\x02\x02\xEF\xF0\t\x05\x02\x02\xF0" +
		"A\x03\x02\x02\x02\xF1\xF2\x07E\x02\x02\xF2C\x03\x02\x02\x02\xF3\xF4\x05" +
		"F$\x02\xF4\xF5\x05:\x1E\x02\xF5\xF6\x07\\\x02\x02\xF6\xF7\x05R*\x02\xF7" +
		"E\x03\x02\x02\x02\xF8\xF9\t\x06\x02\x02\xF9G\x03\x02\x02\x02\xFA\xFB\x07" +
		"E\x02\x02\xFBI\x03\x02\x02\x02\xFC\xFD\x07,\x02\x02\xFD\xFE\x07R\x02\x02" +
		"\xFE\xFF\x05L\'\x02\xFF\u0100\x07-\x02\x02\u0100\u0101\x05N(\x02\u0101" +
		"\u0102\x07-\x02\x02\u0102\u0103\x05P)\x02\u0103\u0104\x07S\x02\x02\u0104" +
		"K\x03\x02\x02\x02\u0105\u0106\x05R*\x02\u0106M\x03\x02\x02\x02\u0107\u0108" +
		"\x05R*\x02\u0108O\x03\x02\x02\x02\u0109\u010C\x05\b\x05\x02\u010A\u010C" +
		"\x05\x06\x04\x02\u010B\u0109\x03\x02\x02\x02\u010B\u010A\x03\x02\x02\x02" +
		"\u010CQ\x03\x02\x02\x02\u010D\u010E\b*\x01\x02\u010E\u010F\x05\n\x06\x02" +
		"\u010F\u0111\x07R\x02\x02\u0110\u0112\x05T+\x02\u0111\u0110\x03\x02\x02" +
		"\x02\u0111\u0112\x03\x02\x02\x02\u0112\u0113\x03\x02\x02\x02\u0113\u0114" +
		"\x07S\x02\x02\u0114\u012C\x03\x02\x02\x02\u0115\u0116\x07R\x02\x02\u0116" +
		"\u0117\x05<\x1F\x02\u0117\u0118\x07S\x02\x02\u0118\u0119\x05R*\x11\u0119" +
		"\u012C\x03\x02\x02\x02\u011A\u011F\x05\b\x05\x02\u011B\u011C\x07T\x02" +
		"\x02\u011C\u011D\x05R*\x02\u011D\u011E\x07U\x02\x02\u011E\u0120\x03\x02" +
		"\x02\x02\u011F\u011B\x03\x02\x02\x02\u0120\u0121\x03\x02\x02\x02\u0121" +
		"\u011F\x03\x02\x02\x02\u0121\u0122\x03\x02\x02\x02\u0122\u012C\x03\x02" +
		"\x02\x02\u0123\u0124\x05X-\x02\u0124\u0125\x05R*\x0F\u0125\u012C\x03\x02" +
		"\x02\x02\u0126\u012C\x05\f\x07\x02\u0127\u0128\x07R\x02\x02\u0128\u0129" +
		"\x05R*\x02\u0129\u012A\x07S\x02\x02\u012A\u012C\x03\x02\x02\x02\u012B" +
		"\u010D\x03\x02\x02\x02\u012B\u0115\x03\x02\x02\x02\u012B\u011A\x03\x02" +
		"\x02\x02\u012B\u0123\x03\x02\x02\x02\u012B\u0126\x03\x02\x02\x02\u012B" +
		"\u0127\x03\x02\x02\x02\u012C\u0157\x03\x02\x02\x02\u012D\u012E\f\x0E\x02" +
		"\x02\u012E\u012F\x05Z.\x02\u012F\u0130\x05R*\x0F\u0130\u0156\x03\x02\x02" +
		"\x02\u0131\u0132\f\r\x02\x02\u0132\u0133\x05\\/\x02\u0133\u0134\x05R*" +
		"\x0E\u0134\u0156\x03\x02\x02\x02\u0135\u0136\f\f\x02\x02\u0136\u0137\x05" +
		"^0\x02\u0137\u0138\x05R*\r\u0138\u0156\x03\x02\x02\x02\u0139\u013A\f\v" +
		"\x02\x02\u013A\u013B\x05`1\x02\u013B\u013C\x05R*\f\u013C\u0156\x03\x02" +
		"\x02\x02\u013D\u013E\f\n\x02\x02\u013E\u013F\x05d3\x02\u013F\u0140\x05" +
		"R*\v\u0140\u0156\x03\x02\x02\x02\u0141\u0142\f\t\x02\x02\u0142\u0143\x05" +
		"f4\x02\u0143\u0144\x05R*\n\u0144\u0156\x03\x02\x02\x02\u0145\u0146\f\b" +
		"\x02\x02\u0146\u0147\x05h5\x02\u0147\u0148\x05R*\t\u0148\u0156\x03\x02" +
		"\x02\x02\u0149\u014A\f\x07\x02\x02\u014A\u014B\x05j6\x02\u014B\u014C\x05" +
		"R*\b\u014C\u0156\x03\x02\x02\x02\u014D\u014E\f\x06\x02\x02\u014E\u014F" +
		"\x05l7\x02\u014F\u0150\x05R*\x07\u0150\u0156\x03\x02\x02\x02\u0151\u0152" +
		"\f\x05\x02\x02\u0152\u0153\x05b2\x02\u0153\u0154\x05R*\x06\u0154\u0156" +
		"\x03\x02\x02\x02\u0155\u012D\x03\x02\x02\x02\u0155\u0131\x03\x02\x02\x02" +
		"\u0155\u0135\x03\x02\x02\x02\u0155\u0139\x03\x02\x02\x02\u0155\u013D\x03" +
		"\x02\x02\x02\u0155\u0141\x03\x02\x02\x02\u0155\u0145\x03\x02\x02\x02\u0155" +
		"\u0149\x03\x02\x02\x02\u0155\u014D\x03\x02\x02\x02\u0155\u0151\x03\x02" +
		"\x02\x02\u0156\u0159\x03\x02\x02\x02\u0157\u0155\x03\x02\x02\x02\u0157" +
		"\u0158\x03\x02\x02\x02\u0158S\x03\x02\x02\x02\u0159\u0157\x03\x02\x02" +
		"\x02\u015A\u015B\x05V,\x02\u015BU\x03\x02\x02\x02\u015C\u0161\x05R*\x02" +
		"\u015D\u015E\x07-\x02\x02\u015E\u0160\x05R*\x02\u015F\u015D\x03\x02\x02" +
		"\x02\u0160\u0163\x03\x02\x02\x02\u0161\u015F\x03\x02\x02\x02\u0161\u0162" +
		"\x03\x02\x02\x02\u0162W\x03\x02\x02\x02\u0163\u0161\x03\x02\x02\x02\u0164" +
		"\u0165\t\x07\x02\x02\u0165Y\x03\x02\x02\x02\u0166\u0167\t\b\x02\x02\u0167" +
		"[\x03\x02\x02\x02\u0168\u0169\t\t\x02\x02\u0169]\x03\x02\x02\x02\u016A" +
		"\u016B\t\n\x02\x02\u016B_\x03\x02\x02\x02\u016C\u016D\t\v\x02\x02\u016D" +
		"a\x03\x02\x02\x02\u016E\u016F\t\f\x02\x02\u016Fc\x03\x02\x02\x02\u0170" +
		"\u0171\t\r\x02\x02\u0171e\x03\x02\x02\x02\u0172\u0173\t\x0E\x02\x02\u0173" +
		"g\x03\x02\x02\x02\u0174\u0175\t\x0F\x02\x02\u0175i\x03\x02\x02\x02\u0176" +
		"\u0177\x07?\x02\x02\u0177k\x03\x02\x02\x02\u0178\u0179\x07@\x02\x02\u0179" +
		"m\x03\x02\x02\x02\u017A\u0185\x05Z.\x02\u017B\u0185\x05\\/\x02\u017C\u0185" +
		"\x05^0\x02\u017D\u0185\x05`1\x02\u017E\u0185\x05b2\x02\u017F\u0185\x05" +
		"d3\x02\u0180\u0185\x05f4\x02\u0181\u0185\x05h5\x02\u0182\u0185\x05j6\x02" +
		"\u0183\u0185\x05l7\x02\u0184\u017A\x03\x02\x02\x02\u0184\u017B\x03\x02" +
		"\x02\x02\u0184\u017C\x03\x02\x02\x02\u0184\u017D\x03\x02\x02\x02\u0184" +
		"\u017E\x03\x02\x02\x02\u0184\u017F\x03\x02\x02\x02\u0184\u0180\x03\x02" +
		"\x02\x02\u0184\u0181\x03\x02\x02\x02\u0184\u0182\x03\x02\x02\x02\u0184" +
		"\u0183\x03\x02\x02\x02\u0185o\x03\x02\x02\x02\x13{\x80\x8F\xB0\xB7\xBE" +
		"\xC3\xDE\xE5\u010B\u0111\u0121\u012B\u0155\u0157\u0161\u0184";
	public static __ATN: ATN;
	public static get _ATN(): ATN {
		if (!exprParser.__ATN) {
			exprParser.__ATN = new ATNDeserializer().deserialize(Utils.toCharArray(exprParser._serializedATN));
		}

		return exprParser.__ATN;
	}

}

export class CommentContext extends ParserRuleContext {
	public BLOCK_COMMENT(): TerminalNode | undefined { return this.tryGetToken(exprParser.BLOCK_COMMENT, 0); }
	public LINE_COMMENT(): TerminalNode | undefined { return this.tryGetToken(exprParser.LINE_COMMENT, 0); }
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_comment; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterComment) {
			listener.enterComment(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitComment) {
			listener.exitComment(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitComment) {
			return visitor.visitComment(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class IdentifierContext extends ParserRuleContext {
	public IDENTIFIER(): TerminalNode { return this.getToken(exprParser.IDENTIFIER, 0); }
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_identifier; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterIdentifier) {
			listener.enterIdentifier(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitIdentifier) {
			listener.exitIdentifier(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitIdentifier) {
			return visitor.visitIdentifier(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class TagContext extends ParserRuleContext {
	public TAG(): TerminalNode { return this.getToken(exprParser.TAG, 0); }
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_tag; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterTag) {
			listener.enterTag(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitTag) {
			listener.exitTag(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitTag) {
			return visitor.visitTag(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class StorageContext extends ParserRuleContext {
	public storageName(): StorageNameContext {
		return this.getRuleContext(0, StorageNameContext);
	}
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_storage; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterStorage) {
			listener.enterStorage(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitStorage) {
			listener.exitStorage(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitStorage) {
			return visitor.visitStorage(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class FunctionNameContext extends ParserRuleContext {
	public identifier(): IdentifierContext | undefined {
		return this.tryGetRuleContext(0, IdentifierContext);
	}
	public binaryOperator(): BinaryOperatorContext | undefined {
		return this.tryGetRuleContext(0, BinaryOperatorContext);
	}
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_functionName; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterFunctionName) {
			listener.enterFunctionName(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitFunctionName) {
			listener.exitFunctionName(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitFunctionName) {
			return visitor.visitFunctionName(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class TerminalContext extends ParserRuleContext {
	public storage(): StorageContext | undefined {
		return this.tryGetRuleContext(0, StorageContext);
	}
	public tag(): TagContext | undefined {
		return this.tryGetRuleContext(0, TagContext);
	}
	public literal(): LiteralContext | undefined {
		return this.tryGetRuleContext(0, LiteralContext);
	}
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_terminal; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterTerminal) {
			listener.enterTerminal(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitTerminal) {
			listener.exitTerminal(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitTerminal) {
			return visitor.visitTerminal(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class LiteralContext extends ParserRuleContext {
	public literalSingle(): LiteralSingleContext | undefined {
		return this.tryGetRuleContext(0, LiteralSingleContext);
	}
	public literalDouble(): LiteralDoubleContext | undefined {
		return this.tryGetRuleContext(0, LiteralDoubleContext);
	}
	public literalSbyte(): LiteralSbyteContext | undefined {
		return this.tryGetRuleContext(0, LiteralSbyteContext);
	}
	public literalByte(): LiteralByteContext | undefined {
		return this.tryGetRuleContext(0, LiteralByteContext);
	}
	public literalInt16(): LiteralInt16Context | undefined {
		return this.tryGetRuleContext(0, LiteralInt16Context);
	}
	public literalUint16(): LiteralUint16Context | undefined {
		return this.tryGetRuleContext(0, LiteralUint16Context);
	}
	public literalInt32(): LiteralInt32Context | undefined {
		return this.tryGetRuleContext(0, LiteralInt32Context);
	}
	public literalUint32(): LiteralUint32Context | undefined {
		return this.tryGetRuleContext(0, LiteralUint32Context);
	}
	public literalInt64(): LiteralInt64Context | undefined {
		return this.tryGetRuleContext(0, LiteralInt64Context);
	}
	public literalUint64(): LiteralUint64Context | undefined {
		return this.tryGetRuleContext(0, LiteralUint64Context);
	}
	public literalChar(): LiteralCharContext | undefined {
		return this.tryGetRuleContext(0, LiteralCharContext);
	}
	public literalString(): LiteralStringContext | undefined {
		return this.tryGetRuleContext(0, LiteralStringContext);
	}
	public literalBool(): LiteralBoolContext | undefined {
		return this.tryGetRuleContext(0, LiteralBoolContext);
	}
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_literal; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterLiteral) {
			listener.enterLiteral(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitLiteral) {
			listener.exitLiteral(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitLiteral) {
			return visitor.visitLiteral(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class LiteralSingleContext extends ParserRuleContext {
	public SINGLE(): TerminalNode { return this.getToken(exprParser.SINGLE, 0); }
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_literalSingle; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterLiteralSingle) {
			listener.enterLiteralSingle(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitLiteralSingle) {
			listener.exitLiteralSingle(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitLiteralSingle) {
			return visitor.visitLiteralSingle(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class LiteralDoubleContext extends ParserRuleContext {
	public DOUBLE(): TerminalNode { return this.getToken(exprParser.DOUBLE, 0); }
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_literalDouble; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterLiteralDouble) {
			listener.enterLiteralDouble(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitLiteralDouble) {
			listener.exitLiteralDouble(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitLiteralDouble) {
			return visitor.visitLiteralDouble(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class LiteralSbyteContext extends ParserRuleContext {
	public SBYTE(): TerminalNode { return this.getToken(exprParser.SBYTE, 0); }
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_literalSbyte; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterLiteralSbyte) {
			listener.enterLiteralSbyte(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitLiteralSbyte) {
			listener.exitLiteralSbyte(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitLiteralSbyte) {
			return visitor.visitLiteralSbyte(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class LiteralByteContext extends ParserRuleContext {
	public BYTE(): TerminalNode { return this.getToken(exprParser.BYTE, 0); }
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_literalByte; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterLiteralByte) {
			listener.enterLiteralByte(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitLiteralByte) {
			listener.exitLiteralByte(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitLiteralByte) {
			return visitor.visitLiteralByte(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class LiteralInt16Context extends ParserRuleContext {
	public INT16(): TerminalNode { return this.getToken(exprParser.INT16, 0); }
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_literalInt16; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterLiteralInt16) {
			listener.enterLiteralInt16(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitLiteralInt16) {
			listener.exitLiteralInt16(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitLiteralInt16) {
			return visitor.visitLiteralInt16(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class LiteralUint16Context extends ParserRuleContext {
	public UINT16(): TerminalNode { return this.getToken(exprParser.UINT16, 0); }
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_literalUint16; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterLiteralUint16) {
			listener.enterLiteralUint16(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitLiteralUint16) {
			listener.exitLiteralUint16(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitLiteralUint16) {
			return visitor.visitLiteralUint16(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class LiteralInt32Context extends ParserRuleContext {
	public INT32(): TerminalNode { return this.getToken(exprParser.INT32, 0); }
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_literalInt32; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterLiteralInt32) {
			listener.enterLiteralInt32(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitLiteralInt32) {
			listener.exitLiteralInt32(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitLiteralInt32) {
			return visitor.visitLiteralInt32(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class LiteralUint32Context extends ParserRuleContext {
	public UINT32(): TerminalNode { return this.getToken(exprParser.UINT32, 0); }
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_literalUint32; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterLiteralUint32) {
			listener.enterLiteralUint32(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitLiteralUint32) {
			listener.exitLiteralUint32(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitLiteralUint32) {
			return visitor.visitLiteralUint32(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class LiteralInt64Context extends ParserRuleContext {
	public INT64(): TerminalNode { return this.getToken(exprParser.INT64, 0); }
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_literalInt64; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterLiteralInt64) {
			listener.enterLiteralInt64(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitLiteralInt64) {
			listener.exitLiteralInt64(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitLiteralInt64) {
			return visitor.visitLiteralInt64(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class LiteralUint64Context extends ParserRuleContext {
	public UINT64(): TerminalNode { return this.getToken(exprParser.UINT64, 0); }
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_literalUint64; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterLiteralUint64) {
			listener.enterLiteralUint64(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitLiteralUint64) {
			listener.exitLiteralUint64(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitLiteralUint64) {
			return visitor.visitLiteralUint64(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class LiteralCharContext extends ParserRuleContext {
	public CHAR(): TerminalNode { return this.getToken(exprParser.CHAR, 0); }
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_literalChar; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterLiteralChar) {
			listener.enterLiteralChar(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitLiteralChar) {
			listener.exitLiteralChar(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitLiteralChar) {
			return visitor.visitLiteralChar(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class LiteralStringContext extends ParserRuleContext {
	public STRING(): TerminalNode { return this.getToken(exprParser.STRING, 0); }
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_literalString; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterLiteralString) {
			listener.enterLiteralString(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitLiteralString) {
			listener.exitLiteralString(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitLiteralString) {
			return visitor.visitLiteralString(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class LiteralBoolContext extends ParserRuleContext {
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_literalBool; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterLiteralBool) {
			listener.enterLiteralBool(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitLiteralBool) {
			listener.exitLiteralBool(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitLiteralBool) {
			return visitor.visitLiteralBool(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class ToplevelsContext extends ParserRuleContext {
	public toplevel(): ToplevelContext[];
	public toplevel(i: number): ToplevelContext;
	public toplevel(i?: number): ToplevelContext | ToplevelContext[] {
		if (i === undefined) {
			return this.getRuleContexts(ToplevelContext);
		} else {
			return this.getRuleContext(i, ToplevelContext);
		}
	}
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_toplevels; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterToplevels) {
			listener.enterToplevels(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitToplevels) {
			listener.exitToplevels(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitToplevels) {
			return visitor.visitToplevels(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class ToplevelContext extends ParserRuleContext {
	public expr(): ExprContext | undefined {
		return this.tryGetRuleContext(0, ExprContext);
	}
	public statement(): StatementContext | undefined {
		return this.tryGetRuleContext(0, StatementContext);
	}
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_toplevel; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterToplevel) {
			listener.enterToplevel(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitToplevel) {
			listener.exitToplevel(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitToplevel) {
			return visitor.visitToplevel(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class StatementContext extends ParserRuleContext {
	public assign(): AssignContext | undefined {
		return this.tryGetRuleContext(0, AssignContext);
	}
	public varDecl(): VarDeclContext | undefined {
		return this.tryGetRuleContext(0, VarDeclContext);
	}
	public timerDecl(): TimerDeclContext | undefined {
		return this.tryGetRuleContext(0, TimerDeclContext);
	}
	public counterDecl(): CounterDeclContext | undefined {
		return this.tryGetRuleContext(0, CounterDeclContext);
	}
	public copyStatement(): CopyStatementContext | undefined {
		return this.tryGetRuleContext(0, CopyStatementContext);
	}
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_statement; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterStatement) {
			listener.enterStatement(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitStatement) {
			listener.exitStatement(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitStatement) {
			return visitor.visitStatement(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class AssignContext extends ParserRuleContext {
	public normalAssign(): NormalAssignContext | undefined {
		return this.tryGetRuleContext(0, NormalAssignContext);
	}
	public risingAssign(): RisingAssignContext | undefined {
		return this.tryGetRuleContext(0, RisingAssignContext);
	}
	public fallingAssign(): FallingAssignContext | undefined {
		return this.tryGetRuleContext(0, FallingAssignContext);
	}
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_assign; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterAssign) {
			listener.enterAssign(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitAssign) {
			listener.exitAssign(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitAssign) {
			return visitor.visitAssign(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class NormalAssignContext extends ParserRuleContext {
	public storageName(): StorageNameContext {
		return this.getRuleContext(0, StorageNameContext);
	}
	public expr(): ExprContext {
		return this.getRuleContext(0, ExprContext);
	}
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_normalAssign; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterNormalAssign) {
			listener.enterNormalAssign(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitNormalAssign) {
			listener.exitNormalAssign(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitNormalAssign) {
			return visitor.visitNormalAssign(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class RisingAssignContext extends ParserRuleContext {
	public LPAREN(): TerminalNode { return this.getToken(exprParser.LPAREN, 0); }
	public storageName(): StorageNameContext {
		return this.getRuleContext(0, StorageNameContext);
	}
	public RPAREN(): TerminalNode { return this.getToken(exprParser.RPAREN, 0); }
	public expr(): ExprContext {
		return this.getRuleContext(0, ExprContext);
	}
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_risingAssign; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterRisingAssign) {
			listener.enterRisingAssign(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitRisingAssign) {
			listener.exitRisingAssign(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitRisingAssign) {
			return visitor.visitRisingAssign(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class FallingAssignContext extends ParserRuleContext {
	public LPAREN(): TerminalNode { return this.getToken(exprParser.LPAREN, 0); }
	public storageName(): StorageNameContext {
		return this.getRuleContext(0, StorageNameContext);
	}
	public RPAREN(): TerminalNode { return this.getToken(exprParser.RPAREN, 0); }
	public expr(): ExprContext {
		return this.getRuleContext(0, ExprContext);
	}
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_fallingAssign; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterFallingAssign) {
			listener.enterFallingAssign(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitFallingAssign) {
			listener.exitFallingAssign(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitFallingAssign) {
			return visitor.visitFallingAssign(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class VarDeclContext extends ParserRuleContext {
	public type(): TypeContext {
		return this.getRuleContext(0, TypeContext);
	}
	public storageName(): StorageNameContext {
		return this.getRuleContext(0, StorageNameContext);
	}
	public EQ(): TerminalNode | undefined { return this.tryGetToken(exprParser.EQ, 0); }
	public expr(): ExprContext | undefined {
		return this.tryGetRuleContext(0, ExprContext);
	}
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_varDecl; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterVarDecl) {
			listener.enterVarDecl(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitVarDecl) {
			listener.exitVarDecl(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitVarDecl) {
			return visitor.visitVarDecl(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class StorageNameContext extends ParserRuleContext {
	public IDENTIFIER(): TerminalNode[];
	public IDENTIFIER(i: number): TerminalNode;
	public IDENTIFIER(i?: number): TerminalNode | TerminalNode[] {
		if (i === undefined) {
			return this.getTokens(exprParser.IDENTIFIER);
		} else {
			return this.getToken(exprParser.IDENTIFIER, i);
		}
	}
	public POINT(): TerminalNode[];
	public POINT(i: number): TerminalNode;
	public POINT(i?: number): TerminalNode | TerminalNode[] {
		if (i === undefined) {
			return this.getTokens(exprParser.POINT);
		} else {
			return this.getToken(exprParser.POINT, i);
		}
	}
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_storageName; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterStorageName) {
			listener.enterStorageName(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitStorageName) {
			listener.exitStorageName(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitStorageName) {
			return visitor.visitStorageName(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class TypeContext extends ParserRuleContext {
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_type; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterType) {
			listener.enterType(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitType) {
			listener.exitType(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitType) {
			return visitor.visitType(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class TimerDeclContext extends ParserRuleContext {
	public timerType(): TimerTypeContext {
		return this.getRuleContext(0, TimerTypeContext);
	}
	public storageName(): StorageNameContext {
		return this.getRuleContext(0, StorageNameContext);
	}
	public EQ(): TerminalNode { return this.getToken(exprParser.EQ, 0); }
	public expr(): ExprContext {
		return this.getRuleContext(0, ExprContext);
	}
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_timerDecl; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterTimerDecl) {
			listener.enterTimerDecl(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitTimerDecl) {
			listener.exitTimerDecl(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitTimerDecl) {
			return visitor.visitTimerDecl(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class TimerTypeContext extends ParserRuleContext {
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_timerType; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterTimerType) {
			listener.enterTimerType(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitTimerType) {
			listener.exitTimerType(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitTimerType) {
			return visitor.visitTimerType(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class TimerNameContext extends ParserRuleContext {
	public IDENTIFIER(): TerminalNode { return this.getToken(exprParser.IDENTIFIER, 0); }
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_timerName; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterTimerName) {
			listener.enterTimerName(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitTimerName) {
			listener.exitTimerName(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitTimerName) {
			return visitor.visitTimerName(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class CounterDeclContext extends ParserRuleContext {
	public counterType(): CounterTypeContext {
		return this.getRuleContext(0, CounterTypeContext);
	}
	public storageName(): StorageNameContext {
		return this.getRuleContext(0, StorageNameContext);
	}
	public EQ(): TerminalNode { return this.getToken(exprParser.EQ, 0); }
	public expr(): ExprContext {
		return this.getRuleContext(0, ExprContext);
	}
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_counterDecl; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterCounterDecl) {
			listener.enterCounterDecl(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitCounterDecl) {
			listener.exitCounterDecl(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitCounterDecl) {
			return visitor.visitCounterDecl(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class CounterTypeContext extends ParserRuleContext {
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_counterType; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterCounterType) {
			listener.enterCounterType(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitCounterType) {
			listener.exitCounterType(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitCounterType) {
			return visitor.visitCounterType(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class CounterNameContext extends ParserRuleContext {
	public IDENTIFIER(): TerminalNode { return this.getToken(exprParser.IDENTIFIER, 0); }
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_counterName; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterCounterName) {
			listener.enterCounterName(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitCounterName) {
			listener.exitCounterName(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitCounterName) {
			return visitor.visitCounterName(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class CopyStatementContext extends ParserRuleContext {
	public LPAREN(): TerminalNode { return this.getToken(exprParser.LPAREN, 0); }
	public copyCondition(): CopyConditionContext {
		return this.getRuleContext(0, CopyConditionContext);
	}
	public copySource(): CopySourceContext {
		return this.getRuleContext(0, CopySourceContext);
	}
	public copyTarget(): CopyTargetContext {
		return this.getRuleContext(0, CopyTargetContext);
	}
	public RPAREN(): TerminalNode { return this.getToken(exprParser.RPAREN, 0); }
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_copyStatement; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterCopyStatement) {
			listener.enterCopyStatement(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitCopyStatement) {
			listener.exitCopyStatement(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitCopyStatement) {
			return visitor.visitCopyStatement(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class CopyConditionContext extends ParserRuleContext {
	public expr(): ExprContext {
		return this.getRuleContext(0, ExprContext);
	}
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_copyCondition; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterCopyCondition) {
			listener.enterCopyCondition(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitCopyCondition) {
			listener.exitCopyCondition(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitCopyCondition) {
			return visitor.visitCopyCondition(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class CopySourceContext extends ParserRuleContext {
	public expr(): ExprContext {
		return this.getRuleContext(0, ExprContext);
	}
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_copySource; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterCopySource) {
			listener.enterCopySource(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitCopySource) {
			listener.exitCopySource(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitCopySource) {
			return visitor.visitCopySource(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class CopyTargetContext extends ParserRuleContext {
	public storage(): StorageContext | undefined {
		return this.tryGetRuleContext(0, StorageContext);
	}
	public tag(): TagContext | undefined {
		return this.tryGetRuleContext(0, TagContext);
	}
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_copyTarget; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterCopyTarget) {
			listener.enterCopyTarget(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitCopyTarget) {
			listener.exitCopyTarget(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitCopyTarget) {
			return visitor.visitCopyTarget(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class ExprContext extends ParserRuleContext {
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_expr; }
	public copyFrom(ctx: ExprContext): void {
		super.copyFrom(ctx);
	}
}
export class FunctionCallExprContext extends ExprContext {
	public functionName(): FunctionNameContext {
		return this.getRuleContext(0, FunctionNameContext);
	}
	public LPAREN(): TerminalNode { return this.getToken(exprParser.LPAREN, 0); }
	public RPAREN(): TerminalNode { return this.getToken(exprParser.RPAREN, 0); }
	public arguments(): ArgumentsContext | undefined {
		return this.tryGetRuleContext(0, ArgumentsContext);
	}
	constructor(ctx: ExprContext) {
		super(ctx.parent, ctx.invokingState);
		this.copyFrom(ctx);
	}
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterFunctionCallExpr) {
			listener.enterFunctionCallExpr(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitFunctionCallExpr) {
			listener.exitFunctionCallExpr(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitFunctionCallExpr) {
			return visitor.visitFunctionCallExpr(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}
export class CastingExprContext extends ExprContext {
	public LPAREN(): TerminalNode { return this.getToken(exprParser.LPAREN, 0); }
	public type(): TypeContext {
		return this.getRuleContext(0, TypeContext);
	}
	public RPAREN(): TerminalNode { return this.getToken(exprParser.RPAREN, 0); }
	public expr(): ExprContext {
		return this.getRuleContext(0, ExprContext);
	}
	constructor(ctx: ExprContext) {
		super(ctx.parent, ctx.invokingState);
		this.copyFrom(ctx);
	}
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterCastingExpr) {
			listener.enterCastingExpr(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitCastingExpr) {
			listener.exitCastingExpr(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitCastingExpr) {
			return visitor.visitCastingExpr(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}
export class ArrayReferenceExprContext extends ExprContext {
	public storage(): StorageContext {
		return this.getRuleContext(0, StorageContext);
	}
	public LBACKET(): TerminalNode[];
	public LBACKET(i: number): TerminalNode;
	public LBACKET(i?: number): TerminalNode | TerminalNode[] {
		if (i === undefined) {
			return this.getTokens(exprParser.LBACKET);
		} else {
			return this.getToken(exprParser.LBACKET, i);
		}
	}
	public expr(): ExprContext[];
	public expr(i: number): ExprContext;
	public expr(i?: number): ExprContext | ExprContext[] {
		if (i === undefined) {
			return this.getRuleContexts(ExprContext);
		} else {
			return this.getRuleContext(i, ExprContext);
		}
	}
	public RBACKET(): TerminalNode[];
	public RBACKET(i: number): TerminalNode;
	public RBACKET(i?: number): TerminalNode | TerminalNode[] {
		if (i === undefined) {
			return this.getTokens(exprParser.RBACKET);
		} else {
			return this.getToken(exprParser.RBACKET, i);
		}
	}
	constructor(ctx: ExprContext) {
		super(ctx.parent, ctx.invokingState);
		this.copyFrom(ctx);
	}
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterArrayReferenceExpr) {
			listener.enterArrayReferenceExpr(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitArrayReferenceExpr) {
			listener.exitArrayReferenceExpr(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitArrayReferenceExpr) {
			return visitor.visitArrayReferenceExpr(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}
export class UnaryExprContext extends ExprContext {
	public unaryOperator(): UnaryOperatorContext {
		return this.getRuleContext(0, UnaryOperatorContext);
	}
	public expr(): ExprContext {
		return this.getRuleContext(0, ExprContext);
	}
	constructor(ctx: ExprContext) {
		super(ctx.parent, ctx.invokingState);
		this.copyFrom(ctx);
	}
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterUnaryExpr) {
			listener.enterUnaryExpr(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitUnaryExpr) {
			listener.exitUnaryExpr(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitUnaryExpr) {
			return visitor.visitUnaryExpr(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}
export class BinaryExprMultiplicativeContext extends ExprContext {
	public expr(): ExprContext[];
	public expr(i: number): ExprContext;
	public expr(i?: number): ExprContext | ExprContext[] {
		if (i === undefined) {
			return this.getRuleContexts(ExprContext);
		} else {
			return this.getRuleContext(i, ExprContext);
		}
	}
	public binaryOperatorMultiplicative(): BinaryOperatorMultiplicativeContext {
		return this.getRuleContext(0, BinaryOperatorMultiplicativeContext);
	}
	constructor(ctx: ExprContext) {
		super(ctx.parent, ctx.invokingState);
		this.copyFrom(ctx);
	}
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterBinaryExprMultiplicative) {
			listener.enterBinaryExprMultiplicative(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitBinaryExprMultiplicative) {
			listener.exitBinaryExprMultiplicative(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitBinaryExprMultiplicative) {
			return visitor.visitBinaryExprMultiplicative(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}
export class BinaryExprAdditiveContext extends ExprContext {
	public expr(): ExprContext[];
	public expr(i: number): ExprContext;
	public expr(i?: number): ExprContext | ExprContext[] {
		if (i === undefined) {
			return this.getRuleContexts(ExprContext);
		} else {
			return this.getRuleContext(i, ExprContext);
		}
	}
	public binaryOperatorAdditive(): BinaryOperatorAdditiveContext {
		return this.getRuleContext(0, BinaryOperatorAdditiveContext);
	}
	constructor(ctx: ExprContext) {
		super(ctx.parent, ctx.invokingState);
		this.copyFrom(ctx);
	}
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterBinaryExprAdditive) {
			listener.enterBinaryExprAdditive(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitBinaryExprAdditive) {
			listener.exitBinaryExprAdditive(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitBinaryExprAdditive) {
			return visitor.visitBinaryExprAdditive(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}
export class BinaryExprBitwiseShiftContext extends ExprContext {
	public expr(): ExprContext[];
	public expr(i: number): ExprContext;
	public expr(i?: number): ExprContext | ExprContext[] {
		if (i === undefined) {
			return this.getRuleContexts(ExprContext);
		} else {
			return this.getRuleContext(i, ExprContext);
		}
	}
	public binaryOperatorBitwiseShift(): BinaryOperatorBitwiseShiftContext {
		return this.getRuleContext(0, BinaryOperatorBitwiseShiftContext);
	}
	constructor(ctx: ExprContext) {
		super(ctx.parent, ctx.invokingState);
		this.copyFrom(ctx);
	}
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterBinaryExprBitwiseShift) {
			listener.enterBinaryExprBitwiseShift(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitBinaryExprBitwiseShift) {
			listener.exitBinaryExprBitwiseShift(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitBinaryExprBitwiseShift) {
			return visitor.visitBinaryExprBitwiseShift(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}
export class BinaryExprRelationalContext extends ExprContext {
	public expr(): ExprContext[];
	public expr(i: number): ExprContext;
	public expr(i?: number): ExprContext | ExprContext[] {
		if (i === undefined) {
			return this.getRuleContexts(ExprContext);
		} else {
			return this.getRuleContext(i, ExprContext);
		}
	}
	public binaryOperatorRelational(): BinaryOperatorRelationalContext {
		return this.getRuleContext(0, BinaryOperatorRelationalContext);
	}
	constructor(ctx: ExprContext) {
		super(ctx.parent, ctx.invokingState);
		this.copyFrom(ctx);
	}
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterBinaryExprRelational) {
			listener.enterBinaryExprRelational(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitBinaryExprRelational) {
			listener.exitBinaryExprRelational(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitBinaryExprRelational) {
			return visitor.visitBinaryExprRelational(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}
export class BinaryExprBitwiseAndContext extends ExprContext {
	public expr(): ExprContext[];
	public expr(i: number): ExprContext;
	public expr(i?: number): ExprContext | ExprContext[] {
		if (i === undefined) {
			return this.getRuleContexts(ExprContext);
		} else {
			return this.getRuleContext(i, ExprContext);
		}
	}
	public binaryOperatorBitwiseAnd(): BinaryOperatorBitwiseAndContext {
		return this.getRuleContext(0, BinaryOperatorBitwiseAndContext);
	}
	constructor(ctx: ExprContext) {
		super(ctx.parent, ctx.invokingState);
		this.copyFrom(ctx);
	}
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterBinaryExprBitwiseAnd) {
			listener.enterBinaryExprBitwiseAnd(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitBinaryExprBitwiseAnd) {
			listener.exitBinaryExprBitwiseAnd(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitBinaryExprBitwiseAnd) {
			return visitor.visitBinaryExprBitwiseAnd(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}
export class BinaryExprBitwiseXorContext extends ExprContext {
	public expr(): ExprContext[];
	public expr(i: number): ExprContext;
	public expr(i?: number): ExprContext | ExprContext[] {
		if (i === undefined) {
			return this.getRuleContexts(ExprContext);
		} else {
			return this.getRuleContext(i, ExprContext);
		}
	}
	public binaryOperatorBitwiseXor(): BinaryOperatorBitwiseXorContext {
		return this.getRuleContext(0, BinaryOperatorBitwiseXorContext);
	}
	constructor(ctx: ExprContext) {
		super(ctx.parent, ctx.invokingState);
		this.copyFrom(ctx);
	}
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterBinaryExprBitwiseXor) {
			listener.enterBinaryExprBitwiseXor(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitBinaryExprBitwiseXor) {
			listener.exitBinaryExprBitwiseXor(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitBinaryExprBitwiseXor) {
			return visitor.visitBinaryExprBitwiseXor(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}
export class BinaryExprBitwiseOrContext extends ExprContext {
	public expr(): ExprContext[];
	public expr(i: number): ExprContext;
	public expr(i?: number): ExprContext | ExprContext[] {
		if (i === undefined) {
			return this.getRuleContexts(ExprContext);
		} else {
			return this.getRuleContext(i, ExprContext);
		}
	}
	public binaryOperatorBitwiseOr(): BinaryOperatorBitwiseOrContext {
		return this.getRuleContext(0, BinaryOperatorBitwiseOrContext);
	}
	constructor(ctx: ExprContext) {
		super(ctx.parent, ctx.invokingState);
		this.copyFrom(ctx);
	}
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterBinaryExprBitwiseOr) {
			listener.enterBinaryExprBitwiseOr(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitBinaryExprBitwiseOr) {
			listener.exitBinaryExprBitwiseOr(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitBinaryExprBitwiseOr) {
			return visitor.visitBinaryExprBitwiseOr(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}
export class BinaryExprLogicalAndContext extends ExprContext {
	public expr(): ExprContext[];
	public expr(i: number): ExprContext;
	public expr(i?: number): ExprContext | ExprContext[] {
		if (i === undefined) {
			return this.getRuleContexts(ExprContext);
		} else {
			return this.getRuleContext(i, ExprContext);
		}
	}
	public binaryOperatorLogicalAnd(): BinaryOperatorLogicalAndContext {
		return this.getRuleContext(0, BinaryOperatorLogicalAndContext);
	}
	constructor(ctx: ExprContext) {
		super(ctx.parent, ctx.invokingState);
		this.copyFrom(ctx);
	}
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterBinaryExprLogicalAnd) {
			listener.enterBinaryExprLogicalAnd(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitBinaryExprLogicalAnd) {
			listener.exitBinaryExprLogicalAnd(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitBinaryExprLogicalAnd) {
			return visitor.visitBinaryExprLogicalAnd(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}
export class BinaryExprLogicalOrContext extends ExprContext {
	public expr(): ExprContext[];
	public expr(i: number): ExprContext;
	public expr(i?: number): ExprContext | ExprContext[] {
		if (i === undefined) {
			return this.getRuleContexts(ExprContext);
		} else {
			return this.getRuleContext(i, ExprContext);
		}
	}
	public binaryOperatorLogicalOr(): BinaryOperatorLogicalOrContext {
		return this.getRuleContext(0, BinaryOperatorLogicalOrContext);
	}
	constructor(ctx: ExprContext) {
		super(ctx.parent, ctx.invokingState);
		this.copyFrom(ctx);
	}
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterBinaryExprLogicalOr) {
			listener.enterBinaryExprLogicalOr(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitBinaryExprLogicalOr) {
			listener.exitBinaryExprLogicalOr(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitBinaryExprLogicalOr) {
			return visitor.visitBinaryExprLogicalOr(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}
export class BinaryExprEqualityContext extends ExprContext {
	public expr(): ExprContext[];
	public expr(i: number): ExprContext;
	public expr(i?: number): ExprContext | ExprContext[] {
		if (i === undefined) {
			return this.getRuleContexts(ExprContext);
		} else {
			return this.getRuleContext(i, ExprContext);
		}
	}
	public binaryOperatorEquality(): BinaryOperatorEqualityContext {
		return this.getRuleContext(0, BinaryOperatorEqualityContext);
	}
	constructor(ctx: ExprContext) {
		super(ctx.parent, ctx.invokingState);
		this.copyFrom(ctx);
	}
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterBinaryExprEquality) {
			listener.enterBinaryExprEquality(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitBinaryExprEquality) {
			listener.exitBinaryExprEquality(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitBinaryExprEquality) {
			return visitor.visitBinaryExprEquality(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}
export class TerminalExprContext extends ExprContext {
	public terminal(): TerminalContext {
		return this.getRuleContext(0, TerminalContext);
	}
	constructor(ctx: ExprContext) {
		super(ctx.parent, ctx.invokingState);
		this.copyFrom(ctx);
	}
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterTerminalExpr) {
			listener.enterTerminalExpr(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitTerminalExpr) {
			listener.exitTerminalExpr(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitTerminalExpr) {
			return visitor.visitTerminalExpr(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}
export class ParenthesysExprContext extends ExprContext {
	public LPAREN(): TerminalNode { return this.getToken(exprParser.LPAREN, 0); }
	public expr(): ExprContext {
		return this.getRuleContext(0, ExprContext);
	}
	public RPAREN(): TerminalNode { return this.getToken(exprParser.RPAREN, 0); }
	constructor(ctx: ExprContext) {
		super(ctx.parent, ctx.invokingState);
		this.copyFrom(ctx);
	}
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterParenthesysExpr) {
			listener.enterParenthesysExpr(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitParenthesysExpr) {
			listener.exitParenthesysExpr(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitParenthesysExpr) {
			return visitor.visitParenthesysExpr(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class ArgumentsContext extends ParserRuleContext {
	public exprList(): ExprListContext {
		return this.getRuleContext(0, ExprListContext);
	}
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_arguments; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterArguments) {
			listener.enterArguments(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitArguments) {
			listener.exitArguments(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitArguments) {
			return visitor.visitArguments(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class ExprListContext extends ParserRuleContext {
	public expr(): ExprContext[];
	public expr(i: number): ExprContext;
	public expr(i?: number): ExprContext | ExprContext[] {
		if (i === undefined) {
			return this.getRuleContexts(ExprContext);
		} else {
			return this.getRuleContext(i, ExprContext);
		}
	}
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_exprList; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterExprList) {
			listener.enterExprList(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitExprList) {
			listener.exitExprList(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitExprList) {
			return visitor.visitExprList(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class UnaryOperatorContext extends ParserRuleContext {
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_unaryOperator; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterUnaryOperator) {
			listener.enterUnaryOperator(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitUnaryOperator) {
			listener.exitUnaryOperator(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitUnaryOperator) {
			return visitor.visitUnaryOperator(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class BinaryOperatorMultiplicativeContext extends ParserRuleContext {
	public TIMES(): TerminalNode | undefined { return this.tryGetToken(exprParser.TIMES, 0); }
	public DIV(): TerminalNode | undefined { return this.tryGetToken(exprParser.DIV, 0); }
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_binaryOperatorMultiplicative; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterBinaryOperatorMultiplicative) {
			listener.enterBinaryOperatorMultiplicative(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitBinaryOperatorMultiplicative) {
			listener.exitBinaryOperatorMultiplicative(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitBinaryOperatorMultiplicative) {
			return visitor.visitBinaryOperatorMultiplicative(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class BinaryOperatorAdditiveContext extends ParserRuleContext {
	public PLUS(): TerminalNode | undefined { return this.tryGetToken(exprParser.PLUS, 0); }
	public MINUS(): TerminalNode | undefined { return this.tryGetToken(exprParser.MINUS, 0); }
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_binaryOperatorAdditive; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterBinaryOperatorAdditive) {
			listener.enterBinaryOperatorAdditive(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitBinaryOperatorAdditive) {
			listener.exitBinaryOperatorAdditive(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitBinaryOperatorAdditive) {
			return visitor.visitBinaryOperatorAdditive(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class BinaryOperatorBitwiseShiftContext extends ParserRuleContext {
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_binaryOperatorBitwiseShift; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterBinaryOperatorBitwiseShift) {
			listener.enterBinaryOperatorBitwiseShift(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitBinaryOperatorBitwiseShift) {
			listener.exitBinaryOperatorBitwiseShift(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitBinaryOperatorBitwiseShift) {
			return visitor.visitBinaryOperatorBitwiseShift(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class BinaryOperatorRelationalContext extends ParserRuleContext {
	public GT(): TerminalNode | undefined { return this.tryGetToken(exprParser.GT, 0); }
	public LT(): TerminalNode | undefined { return this.tryGetToken(exprParser.LT, 0); }
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_binaryOperatorRelational; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterBinaryOperatorRelational) {
			listener.enterBinaryOperatorRelational(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitBinaryOperatorRelational) {
			listener.exitBinaryOperatorRelational(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitBinaryOperatorRelational) {
			return visitor.visitBinaryOperatorRelational(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class BinaryOperatorEqualityContext extends ParserRuleContext {
	public EQ(): TerminalNode { return this.getToken(exprParser.EQ, 0); }
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_binaryOperatorEquality; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterBinaryOperatorEquality) {
			listener.enterBinaryOperatorEquality(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitBinaryOperatorEquality) {
			listener.exitBinaryOperatorEquality(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitBinaryOperatorEquality) {
			return visitor.visitBinaryOperatorEquality(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class BinaryOperatorBitwiseAndContext extends ParserRuleContext {
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_binaryOperatorBitwiseAnd; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterBinaryOperatorBitwiseAnd) {
			listener.enterBinaryOperatorBitwiseAnd(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitBinaryOperatorBitwiseAnd) {
			listener.exitBinaryOperatorBitwiseAnd(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitBinaryOperatorBitwiseAnd) {
			return visitor.visitBinaryOperatorBitwiseAnd(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class BinaryOperatorBitwiseXorContext extends ParserRuleContext {
	public POW(): TerminalNode { return this.getToken(exprParser.POW, 0); }
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_binaryOperatorBitwiseXor; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterBinaryOperatorBitwiseXor) {
			listener.enterBinaryOperatorBitwiseXor(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitBinaryOperatorBitwiseXor) {
			listener.exitBinaryOperatorBitwiseXor(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitBinaryOperatorBitwiseXor) {
			return visitor.visitBinaryOperatorBitwiseXor(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class BinaryOperatorBitwiseOrContext extends ParserRuleContext {
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_binaryOperatorBitwiseOr; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterBinaryOperatorBitwiseOr) {
			listener.enterBinaryOperatorBitwiseOr(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitBinaryOperatorBitwiseOr) {
			listener.exitBinaryOperatorBitwiseOr(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitBinaryOperatorBitwiseOr) {
			return visitor.visitBinaryOperatorBitwiseOr(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class BinaryOperatorLogicalAndContext extends ParserRuleContext {
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_binaryOperatorLogicalAnd; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterBinaryOperatorLogicalAnd) {
			listener.enterBinaryOperatorLogicalAnd(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitBinaryOperatorLogicalAnd) {
			listener.exitBinaryOperatorLogicalAnd(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitBinaryOperatorLogicalAnd) {
			return visitor.visitBinaryOperatorLogicalAnd(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class BinaryOperatorLogicalOrContext extends ParserRuleContext {
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_binaryOperatorLogicalOr; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterBinaryOperatorLogicalOr) {
			listener.enterBinaryOperatorLogicalOr(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitBinaryOperatorLogicalOr) {
			listener.exitBinaryOperatorLogicalOr(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitBinaryOperatorLogicalOr) {
			return visitor.visitBinaryOperatorLogicalOr(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class BinaryOperatorContext extends ParserRuleContext {
	public binaryOperatorMultiplicative(): BinaryOperatorMultiplicativeContext | undefined {
		return this.tryGetRuleContext(0, BinaryOperatorMultiplicativeContext);
	}
	public binaryOperatorAdditive(): BinaryOperatorAdditiveContext | undefined {
		return this.tryGetRuleContext(0, BinaryOperatorAdditiveContext);
	}
	public binaryOperatorBitwiseShift(): BinaryOperatorBitwiseShiftContext | undefined {
		return this.tryGetRuleContext(0, BinaryOperatorBitwiseShiftContext);
	}
	public binaryOperatorRelational(): BinaryOperatorRelationalContext | undefined {
		return this.tryGetRuleContext(0, BinaryOperatorRelationalContext);
	}
	public binaryOperatorEquality(): BinaryOperatorEqualityContext | undefined {
		return this.tryGetRuleContext(0, BinaryOperatorEqualityContext);
	}
	public binaryOperatorBitwiseAnd(): BinaryOperatorBitwiseAndContext | undefined {
		return this.tryGetRuleContext(0, BinaryOperatorBitwiseAndContext);
	}
	public binaryOperatorBitwiseXor(): BinaryOperatorBitwiseXorContext | undefined {
		return this.tryGetRuleContext(0, BinaryOperatorBitwiseXorContext);
	}
	public binaryOperatorBitwiseOr(): BinaryOperatorBitwiseOrContext | undefined {
		return this.tryGetRuleContext(0, BinaryOperatorBitwiseOrContext);
	}
	public binaryOperatorLogicalAnd(): BinaryOperatorLogicalAndContext | undefined {
		return this.tryGetRuleContext(0, BinaryOperatorLogicalAndContext);
	}
	public binaryOperatorLogicalOr(): BinaryOperatorLogicalOrContext | undefined {
		return this.tryGetRuleContext(0, BinaryOperatorLogicalOrContext);
	}
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return exprParser.RULE_binaryOperator; }
	// @Override
	public enterRule(listener: exprListener): void {
		if (listener.enterBinaryOperator) {
			listener.enterBinaryOperator(this);
		}
	}
	// @Override
	public exitRule(listener: exprListener): void {
		if (listener.exitBinaryOperator) {
			listener.exitBinaryOperator(this);
		}
	}
	// @Override
	public accept<Result>(visitor: exprVisitor<Result>): Result {
		if (visitor.visitBinaryOperator) {
			return visitor.visitBinaryOperator(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}



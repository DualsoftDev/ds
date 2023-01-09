// Generated from ../../../Grammar/g4s/fqdn.g4 by ANTLR 4.9.0-SNAPSHOT


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

import { fqdnListener } from "./fqdnListener";
import { fqdnVisitor } from "./fqdnVisitor";


export class fqdnParser extends Parser {
	public static readonly T__0 = 1;
	public static readonly T__1 = 2;
	public static readonly WS = 3;
	public static readonly Identifier = 4;
	public static readonly QuotedStringLiteral = 5;
	public static readonly RULE_fqdns = 0;
	public static readonly RULE_fqdn = 1;
	public static readonly RULE_id = 2;
	public static readonly RULE_qid = 3;
	public static readonly RULE_nameComponent = 4;
	// tslint:disable:no-trailing-whitespace
	public static readonly ruleNames: string[] = [
		"fqdns", "fqdn", "id", "qid", "nameComponent",
	];

	private static readonly _LITERAL_NAMES: Array<string | undefined> = [
		undefined, "';'", "'.'",
	];
	private static readonly _SYMBOLIC_NAMES: Array<string | undefined> = [
		undefined, undefined, undefined, "WS", "Identifier", "QuotedStringLiteral",
	];
	public static readonly VOCABULARY: Vocabulary = new VocabularyImpl(fqdnParser._LITERAL_NAMES, fqdnParser._SYMBOLIC_NAMES, []);

	// @Override
	// @NotNull
	public get vocabulary(): Vocabulary {
		return fqdnParser.VOCABULARY;
	}
	// tslint:enable:no-trailing-whitespace

	// @Override
	public get grammarFileName(): string { return "fqdn.g4"; }

	// @Override
	public get ruleNames(): string[] { return fqdnParser.ruleNames; }

	// @Override
	public get serializedATN(): string { return fqdnParser._serializedATN; }

	protected createFailedPredicateException(predicate?: string, message?: string): FailedPredicateException {
		return new FailedPredicateException(this, predicate, message);
	}

	constructor(input: TokenStream) {
		super(input);
		this._interp = new ParserATNSimulator(fqdnParser._ATN, this);
	}
	// @RuleVersion(0)
	public fqdns(): FqdnsContext {
		let _localctx: FqdnsContext = new FqdnsContext(this._ctx, this.state);
		this.enterRule(_localctx, 0, fqdnParser.RULE_fqdns);
		let _la: number;
		try {
			let _alt: number;
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 10;
			this.fqdn();
			this.state = 15;
			this._errHandler.sync(this);
			_alt = this.interpreter.adaptivePredict(this._input, 0, this._ctx);
			while (_alt !== 2 && _alt !== ATN.INVALID_ALT_NUMBER) {
				if (_alt === 1) {
					{
					{
					this.state = 11;
					this.match(fqdnParser.T__0);
					this.state = 12;
					this.fqdn();
					}
					}
				}
				this.state = 17;
				this._errHandler.sync(this);
				_alt = this.interpreter.adaptivePredict(this._input, 0, this._ctx);
			}
			this.state = 19;
			this._errHandler.sync(this);
			_la = this._input.LA(1);
			if (_la === fqdnParser.T__0) {
				{
				this.state = 18;
				this.match(fqdnParser.T__0);
				}
			}

			this.state = 21;
			this.match(fqdnParser.EOF);
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
	public fqdn(): FqdnContext {
		let _localctx: FqdnContext = new FqdnContext(this._ctx, this.state);
		this.enterRule(_localctx, 2, fqdnParser.RULE_fqdn);
		let _la: number;
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 23;
			this.nameComponent();
			this.state = 28;
			this._errHandler.sync(this);
			_la = this._input.LA(1);
			while ((((_la) & ~0x1F) === 0 && ((1 << _la) & ((1 << fqdnParser.T__1) | (1 << fqdnParser.Identifier) | (1 << fqdnParser.QuotedStringLiteral))) !== 0)) {
				{
				this.state = 26;
				this._errHandler.sync(this);
				switch (this._input.LA(1)) {
				case fqdnParser.T__1:
					{
					this.state = 24;
					this.match(fqdnParser.T__1);
					}
					break;
				case fqdnParser.Identifier:
				case fqdnParser.QuotedStringLiteral:
					{
					this.state = 25;
					this.nameComponent();
					}
					break;
				default:
					throw new NoViableAltException(this);
				}
				}
				this.state = 30;
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
	public id(): IdContext {
		let _localctx: IdContext = new IdContext(this._ctx, this.state);
		this.enterRule(_localctx, 4, fqdnParser.RULE_id);
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 31;
			this.match(fqdnParser.Identifier);
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
	public qid(): QidContext {
		let _localctx: QidContext = new QidContext(this._ctx, this.state);
		this.enterRule(_localctx, 6, fqdnParser.RULE_qid);
		try {
			this.enterOuterAlt(_localctx, 1);
			{
			this.state = 33;
			this.match(fqdnParser.QuotedStringLiteral);
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
	public nameComponent(): NameComponentContext {
		let _localctx: NameComponentContext = new NameComponentContext(this._ctx, this.state);
		this.enterRule(_localctx, 8, fqdnParser.RULE_nameComponent);
		try {
			this.state = 37;
			this._errHandler.sync(this);
			switch (this._input.LA(1)) {
			case fqdnParser.Identifier:
				this.enterOuterAlt(_localctx, 1);
				{
				this.state = 35;
				this.id();
				}
				break;
			case fqdnParser.QuotedStringLiteral:
				this.enterOuterAlt(_localctx, 2);
				{
				this.state = 36;
				this.qid();
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

	public static readonly _serializedATN: string =
		"\x03\uC91D\uCABA\u058D\uAFBA\u4F53\u0607\uEA8B\uC241\x03\x07*\x04\x02" +
		"\t\x02\x04\x03\t\x03\x04\x04\t\x04\x04\x05\t\x05\x04\x06\t\x06\x03\x02" +
		"\x03\x02\x03\x02\x07\x02\x10\n\x02\f\x02\x0E\x02\x13\v\x02\x03\x02\x05" +
		"\x02\x16\n\x02\x03\x02\x03\x02\x03\x03\x03\x03\x03\x03\x07\x03\x1D\n\x03" +
		"\f\x03\x0E\x03 \v\x03\x03\x04\x03\x04\x03\x05\x03\x05\x03\x06\x03\x06" +
		"\x05\x06(\n\x06\x03\x06\x02\x02\x02\x07\x02\x02\x04\x02\x06\x02\b\x02" +
		"\n\x02\x02\x02\x02)\x02\f\x03\x02\x02\x02\x04\x19\x03\x02\x02\x02\x06" +
		"!\x03\x02\x02\x02\b#\x03\x02\x02\x02\n\'\x03\x02\x02\x02\f\x11\x05\x04" +
		"\x03\x02\r\x0E\x07\x03\x02\x02\x0E\x10\x05\x04\x03\x02\x0F\r\x03\x02\x02" +
		"\x02\x10\x13\x03\x02\x02\x02\x11\x0F\x03\x02\x02\x02\x11\x12\x03\x02\x02" +
		"\x02\x12\x15\x03\x02\x02\x02\x13\x11\x03\x02\x02\x02\x14\x16\x07\x03\x02" +
		"\x02\x15\x14\x03\x02\x02\x02\x15\x16\x03\x02\x02\x02\x16\x17\x03\x02\x02" +
		"\x02\x17\x18\x07\x02\x02\x03\x18\x03\x03\x02\x02\x02\x19\x1E\x05\n\x06" +
		"\x02\x1A\x1D\x07\x04\x02\x02\x1B\x1D\x05\n\x06\x02\x1C\x1A\x03\x02\x02" +
		"\x02\x1C\x1B\x03\x02\x02\x02\x1D \x03\x02\x02\x02\x1E\x1C\x03\x02\x02" +
		"\x02\x1E\x1F\x03\x02\x02\x02\x1F\x05\x03\x02\x02\x02 \x1E\x03\x02\x02" +
		"\x02!\"\x07\x06\x02\x02\"\x07\x03\x02\x02\x02#$\x07\x07\x02\x02$\t\x03" +
		"\x02\x02\x02%(\x05\x06\x04\x02&(\x05\b\x05\x02\'%\x03\x02\x02\x02\'&\x03" +
		"\x02\x02\x02(\v\x03\x02\x02\x02\x07\x11\x15\x1C\x1E\'";
	public static __ATN: ATN;
	public static get _ATN(): ATN {
		if (!fqdnParser.__ATN) {
			fqdnParser.__ATN = new ATNDeserializer().deserialize(Utils.toCharArray(fqdnParser._serializedATN));
		}

		return fqdnParser.__ATN;
	}

}

export class FqdnsContext extends ParserRuleContext {
	public fqdn(): FqdnContext[];
	public fqdn(i: number): FqdnContext;
	public fqdn(i?: number): FqdnContext | FqdnContext[] {
		if (i === undefined) {
			return this.getRuleContexts(FqdnContext);
		} else {
			return this.getRuleContext(i, FqdnContext);
		}
	}
	public EOF(): TerminalNode { return this.getToken(fqdnParser.EOF, 0); }
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return fqdnParser.RULE_fqdns; }
	// @Override
	public enterRule(listener: fqdnListener): void {
		if (listener.enterFqdns) {
			listener.enterFqdns(this);
		}
	}
	// @Override
	public exitRule(listener: fqdnListener): void {
		if (listener.exitFqdns) {
			listener.exitFqdns(this);
		}
	}
	// @Override
	public accept<Result>(visitor: fqdnVisitor<Result>): Result {
		if (visitor.visitFqdns) {
			return visitor.visitFqdns(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class FqdnContext extends ParserRuleContext {
	public nameComponent(): NameComponentContext[];
	public nameComponent(i: number): NameComponentContext;
	public nameComponent(i?: number): NameComponentContext | NameComponentContext[] {
		if (i === undefined) {
			return this.getRuleContexts(NameComponentContext);
		} else {
			return this.getRuleContext(i, NameComponentContext);
		}
	}
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return fqdnParser.RULE_fqdn; }
	// @Override
	public enterRule(listener: fqdnListener): void {
		if (listener.enterFqdn) {
			listener.enterFqdn(this);
		}
	}
	// @Override
	public exitRule(listener: fqdnListener): void {
		if (listener.exitFqdn) {
			listener.exitFqdn(this);
		}
	}
	// @Override
	public accept<Result>(visitor: fqdnVisitor<Result>): Result {
		if (visitor.visitFqdn) {
			return visitor.visitFqdn(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class IdContext extends ParserRuleContext {
	public Identifier(): TerminalNode { return this.getToken(fqdnParser.Identifier, 0); }
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return fqdnParser.RULE_id; }
	// @Override
	public enterRule(listener: fqdnListener): void {
		if (listener.enterId) {
			listener.enterId(this);
		}
	}
	// @Override
	public exitRule(listener: fqdnListener): void {
		if (listener.exitId) {
			listener.exitId(this);
		}
	}
	// @Override
	public accept<Result>(visitor: fqdnVisitor<Result>): Result {
		if (visitor.visitId) {
			return visitor.visitId(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class QidContext extends ParserRuleContext {
	public QuotedStringLiteral(): TerminalNode { return this.getToken(fqdnParser.QuotedStringLiteral, 0); }
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return fqdnParser.RULE_qid; }
	// @Override
	public enterRule(listener: fqdnListener): void {
		if (listener.enterQid) {
			listener.enterQid(this);
		}
	}
	// @Override
	public exitRule(listener: fqdnListener): void {
		if (listener.exitQid) {
			listener.exitQid(this);
		}
	}
	// @Override
	public accept<Result>(visitor: fqdnVisitor<Result>): Result {
		if (visitor.visitQid) {
			return visitor.visitQid(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}


export class NameComponentContext extends ParserRuleContext {
	public id(): IdContext | undefined {
		return this.tryGetRuleContext(0, IdContext);
	}
	public qid(): QidContext | undefined {
		return this.tryGetRuleContext(0, QidContext);
	}
	constructor(parent: ParserRuleContext | undefined, invokingState: number) {
		super(parent, invokingState);
	}
	// @Override
	public get ruleIndex(): number { return fqdnParser.RULE_nameComponent; }
	// @Override
	public enterRule(listener: fqdnListener): void {
		if (listener.enterNameComponent) {
			listener.enterNameComponent(this);
		}
	}
	// @Override
	public exitRule(listener: fqdnListener): void {
		if (listener.exitNameComponent) {
			listener.exitNameComponent(this);
		}
	}
	// @Override
	public accept<Result>(visitor: fqdnVisitor<Result>): Result {
		if (visitor.visitNameComponent) {
			return visitor.visitNameComponent(this);
		} else {
			return visitor.visitChildren(this);
		}
	}
}



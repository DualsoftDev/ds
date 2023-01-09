// Generated from ../../../Grammar/g4s/fqdn.g4 by ANTLR 4.9.0-SNAPSHOT


import { ATN } from "antlr4ts/atn/ATN";
import { ATNDeserializer } from "antlr4ts/atn/ATNDeserializer";
import { CharStream } from "antlr4ts/CharStream";
import { Lexer } from "antlr4ts/Lexer";
import { LexerATNSimulator } from "antlr4ts/atn/LexerATNSimulator";
import { NotNull } from "antlr4ts/Decorators";
import { Override } from "antlr4ts/Decorators";
import { RuleContext } from "antlr4ts/RuleContext";
import { Vocabulary } from "antlr4ts/Vocabulary";
import { VocabularyImpl } from "antlr4ts/VocabularyImpl";

import * as Utils from "antlr4ts/misc/Utils";


export class fqdnLexer extends Lexer {
	public static readonly T__0 = 1;
	public static readonly T__1 = 2;
	public static readonly WS = 3;
	public static readonly Identifier = 4;
	public static readonly QuotedStringLiteral = 5;

	// tslint:disable:no-trailing-whitespace
	public static readonly channelNames: string[] = [
		"DEFAULT_TOKEN_CHANNEL", "HIDDEN",
	];

	// tslint:disable:no-trailing-whitespace
	public static readonly modeNames: string[] = [
		"DEFAULT_MODE",
	];

	public static readonly ruleNames: string[] = [
		"T__0", "T__1", "WS", "Identifier", "HangulChar", "ValidIdStart", "ValidIdChar", 
		"QuotedStringLiteral",
	];

	private static readonly _LITERAL_NAMES: Array<string | undefined> = [
		undefined, "';'", "'.'",
	];
	private static readonly _SYMBOLIC_NAMES: Array<string | undefined> = [
		undefined, undefined, undefined, "WS", "Identifier", "QuotedStringLiteral",
	];
	public static readonly VOCABULARY: Vocabulary = new VocabularyImpl(fqdnLexer._LITERAL_NAMES, fqdnLexer._SYMBOLIC_NAMES, []);

	// @Override
	// @NotNull
	public get vocabulary(): Vocabulary {
		return fqdnLexer.VOCABULARY;
	}
	// tslint:enable:no-trailing-whitespace


	constructor(input: CharStream) {
		super(input);
		this._interp = new LexerATNSimulator(fqdnLexer._ATN, this);
	}

	// @Override
	public get grammarFileName(): string { return "fqdn.g4"; }

	// @Override
	public get ruleNames(): string[] { return fqdnLexer.ruleNames; }

	// @Override
	public get serializedATN(): string { return fqdnLexer._serializedATN; }

	// @Override
	public get channelNames(): string[] { return fqdnLexer.channelNames; }

	// @Override
	public get modeNames(): string[] { return fqdnLexer.modeNames; }

	public static readonly _serializedATN: string =
		"\x03\uC91D\uCABA\u058D\uAFBA\u4F53\u0607\uEA8B\uC241\x02\x07>\b\x01\x04" +
		"\x02\t\x02\x04\x03\t\x03\x04\x04\t\x04\x04\x05\t\x05\x04\x06\t\x06\x04" +
		"\x07\t\x07\x04\b\t\b\x04\t\t\t\x03\x02\x03\x02\x03\x03\x03\x03\x03\x04" +
		"\x06\x04\x19\n\x04\r\x04\x0E\x04\x1A\x03\x04\x03\x04\x03\x05\x03\x05\x07" +
		"\x05!\n\x05\f\x05\x0E\x05$\v\x05\x03\x06\x06\x06\'\n\x06\r\x06\x0E\x06" +
		"(\x03\x07\x03\x07\x05\x07-\n\x07\x03\b\x03\b\x03\b\x05\b2\n\b\x03\t\x03" +
		"\t\x03\t\x03\t\x07\t8\n\t\f\t\x0E\t;\v\t\x03\t\x03\t\x02\x02\x02\n\x03" +
		"\x02\x03\x05\x02\x04\x07\x02\x05\t\x02\x06\v\x02\x02\r\x02\x02\x0F\x02" +
		"\x02\x11\x02\x07\x03\x02\x07\x05\x02\v\f\x0F\x0F\"\"\x03\x02\uAC02\uD7A5" +
		"\x05\x02C\\aac|\x06\x02\f\f\x0F\x0F$$^^\x04\x02$$^^\x02B\x02\x03\x03\x02" +
		"\x02\x02\x02\x05\x03\x02\x02\x02\x02\x07\x03\x02\x02\x02\x02\t\x03\x02" +
		"\x02\x02\x02\x11\x03\x02\x02\x02\x03\x13\x03\x02\x02\x02\x05\x15\x03\x02" +
		"\x02\x02\x07\x18\x03\x02\x02\x02\t\x1E\x03\x02\x02\x02\v&\x03\x02\x02" +
		"\x02\r,\x03\x02\x02\x02\x0F1\x03\x02\x02\x02\x113\x03\x02\x02\x02\x13" +
		"\x14\x07=\x02\x02\x14\x04\x03\x02\x02\x02\x15\x16\x070\x02\x02\x16\x06" +
		"\x03\x02\x02\x02\x17\x19\t\x02\x02\x02\x18\x17\x03\x02\x02\x02\x19\x1A" +
		"\x03\x02\x02\x02\x1A\x18\x03\x02\x02\x02\x1A\x1B\x03\x02\x02\x02\x1B\x1C" +
		"\x03\x02\x02\x02\x1C\x1D\b\x04\x02\x02\x1D\b\x03\x02\x02\x02\x1E\"\x05" +
		"\r\x07\x02\x1F!\x05\x0F\b\x02 \x1F\x03\x02\x02\x02!$\x03\x02\x02\x02\"" +
		" \x03\x02\x02\x02\"#\x03\x02\x02\x02#\n\x03\x02\x02\x02$\"\x03\x02\x02" +
		"\x02%\'\t\x03\x02\x02&%\x03\x02\x02\x02\'(\x03\x02\x02\x02(&\x03\x02\x02" +
		"\x02()\x03\x02\x02\x02)\f\x03\x02\x02\x02*-\t\x04\x02\x02+-\x05\v\x06" +
		"\x02,*\x03\x02\x02\x02,+\x03\x02\x02\x02-\x0E\x03\x02\x02\x02.2\x05\r" +
		"\x07\x02/2\x042;\x0202\x05\v\x06\x021.\x03\x02\x02\x021/\x03\x02\x02\x02" +
		"10\x03\x02\x02\x022\x10\x03\x02\x02\x0239\x07$\x02\x0248\n\x05\x02\x02" +
		"56\x07^\x02\x0268\t\x06\x02\x0274\x03\x02\x02\x0275\x03\x02\x02\x028;" +
		"\x03\x02\x02\x0297\x03\x02\x02\x029:\x03\x02\x02\x02:<\x03\x02\x02\x02" +
		";9\x03\x02\x02\x02<=\x07$\x02\x02=\x12\x03\x02\x02\x02\n\x02\x1A\"(,1" +
		"79\x03\b\x02\x02";
	public static __ATN: ATN;
	public static get _ATN(): ATN {
		if (!fqdnLexer.__ATN) {
			fqdnLexer.__ATN = new ATNDeserializer().deserialize(Utils.toCharArray(fqdnLexer._serializedATN));
		}

		return fqdnLexer.__ATN;
	}

}


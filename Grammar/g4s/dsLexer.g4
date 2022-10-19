lexer grammar dsLexer;


SYS: 'sys';
IP: 'ip';
HOST: 'host';
COPY_SYSTEM: 'copy_system';
LAYOUTS: 'layouts';
ADDRESSES: 'addresses';
PROP: 'prop';
SAFETY: 'safety';
FLOW: 'flow';
INTERFACES: 'interfaces';
ALIASES: 'aliases';
EMG_IN: 'emg_in';
EMG: 'emg';
AUTO_IN: 'auto_in';
AUTO: 'auto';
START_IN: 'start_in';
START: 'start';
RESET_IN: 'reset_in';
RESET: 'reset';

WS: [ \t\r\n]+ -> skip;
BLOCK_COMMENT : '/*' (BLOCK_COMMENT|.)*? '*/' -> channel(HIDDEN) ;
LINE_COMMENT  : '//' .*? ('\n'|EOF) -> channel(HIDDEN) ;


fragment Identifier: ValidIdStart ValidIdChar*;
    fragment HangulChar: [\uAC00-\uD7A3]+;

    fragment ValidIdStart
    : ('a' .. 'z') | ('A' .. 'Z') | '_' | HangulChar
    ;

    fragment ValidIdChar
    : ValidIdStart | ('0' .. '9') | HangulChar
    ;


fragment QuotedStringLiteral : '"' (~('"' | '\\' | '\r' | '\n') | '\\' ('"' | '\\'))* '"';

fragment Compo: Identifier|QuotedStringLiteral;

IDENTIFIER1: Compo;
IDENTIFIER2: Compo '.' Compo;
IDENTIFIER3: Compo '.' Compo '.' Compo;
IDENTIFIER4: Compo '.' Compo '.' Compo '.' Compo;


IPV4: [1-9][0-9]*'.'('0'|[1-9][0-9]*)'.'('0'|[1-9][0-9]*)'.'('0'|[1-9][0-9]*);
// IPV4: (INTEGER)(DOT) INTEGER DOT INTEGER DOT INTEGER;

TAG_ADDRESS: ValidTagStart ValidTagChar*;
fragment ValidTagStart
   : PERCENT   // | ('a' .. 'z') | ('A' .. 'Z') | '_' | HANGUL_CHAR
   ;
fragment ValidTagChar
   : DOT | ValidIdChar | ('0' .. '9') | HangulChar
   ;


SQUOTE: '\'';
DQUOTE: '"';
LBRACKET: '[';
RBRACKET: ']';
LBRACE: '{';
RBRACE: '}';
LPARENTHESIS: '(';
RPARENTHESIS: ')';
EQ: '=';
SEIMCOLON: ';';
DOT: '.';
TILDE: '~';
COMMA: ',';
AND: '&';
EXCLAMATION: '!';
OR: '|';
AT: '@';
POUND: '#';
PLUS: '+';
MINUS: '-';
MUL: STAR;
DIV: SLASH;
MOD: PERCENT;
fragment STAR: '*';
fragment SLASH: '/';
fragment PERCENT: '%';

// fragment RANGLE: '>';
// fragment LANGLE: '<';
// GT: RANGLE;
// LT: LANGLE;
GTE: '>=';
LTE: '<=';

OR2: '||';
EQ2: '==';
NEQ: '!=';
//NEWLINE: '\r'? '\n';

INTEGER: [1-9][0-9]*;
FLOAT: [1-9][0-9]*('.'[0-9]+)?;

// lexical rule for hangul characters
HANGUL_CHAR: [\uAC00-\uD7A3]+;


// COMMENT
//     : '/*' .*? '*/' -> skip
// ;

// LINE_COMMENT
//     : '//' ~[\r\n]* -> skip
// ;

// Close Angle Bracket
Cab: '>';
CabCab: '>>';
// BWDANGLEBRACKETx2: '<<';
// BWDANGLEBRACKET: '<';

// Open Angle Bracket
Oab: '<';
OabOab: '<<';

PipeCab: '|>';
CabPipe: '<|';
OabPipePipeCab: '<||>';
CabOabPipe: '><|';
EqualCab: '=>';
CabPipeCab: '>|>';
PipeCabCab: '|>>';
OabOabPipe: '<<|';
OabPipeOab: '<|<';
PipeCabOab: '|><';
PipePipeOab: '||>';
CabPipePipe: '<||';
OabOabPipePipeCabCab: '<<||>>';


// CAUSAL_FWD: '>';
// CAUSAL_BWD: '<';
// CAUSAL_RESET_FWD: '|>';
// CAUSAL_RESET_BWD: '<|';
// CAUSAL_RESET_FB: '<||>';
// CAUSAL_FWD_AND_RESET_BWD: '><|' | '=>';
// CAUSAL_FWD_AND_RESET_FWD: '>|>' | '|>>';
// CAUSAL_BWD_AND_RESET_BWD: '<<|' | '<|<';
// CAUSAL_BWD_AND_RESET_FWD: '|><';



QUESTION: '?';
// FWDANGLEBRACKETx2: '>>';
// FWDANGLEBRACKET: '>';
// // FWDANGLEBRACKET_PIPE_FWDANGLEBRACKET: '>|>';    // CAUSAL_FWD_AND_RESET_FWD
// BWDANGLEBRACKETx2: '<<';
// BWDANGLEBRACKET: '<';
// // BWDANGLEBRACKET_PIPE_BWDANGLEBRACKET: '<|<';    // CAUSAL_BWD_AND_RESET_BWD
// // FWDANGLEBRACKET_BWDANGLEBRACKET_PIPE: '><|';
// // '=>';
// CAUSAL_RESET_STRONG_FWD: '||>';
// CAUSAL_RESET_STRONG_BWD: '<||';
// CAUSAL_RESET_STRONG_BIDIRECTION: '<<||>>';



// TOKEN
//    : ('0' .. '9' | 'a' .. 'z' | 'A' .. 'Z' | '-' | ' ' | '/' | '_' | ':' | ',')+
//    ;
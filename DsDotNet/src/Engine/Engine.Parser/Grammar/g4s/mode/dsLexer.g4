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


// STRING_LITERAL : '"' (~('"' | '\\' | '\r' | '\n' | ' ') | '\\' ('"' | '\\'))* '"';
STRING_LITERAL : '"' (~('"' | '\\' | '\r' | '\n') | '\\' ('"' | '\\'))* '"';

// identifier: QUOTED_IDENTIFIER | IDENTIFIER;
// QUOTED_IDENTIFIER : '"' STRING_LITERAL '"';
// fragment STRING_LITERAL: (~('"' | '\\' | '\r' | '\n' | ' ') | '\\' ('"' | '\\'))*;


// identifier: quotedIdentifier | simpleIdentifier;
// simpleIdentifier: IDENTIFIER;
// quotedIdentifier : '"' STRING_LITERAL '"';
// fragment STRING_LITERAL: (~('"' | '\\' | '\r' | '\n' | ' ') | '\\' ('"' | '\\'))*;



IDENTIFIER: VALID_ID_START VALID_ID_CHAR*;
fragment VALID_ID_START
   : ('a' .. 'z') | ('A' .. 'Z') | '_' | HANGUL_CHAR
   ;

fragment VALID_ID_CHAR
   : VALID_ID_START | ('0' .. '9') | HANGUL_CHAR
   ;



TAG_ADDRESS: VALID_TAG_START VALID_TAG_CHAR*;
fragment VALID_TAG_START
   : PERCENT   // | ('a' .. 'z') | ('A' .. 'Z') | '_' | HANGUL_CHAR
   ;

fragment VALID_TAG_CHAR
   : DOT | VALID_ID_CHAR | ('0' .. '9') | HANGUL_CHAR
   ;
// TAG_ADDRESS: TAG_CHAR+;
// fragment TAG_CHAR
//    : '%' | VALID_ID_CHAR;   //|DOT)*;



IPV4: [1-9][0-9]*'.'('0'|[1-9][0-9]*)'.'('0'|[1-9][0-9]*)'.'('0'|[1-9][0-9]*);
// IPV4: (INTEGER)(DOT) INTEGER DOT INTEGER DOT INTEGER;

BLOCK_COMMENT : '/*' (BLOCK_COMMENT|.)*? '*/' -> channel(HIDDEN) ;
LINE_COMMENT  : '//' .*? ('\n'|EOF) -> channel(HIDDEN) ;

SQUOTE: '\'';
DQUOTE: '"';
LBRACKET: '[';
RBRACKET: ']';
LBRACE: '{';
RBRACE: '}';
LPARENTHESIS: '(';
RPARENTHESIS: ')';
EQ: '==';
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

fragment RANGLE: '>';
fragment LANGLE: '<';
GT: RANGLE;
LT: LANGLE;
GTE: '>=';
LTE: '<=';

OR2: '||';
EQ2: '==';
NEQ: '!=';
//NEWLINE: '\r'? '\n';
WS: [ \t\r\n]+ -> skip;

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

CAUSAL_FWD: GT; // '>'
CAUSAL_BWD: LT; // '<'
CAUSAL_RESET_FWD: '|>';
CAUSAL_RESET_BWD: '<|';
CAUSAL_RESET_FB: '<||>';
CAUSAL_FWD_AND_RESET_BWD: '><|' | '=>';
CAUSAL_FWD_AND_RESET_FWD: '>|>' | '|>>';
CAUSAL_BWD_AND_RESET_BWD: '<<|' | '<|<';
CAUSAL_BWD_AND_RESET_FWD: '|><';



QUESTION: '?';
FWDANGLEBRACKETx2: '>>';
FWDANGLEBRACKET: '>';
// FWDANGLEBRACKET_PIPE_FWDANGLEBRACKET: '>|>';    // CAUSAL_FWD_AND_RESET_FWD
BWDANGLEBRACKETx2: '<<';
BWDANGLEBRACKET: '<';
// BWDANGLEBRACKET_PIPE_BWDANGLEBRACKET: '<|<';    // CAUSAL_BWD_AND_RESET_BWD
// FWDANGLEBRACKET_BWDANGLEBRACKET_PIPE: '><|';
// '=>';
CAUSAL_RESET_STRONG_FWD: '||>';
CAUSAL_RESET_STRONG_BWD: '<||';
CAUSAL_RESET_STRONG_BIDIRECTION: '<<||>>';



// TOKEN
//    : ('0' .. '9' | 'a' .. 'z' | 'A' .. 'Z' | '-' | ' ' | '/' | '_' | ':' | ',')+
//    ;
grammar dsLexer;


identifier: STRING_LITERAL | IDENTIFIER;
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



identifier2: identifier DOT identifier;
identifier3: identifier DOT identifier DOT identifier;

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


// M.U, M.D
segments: segmentsDNF*;
// - Segment 규격
// - 0 DOT: TagName
// - 1 DOT: TaskName.SegmentName  : mysystem 을 가정하고 있음.  필요한가?
// - 2 DOT: System.TaskName.SegmentName
segment: (identifier | identifier2 | identifier3);

flowPath: identifier2;

segmentsCNF: segment (COMMA segment)*;
segmentsDNF: segmentsCNF (OR2 segmentsCNF)*;



comment: BLOCK_COMMENT | LINE_COMMENT;
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

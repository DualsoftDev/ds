grammar dsLexer;


IDENTIFIER: VALID_ID_START VALID_ID_CHAR*;
fragment VALID_ID_START
   : ('a' .. 'z') | ('A' .. 'Z') | '_'
   ;

fragment VALID_ID_CHAR
   : VALID_ID_START | ('0' .. '9')
   ;

// M.U, M.D
segments: segment (COMMA segment)*;
segment1: IDENTIFIER;
segment2: segment1 DOT segment1;
segment: (segment1 | segment2);


comment: BLOCK_COMMENT | LINE_COMMENT;
BLOCK_COMMENT : '/*' (BLOCK_COMMENT|.)*? '*/' -> channel(HIDDEN) ;
LINE_COMMENT  : '//' .*? ('\n'|EOF) -> channel(HIDDEN) ;

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
OR: '|';
AT: '@';
//NEWLINE: '\r'? '\n';
WS: [ \t\r\n]+ -> skip;

// COMMENT
//     : '/*' .*? '*/' -> skip
// ;

// LINE_COMMENT
//     : '//' ~[\r\n]* -> skip
// ;

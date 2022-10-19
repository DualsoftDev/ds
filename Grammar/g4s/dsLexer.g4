grammar dsLexer;

WS: [ \t\r\n]+ -> skip;
comment: BLOCK_COMMENT | LINE_COMMENT;
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

identifier1: IDENTIFIER1;
identifier2: IDENTIFIER2;
identifier3: IDENTIFIER3;
identifier4: IDENTIFIER4;

// // - Segment 규격
// // - 0 DOT: TagName
// // - 1 DOT: TaskName.SegmentName  : mysystem 을 가정하고 있음.  필요한가?
// // - 2 DOT: System.TaskName.SegmentName
identifier12: identifier1 | identifier2;
identifier123: identifier12 | identifier3;

flowPath: identifier2;

identifier123CNF: identifier123 (COMMA identifier123)*;
identifier123DNF: identifier123CNF (OR2 identifier123CNF)*;

identifier1234: (identifier1 | identifier2 | identifier3 | identifier4);

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

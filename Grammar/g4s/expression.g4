// https://github.com/antlr/grammars-v4/blob/master/arithmetic/arithmetic.g4
grammar expression;

WS: [ \t\r\n]+ -> skip;
BLOCK_COMMENT : '/*' (BLOCK_COMMENT|.)*? '*/' -> channel(HIDDEN) ;
LINE_COMMENT  : '//' .*? ('\n'|EOF) -> channel(HIDDEN) ;

comment: BLOCK_COMMENT | LINE_COMMENT;
identifier : IDENTIFIER;
    variable: identifier;

expression: (terminal | function | expr);

function: functionName LPAREN expression (';' expression)* RPAREN;
    functionName: '@' identifier;

terminal: literal | tag | identifier;

literal: variable | scientific | integer;
    scientific: SCIENTIFIC_NUMBER;
    integer: INTEGER;
tag: TAG;
    TAG: '$' IDENTIFIER;

expr:   IDENTIFIER '(' exprList? ')'    // func call like f(), f(x), f(1,2)
    |   IDENTIFIER '[' expr ']'         // array index like a[i], a[i][j]
    |   '-' expr                // unary minus
    |   '!' expr                // boolean not
    |   expr ('+'|'-'|'*'|'/'|'%') expr
    |   expr '==' expr          // equality comparison (lowest priority op)
    |   IDENTIFIER                      // variable reference
    |   INTEGER
    |   '(' expr ')'
    ;
exprList : expr (',' expr)* ;   // arg list    

INTEGER: SIGN? UNSIGNED_INTEGER;
IDENTIFIER: VALID_ID_START VALID_ID_CHAR*;
fragment VALID_ID_START: ('a' .. 'z') | ('A' .. 'Z') | '_';
fragment VALID_ID_CHAR: VALID_ID_START | ('0' .. '9');

SCIENTIFIC_NUMBER: NUMBER (E SIGN? UNSIGNED_INTEGER)?;
fragment NUMBER: ('0' .. '9') + ('.' ('0' .. '9') +)?;
fragment UNSIGNED_INTEGER: ('0' .. '9')+;

fragment E : 'E' | 'e' ;
fragment SIGN : ('+' | '-') ;

LPAREN : '(' ;
RPAREN : ')' ;
PLUS : '+' ;
MINUS : '-' ;
TIMES : '*' ;
DIV : '/' ;
GT : '>' ;
LT : '<' ;
EQ : '=' ;
POINT : '.' ;
POW : '^' ;
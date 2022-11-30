// grun expr exprs < g4test/expr/exp.exp
// https://github.com/antlr/grammars-v4/blob/master/arithmetic/arithmetic.g4
grammar expr;

WS: [ \t\r\n]+ -> skip;
BLOCK_COMMENT : '/*' (BLOCK_COMMENT|.)*? '*/' -> channel(HIDDEN) ;
LINE_COMMENT  : '//' .*? ('\n'|EOF) -> channel(HIDDEN) ;

comment: BLOCK_COMMENT | LINE_COMMENT;
identifier : IDENTIFIER;
    variable: identifier;
    //tagName: identifier;
    functionName: identifier;

// expression: (terminal | function | expr);

// function: functionName '[' expression (';' expression)* ']';

// terminal: literal | tag | identifier;

literal: variable | tag | scientific | integer;
    scientific: SCIENTIFIC_NUMBER;
    integer: INTEGER;
tag: TAG;
    TAG: '$' IDENTIFIER;

exprs: ( (expr|statement) ';')*;     // for debugging purpose

statement: assign;
assign: expr ':=' expr;

// https://stackoverflow.com/questions/41017948/antlr4-the-following-sets-of-rules-are-mutually-left-recursive
// https://github.com/antlr/antlr4/blob/master/doc/parser-rules.md#alternative-labels
expr:   functionName '(' arguments? ')'         # FunctionCallExpr  // func call like f(), f(x), f(1,2)
    |   variable ('[' expr ']')+                # ArrayReferenceExpr // array index like a[i], a[i][j]
    |   ('-'|'!') expr                          # UnaryExpr           // unary minus, boolean not
    |   expr (
            | '+'|'-'|'*'|'/'|'%'
            | '&&' | '||' 
            |'=' | '!='
            //|':='
            ) expr                              # BinaryExpr // ':=': assignment equality comparison (lowest priority op)
    //|   expr ':=' expr                          # AssignmentStatement
    |   literal                                 # LiteralExpr
    |   '(' expr ')'                            # ParenthesysExpr
    ;

    functionCall: functionName '(' arguments? ')';    // func call like f(), f(x), f(1,2)
    arguments: exprList;
    exprList : expr (',' expr)* ;   // arg list
    // arrayReference: variable ('[' expr ']')+;           // array index like a[i], a[i][j]
    // unary: ('-'|'!') expr;                           // unary minus, boolean not
    // binary: expr ('+'|'-'|'*'|'/'|'%'|'=') expr;      // '=': equality comparison (lowest priority op)

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
LBACKET : '[' ;
RBACKET : ']' ;
PLUS : '+' ;
MINUS : '-' ;
TIMES : '*' ;
DIV : '/' ;
GT : '>' ;
LT : '<' ;
EQ : '=' ;
POINT : '.' ;
POW : '^' ;
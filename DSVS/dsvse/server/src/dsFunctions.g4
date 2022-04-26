grammar dsFunctions;

import dsLexer;

value
    : segment
    | func
    ;

logicalBinaryOperator
    : '*' | '/' | '%'   // MUL | DIV | MOD
    | '+' | '-'         // PLUS | MINUS
    | '&&'
    | '||'       // AND | OR
    | '==' | '!=' | '>=' | '<=' | '>' | '<' // DO *NOT* use like... EQ2 | NEQ | GTE | LTE | GT | LT
    ;

/*
 * Expression
 */
expression
    : func
    | expression logicalBinaryOperator expression
    | LPARENTHESIS expression RPARENTHESIS
    | segment
    | NUMBER
    ;

// unary function : #fun(args)
funcSet: POUND 'set' LPARENTHESIS segment RPARENTHESIS;
funcG: POUND 'g' LPARENTHESIS segment RPARENTHESIS;
funcH: POUND 'h' LPARENTHESIS segment RPARENTHESIS;

// binary function : #fun(arg1, arg2)
funcLatch: POUND 'latch' LPARENTHESIS value COMMA value RPARENTHESIS;
funcXOR: POUND 'xor' LPARENTHESIS value COMMA value RPARENTHESIS;
funcNXOR: POUND 'nxor' LPARENTHESIS value COMMA value RPARENTHESIS;
funcNAND: POUND 'nand' LPARENTHESIS value COMMA value RPARENTHESIS;
funcNOR: POUND 'nor' LPARENTHESIS value COMMA value RPARENTHESIS;
funcExpression: POUND LPARENTHESIS expression RPARENTHESIS;

// #, 값, 네모
func
    : funcSet
    | funcLatch
    | funcG
    | funcH
    | funcXOR
    | funcNXOR
    | funcNAND
    | funcNOR
    | funcExpression
    ;

// @, 세그먼트, 동그라미
proc
    : procAssign
    | procSleepMs
    | procSleepS
    ;
procAssign: AT LPARENTHESIS segment EQ value RPARENTHESIS;    // @(C = #sin(#num(B)))
procSleepMs: AT MS LPARENTHESIS (INTEGER|value) RPARENTHESIS;    // @ms(500)
procSleepS: AT S LPARENTHESIS (INTEGER|value) RPARENTHESIS;    // @s(2)


MS: 'ms';
S: 's';

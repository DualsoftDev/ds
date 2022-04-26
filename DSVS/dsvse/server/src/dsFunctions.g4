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
 * Expression : https://docs.microsoft.com/en-us/cpp/c-language/precedence-and-order-of-evaluation?view=msvc-170
 */
expression
    : func
    | ('!' | '+' | '-' ) expression // unary opeartor
    | expression ('*' | '/' | '%') expression
    | expression ('+' | '-') expression
    | expression ('>=' | '<=' | '>' | '<') expression
    | expression ('==' | '!=') expression
    | expression ('&&') expression
    | expression ('||') expression

    | LPARENTHESIS expression RPARENTHESIS
    | segment
    | number
    ;

number
    : FLOAT
    | INTEGER
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

funcConvNum: POUND 'num' LPARENTHESIS value RPARENTHESIS;
funcConvStr: POUND 'str' LPARENTHESIS value RPARENTHESIS;
funcConvBCD: POUND 'bcd' LPARENTHESIS value RPARENTHESIS;
funcConvBin: POUND 'bin' LPARENTHESIS value RPARENTHESIS;

funcConvAbs: POUND 'abs' LPARENTHESIS value RPARENTHESIS;
funcConvSin: POUND 'sin' LPARENTHESIS value RPARENTHESIS;
funcConvCos: POUND 'cos' LPARENTHESIS value RPARENTHESIS;
funcConvRound: POUND 'round' LPARENTHESIS value RPARENTHESIS;

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

    | funcConvNum
    | funcConvStr
    | funcConvBCD
    | funcConvBin

    | funcConvAbs
    | funcConvSin
    | funcConvCos
    | funcConvRound

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

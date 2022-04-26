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
    | string
    ;

number
    : FLOAT
    | INTEGER
    ;
string
    : '"' (~'"')* '"'
    | '\'' (~'\'')* '\''
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

funcSysToggleMs: POUND '_togglems' LPARENTHESIS INTEGER RPARENTHESIS;
funcSysToggleS: POUND '_toggles' LPARENTHESIS INTEGER RPARENTHESIS;
funcSysCurrentTime: POUND '_currt' LPARENTHESIS TIMESTRING RPARENTHESIS;

TIMESTRING: '\'' [0-9.:]* '\'';

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
    | funcSysToggleMs
    | funcSysToggleS
    | funcSysCurrentTime
    ;

// @, 세그먼트, 동그라미
proc
    : procAssign
    | procSleepMs
    | procSleepS

    | procStartFirst
    | procLastFirst
    | procPushStart
    | procPushReset
    | procPushStart

    | procOnlyStart
    | procOnlyReset
    | procSelfStart
    | procSelfReset
    ;

procAssign: AT LPARENTHESIS segment EQ expression RPARENTHESIS;    // @(C = #sin(#num(B)))
procSleepMs: AT 'ms' LPARENTHESIS (INTEGER|value) RPARENTHESIS;    // @ms(500)
procSleepS: AT 's' LPARENTHESIS (INTEGER|value) RPARENTHESIS;    // @s(2)

procStartFirst: AT 'sf' LPARENTHESIS segment RPARENTHESIS;    // @sf(A)
procLastFirst: AT 'lf' LPARENTHESIS segment RPARENTHESIS;    // @lf(A)
procPushStart: AT 'pushs' LPARENTHESIS segment RPARENTHESIS;    // @pushs(A)
procPushReset: AT 'pushr' LPARENTHESIS segment RPARENTHESIS;    // @pushr(A)
procPushStartReset: AT 'pushsr' LPARENTHESIS segment RPARENTHESIS;    // @pushr(A)

procOnlyStart: AT 'onlys' LPARENTHESIS segment RPARENTHESIS;    // @onlys(A)
procOnlyReset: AT 'onlyr' LPARENTHESIS segment RPARENTHESIS;    // @onlyr(A)
procSelfStart: AT 'selfs' LPARENTHESIS segment RPARENTHESIS;    // @selfs(A)
procSelfReset: AT 'selfr' LPARENTHESIS segment RPARENTHESIS;    // @selfr(A)

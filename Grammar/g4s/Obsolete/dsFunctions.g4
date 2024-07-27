// grammar dsFunctions;

// import dsLexer;

// logicalBinaryOperator
//     : '*' | '/' | '%'   // MUL | DIV | MOD
//     | '+' | '-'         // PLUS | MINUS
//     | '&&'
//     | '||'       // AND | OR
//     | '==' | '!=' | '>>=' | '<<=' | '>>' | '<<' // DO *NOT* use like... EQ2 | NEQ | GTE | LTE | GT | LT
//     ;

// /*
//  * Expression : https://docs.microsoft.com/en-us/cpp/c-language/precedence-and-order-of-evaluation?view=msvc-170
//  */
// expression
//     : func
//     | ('!' | '+' | '-' ) expression // unary opeartor
//     | expression ('*' | '/' | '%') expression
//     | expression ('+' | '-') expression
//     | expression (':>=' | ':<=' | ':>' | ':<') expression
//     | expression ('==' | '!=') expression
//     | expression ('&&') expression
//     | expression ('||') expression

//     | LPARENTHESIS expression RPARENTHESIS
//     | identifier123
//     | number
//     | string
//     ;

// number
//     : FLOAT
//     | INTEGER
//     ;
// string
//     : '"' (~'"')* '"'
//     | '\'' (~'\'')* '\''
//     ;

// segValue
//     : identifier123
//     | func
//     ;
// value
//     : segValue
//     | string
//     | number
//     ;

// // unary function : #fun(args)
// funcSet: POUND 'set' LPARENTHESIS identifier123 RPARENTHESIS;
// funcG: POUND 'g' LPARENTHESIS identifier123 RPARENTHESIS;
// funcH: POUND 'h' LPARENTHESIS identifier123 RPARENTHESIS;

// // binary function : #fun(arg1, arg2)
// funcLatch: POUND 'latch' LPARENTHESIS segValue COMMA segValue RPARENTHESIS;
// funcXOR: POUND 'xor' LPARENTHESIS value COMMA value RPARENTHESIS;
// funcNXOR: POUND 'nxor' LPARENTHESIS value COMMA value RPARENTHESIS;
// funcNAND: POUND 'nand' LPARENTHESIS value COMMA value RPARENTHESIS;
// funcNOR: POUND 'nor' LPARENTHESIS value COMMA value RPARENTHESIS;
// funcExpression: POUND LPARENTHESIS expression RPARENTHESIS;

// funcConvNum: POUND 'num' LPARENTHESIS value RPARENTHESIS;
// funcConvStr: POUND 'str' LPARENTHESIS value RPARENTHESIS;
// funcConvBCD: POUND 'bcd' LPARENTHESIS value RPARENTHESIS;
// funcConvBin: POUND 'bin' LPARENTHESIS value RPARENTHESIS;

// funcConvAbs: POUND 'abs' LPARENTHESIS value RPARENTHESIS;
// funcConvSin: POUND 'sin' LPARENTHESIS value RPARENTHESIS;
// funcConvCos: POUND 'cos' LPARENTHESIS value RPARENTHESIS;
// funcConvRound: POUND 'round' LPARENTHESIS value RPARENTHESIS;

// funcSysToggleMs: POUND '_togglems' LPARENTHESIS INTEGER RPARENTHESIS;
// funcSysToggleS: POUND '_toggles' LPARENTHESIS INTEGER RPARENTHESIS;
// funcSysCurrentTime: POUND '_currt' LPARENTHESIS TIMESTRING RPARENTHESIS;

// TIMESTRING: '\'' [0-9.:]* '\'';

// // #, 값, 네모
// func
//     : funcSet
//     | funcLatch
//     | funcG
//     | funcH
//     | funcXOR
//     | funcNXOR
//     | funcNAND
//     | funcNOR
//     | funcExpression

//     | funcConvNum
//     | funcConvStr
//     | funcConvBCD
//     | funcConvBin

//     | funcConvAbs
//     | funcConvSin
//     | funcConvCos
//     | funcConvRound
//     | funcSysToggleMs
//     | funcSysToggleS
//     | funcSysCurrentTime
//     ;

// // @, 세그먼트, 동그라미
// proc
//     : procAssign
//     | procSleepMs
//     | procSleepS

//     | procStartFirst
//     | procLastFirst
//     | procPushStart
//     | procPushReset
//     | procPushStart

//     | procOnlyStart
//     | procOnlyReset
//     | procSelfStart
//     | procSelfReset
//     ;

// procAssign: AT LPARENTHESIS identifier123 EQ expression RPARENTHESIS;    // @(C = #sin(#num(B)))
// procSleepMs: AT 'ms' LPARENTHESIS (INTEGER|value) RPARENTHESIS;    // @ms(500)
// procSleepS: AT 's' LPARENTHESIS (INTEGER|value) RPARENTHESIS;    // @s(2)

// procStartFirst: AT 'sf' LPARENTHESIS identifier123 RPARENTHESIS;    // @sf(A)
// procLastFirst: AT 'lf' LPARENTHESIS identifier123 RPARENTHESIS;    // @lf(A)
// procPushStart: AT 'pushs' LPARENTHESIS identifier123 RPARENTHESIS;    // @pushs(A)
// procPushReset: AT 'pushr' LPARENTHESIS identifier123 RPARENTHESIS;    // @pushr(A)
// procPushStartReset: AT 'pushsr' LPARENTHESIS identifier123 RPARENTHESIS;    // @pushr(A)

// procOnlyStart: AT 'onlys' LPARENTHESIS identifier123 RPARENTHESIS;    // @onlys(A)
// procOnlyReset: AT 'onlyr' LPARENTHESIS identifier123 RPARENTHESIS;    // @onlyr(A)
// procSelfStart: AT 'selfs' LPARENTHESIS identifier123 RPARENTHESIS;    // @selfs(A)
// procSelfReset: AT 'selfr' LPARENTHESIS identifier123 RPARENTHESIS;    // @selfr(A)

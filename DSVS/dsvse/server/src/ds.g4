/*
 * ------------------------------------------------------
 * WARNING: 변수 이름 변경시, 관련 코드 모두 수정되어야 함.
 * ------------------------------------------------------
 *  - 대문자 : Lexical rule
 *  - 소문자 : Parser rule
 *  - 수정 후
 *      1. ../../../.. 에서 make g4 수행
 *      1. dsvse/server/src/parser.ts 수정
 */


// https://www.youtube.com/watch?v=bfiAvWZWnDA&t=157s

// https://github.com/tunnelvisionlabs/antlr4ts
// ds-master/dsvs/dsvse/server$ yarn antlr4ts src/ds-language.g4

grammar ds;

IDENTIFIER: VALID_ID_START VALID_ID_CHAR*;
fragment VALID_ID_START
   : ('a' .. 'z') | ('A' .. 'Z') | '_'
   ;


fragment VALID_ID_CHAR
   : VALID_ID_START | ('0' .. '9')
   ;

program: (system|comment)* EOF;


system: sysHdr segment1_ '=' sysBlock;    // [Sys] Seg = {..}


sysHdr: LBRACKET sys_ RBRACKET;  // [Sys]
sysBlock
    : simpleSysBlock        #caseSimpleSysBlock
    | complexSysBlock       #caseComplexSysBlock
    ;
simpleSysBlock:  LBRACE segment1_ (';' segment1_)* RBRACE;
complexSysBlock: LBRACE (acc|macro|causal)* RBRACE;

acc: LBRACKET accSRE RBRACKET EQ LBRACE segment1_ (SEIMCOLON segment1_)* RBRACE;    // [accSRE] = { A; B }


/*
 * MACRO definitions
 */
// [macro=T] = { (call)* }
macro: LBRACKET macroHeader RBRACKET EQ LBRACE (call)* RBRACE;
macroHeader
    : simpleMacroHeader
    | namedMacroHeader
    ;
simpleMacroHeader: 'macro';
namedMacroHeader: 'macro' EQ segment1_;

// A23 = { M.U ~ S.S3U }
call: segment1_ EQ LBRACE segment2s_ TILDE segment2s_ RBRACE;
// M.U, M.D
segment2s_: segment2_ (COMMA segment2_)*;

// B.F1 > Set1F <| T.A21;
causal
    : expression causalOperator expression (causalOperator expression)* SEIMCOLON
    // | expression causalOperator expression SEIMCOLON
    ;
//causal: expression causalOperator causal SEIMCOLON;

causalOperator: '<' | '>' | '<|' | '|>' | '<|>';
logicalBinaryOperator: '&' | '|';

segment1_: WS* IDENTIFIER WS*;
segment2_: segment1_ DOT segment1_;
segmentAny_: (segment1_ | segment2_);


/*
 * Expression
 */
expression
    : segmentAny_
    | expression logicalBinaryOperator expression
    | LPARENTHESIS expression RPARENTHESIS
    ;

comment: BLOCK_COMMENT | LINE_COMMENT;
BLOCK_COMMENT : '/*' (BLOCK_COMMENT|.)*? '*/' -> channel(HIDDEN) ;
LINE_COMMENT  : '//' .*? ('\n'|EOF) -> channel(HIDDEN) ;


// COMMENT
//     : '/*' .*? '*/' -> skip
// ;

// LINE_COMMENT
//     : '//' ~[\r\n]* -> skip
// ;

sys_: 'Sys';

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
//NEWLINE: '\r'? '\n';

accSRE: ('accSRE'|'accSR'|'accRE'|'accSE'|'accS'|'accR'|'accE');

WS: [ \t\r\n]+ -> skip;

// TOKEN
//    : ('0' .. '9' | 'a' .. 'z' | 'A' .. 'Z' | '-' | ' ' | '/' | '_' | ':' | ',')+
//    ;
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

import dsFunctions;

program: (system|comment)* EOF;


system: sysHdr segment1 '=' sysBlock;    // [Sys] Seg = {..}


sysHdr: LBRACKET sys_ RBRACKET;  // [Sys]
sysBlock
    : simpleSysBlock        //#caseSimpleSysBlock
    | complexSysBlock       //#caseComplexSysBlock
    ;
simpleSysBlock:  LBRACE segment1 (';' segment1)* RBRACE;
complexSysBlock: LBRACE (acc|macro|causal)* RBRACE;

acc: LBRACKET accSRE RBRACKET EQ LBRACE segment1 (SEIMCOLON segment1)* RBRACE;    // [accSRE] = { A; B }


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
namedMacroHeader: 'macro' EQ segment1;

// A23 = { M.U ~ S.S3U }
call: segment1 EQ LBRACE segments TILDE segments RBRACE;

// B.F1 > Set1F <| T.A21;
causal
    : causalExpression causalOperator causalExpression SEIMCOLON
    | causalExpression causalBwdOperator expression SEIMCOLON
    | expression causalFwdOperator causalExpression SEIMCOLON
    ;
//causal: expression causalOperator causal SEIMCOLON;

logicalBinaryOperator: '&' | '|';
 
/*
 * Causal Expression
    A, B |> C
    (A & B) |> C
 */
causalExpression
    : segments
    | causalExpression  causalOperator      causalExpression
    | expression        causalFwdOperator   causalExpression
    | causalExpression  causalBwdOperator   expression
    ;
/*
 * Expression
 */
expression
    : segment
    | func
    | expression logicalBinaryOperator expression
    | LPARENTHESIS expression RPARENTHESIS
    ;


causalOperator
    : causalFwdOperator
    | causalBwdOperator
    | causalFBOperator
    ;

causalFwdOperator
    : CAUSAL_FWD
    | CAUSAL_RESET_FWD
    | CAUSAL_FWD_AND_RESET_FWD  // '>|>' | '|>>';
    ;
causalBwdOperator
    : CAUSAL_BWD
    | CAUSAL_RESET_BWD
    | CAUSAL_BWD_AND_RESET_BWD  // '<<|' | '<|<';
    ;
causalFBOperator
    : CAUSAL_RESET_FB           // <||>
    | CAUSAL_FWD_AND_RESET_BWD  // '><|';
    | CAUSAL_BWD_AND_RESET_FWD  // '|><';
    ;



CAUSAL_FWD: '>';
CAUSAL_BWD: '<';
CAUSAL_RESET_FWD: '|>';
CAUSAL_RESET_BWD: '<|';
CAUSAL_RESET_FB: '<||>';
CAUSAL_FWD_AND_RESET_BWD: '><|';
CAUSAL_FWD_AND_RESET_FWD: '>|>' | '|>>';
CAUSAL_BWD_AND_RESET_BWD: '<<|' | '<|<';
CAUSAL_BWD_AND_RESET_FWD: '|><';


sys_: 'Sys';


accSRE: ('accSRE'|'accSR'|'accRE'|'accSE'|'accS'|'accR'|'accE');


// TOKEN
//    : ('0' .. '9' | 'a' .. 'z' | 'A' .. 'Z' | '-' | ' ' | '/' | '_' | ':' | ',')+
//    ;
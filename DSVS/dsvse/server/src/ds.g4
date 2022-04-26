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


system: sysHdr IDENTIFIER '=' sysBlock;    // [sys] Seg = {..}


sysHdr: LBRACKET sys_ RBRACKET;  // [sys]
sysBlock
    : simpleSysBlock        //#caseSimpleSysBlock
    | complexSysBlock       //#caseComplexSysBlock
    ;
simpleSysBlock:  LBRACE IDENTIFIER (';' IDENTIFIER)* RBRACE;
complexSysBlock
    : LBRACE (acc|macro|causal|importStatement|call (SEIMCOLON)?)* (causalPhrase|call)? RBRACE;
//complexSysBlock: LBRACE (acc|macro|causal|importStatement)* (causalPhrase|importFinal)? RBRACE;

acc: LBRACKET ACCESS_SRE RBRACKET EQ LBRACE IDENTIFIER (SEIMCOLON IDENTIFIER)* RBRACE;    // [accsre] = { A; B }


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
namedMacroHeader: 'macro' EQ IDENTIFIER;

// A23 = { M.U ~ S.S3U ~ _ }
call: IDENTIFIER EQ LBRACE segments TILDE segments (TILDE segments)? RBRACE;

// B.F1 > Set1F <| T.A21;
causal
    : causalPhrase SEIMCOLON
    ;


// debugging purpose {
causals: causal* (causalPhrase)?;
importStatements: importStatement+ ;
expressions: (expression SEIMCOLON)+ ;
calls: (call SEIMCOLON)+ ;
// } debugging purpose



causalPhrase
    : causalTokensDNF (causalOperator causalTokensDNF)+
    ;

causalToken
    : proc
    | func
    | segmentValue  // '(A)' or '(A.B)'
    | expression
    | segment       // 'A' or 'A.B'
    ;
segmentValue: LPARENTHESIS segment RPARENTHESIS;

causalTokensCNF
    : causalToken (COMMA causalToken)*
    ;
causalTokensDNF
    : causalTokensCNF (OR2 causalTokensCNF)*
    ;

importFinal
    : '!#import' importAs 'from' quotedFilePath
    ;
importStatement : importFinal SEIMCOLON;
importAs
    : IDENTIFIER 'as' IDENTIFIER
    | LBRACE IDENTIFIER 'as' IDENTIFIER
        (COMMA IDENTIFIER 'as' IDENTIFIER)* (COMMA)?
        RBRACE
    ;
quotedFilePath
    : DQUOTE (~DQUOTE)* DQUOTE
    | SQUOTE (~SQUOTE)* SQUOTE
    ;





causalOperator
    : '>'   // CAUSAL_FWD
    | '|>'  // CAUSAL_RESET_FWD
    | '>|>' | '|>>'     //CAUSAL_FWD_AND_RESET_FWD  // '>|>' | '|>>';
    | '<'   // CAUSAL_BWD
    | '<|'  // CAUSAL_RESET_BWD
    | '<<|' | '<|<' // CAUSAL_BWD_AND_RESET_BWD
    | '<||>'        // CAUSAL_RESET_FB         
    | '><|'         // CAUSAL_FWD_AND_RESET_BWD
    | '|><'         // CAUSAL_BWD_AND_RESET_FWD
    ;

// causalOperator
//     : causalFwdOperator
//     | causalBwdOperator
//     | causalFBOperator
//     ;

// causalFwdOperator
//     : CAUSAL_FWD
//     | CAUSAL_RESET_FWD
//     | CAUSAL_FWD_AND_RESET_FWD  // '>|>' | '|>>';
//     ;
// causalBwdOperator
//     : CAUSAL_BWD
//     | CAUSAL_RESET_BWD
//     | CAUSAL_BWD_AND_RESET_BWD  // '<<|' | '<|<';
//     ;
// causalFBOperator
//     : CAUSAL_RESET_FB           // <||>
//     | CAUSAL_FWD_AND_RESET_BWD  // '><|';
//     | CAUSAL_BWD_AND_RESET_FWD  // '|><';
//     ;



CAUSAL_FWD: GT; // '>'
CAUSAL_BWD: LT; // '<'
CAUSAL_RESET_FWD: '|>';
CAUSAL_RESET_BWD: '<|';
CAUSAL_RESET_FB: '<||>';
CAUSAL_FWD_AND_RESET_BWD: '><|';
CAUSAL_FWD_AND_RESET_FWD: '>|>' | '|>>';
CAUSAL_BWD_AND_RESET_BWD: '<<|' | '<|<';
CAUSAL_BWD_AND_RESET_FWD: '|><';


sys_: 'sys';


ACCESS_SRE: ('accsre'|'accsr'|'accre'|'accse'|'accs'|'accr'|'acce');


// TOKEN
//    : ('0' .. '9' | 'a' .. 'z' | 'A' .. 'Z' | '-' | ' ' | '/' | '_' | ':' | ',')+
//    ;
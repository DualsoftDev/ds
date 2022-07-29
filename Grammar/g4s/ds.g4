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

program: (importStatement|system|cpus|comment)* EOF;


system: sysProp id '=' sysBlock;    // [sys] Seg = {..}
sysProp: '[' 'sys' ']';
sysBlock
    : LBRACE (task|flow|listing|alias|parenting|causal|call|acc|macro)* RBRACE
    ;


/*
[cpus] AllCpus = {
    [cpu] Cpu = {
        L.F;
    }
}
 */

cpus: cpusProp (id)? '=' cpusBlock;
cpusProp: '[' 'cpus' ']';
cpusBlock
    : LBRACE (cpu)* RBRACE
    ;

cpu: cpuProp id '=' cpuBlock;    // [cpu] Cpu = {..}
cpuProp: '[' 'cpu' ']';
cpuBlock
    : LBRACE flowPath (SEIMCOLON flowPath)* SEIMCOLON? RBRACE
    ;

task
    : taskProp id '=' LBRACE (listing|call)* RBRACE
    ;
taskProp: '[' 'task' ']';

flow
    : flowProp id '=' LBRACE (causal|parenting|listing)* RBRACE
    ;
flowProp : '[' 'flow' ('of' id)? ']';

alias
    : aliasProp (id)? '=' LBRACE (aliasListing)* RBRACE
    ;
aliasProp: '[' 'alias' ']';
aliasListing:
    aliasDef '=' LBRACE (aliasMnemonic)? ( ';' aliasMnemonic)* (';')+ RBRACE
    ;
aliasDef: (IDENTIFIER DOT IDENTIFIER DOT IDENTIFIER);
aliasMnemonic: IDENTIFIER;


id: IDENTIFIER;

acc: LBRACKET ACCESS_SRE RBRACKET EQ LBRACE IDENTIFIER (SEIMCOLON IDENTIFIER)* SEIMCOLON? RBRACE;    // [accsre] = { A; B }
listing: id SEIMCOLON;     // A;
parenting: id EQ LBRACE causal* RBRACE;

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
call: id EQ LBRACE callPhrase RBRACE;
callPhrase: segments TILDE segments (TILDE segments)?;

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

causalTokensDNF
    : causalTokensCNF ('?' causalTokensCNF)*
    ;
causalTokensCNF
    : causalToken (',' causalToken)*
    ;

importFinal
    : '!#import' importAs 'from' quotedFilePath
    ;
importStatement : importFinal SEIMCOLON;

importAs
    : importPhrase
    | LBRACE importPhrase
        (COMMA importPhrase)* (COMMA)?
        RBRACE
    ;

importPhrase: importSystemName 'as' importAlias;
importSystemName: IDENTIFIER;
importAlias: IDENTIFIER;


quotedFilePath
    : DQUOTE (~DQUOTE)* DQUOTE
    | SQUOTE (~SQUOTE)* SQUOTE
    ;





causalOperator
    : '>>'
    | '>'   // CAUSAL_FWD
    | '|>>'  // CAUSAL_RESET_FWD_STRONG
    | '|>'  // CAUSAL_RESET_FWD
    | '>|>'  //CAUSAL_FWD_AND_RESET_FWD
    | '<<'   // CAUSAL_BWD_STRONG
    | '<'   // CAUSAL_BWD
    | '<<|'  // CAUSAL_RESET_BWD
    | '<|'  // CAUSAL_RESET_BWD
    | '<|<' // CAUSAL_BWD_AND_RESET_BWD
    | '<<||>>'        // CAUSAL_RESET_FB_STRONG
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
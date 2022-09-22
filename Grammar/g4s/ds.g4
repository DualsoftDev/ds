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

program: (system|cpus|layouts|addresses|properties|comment)* EOF;        // importStatement|

test:qstring EOF;
qstring: STRING_LITERAL EOF;


system: sysProp identifier1 '=' sysBlock;    // [sys] Seg = {..}
sysProp: '[' 'sys' ']';
sysBlock
    : LBRACE (flow|buttons)* RBRACE       // identifier1Listing|parenting|causal|call
    ;


/*
[cpus] AllCpus = {
    [cpu] Cpu = {
        L.F;
    }
}
 */

cpus: cpusProp (identifier1)? '=' cpusBlock;
cpusProp: '[' 'cpus' ']';
cpusBlock
    : LBRACE (cpu)* RBRACE
    ;

cpu: cpuProp identifier1 '=' cpuBlock;    // [cpu] Cpu = {..}
cpuProp: '[' 'cpu' ']';
cpuBlock
    : LBRACE flowPath (SEIMCOLON flowPath)* SEIMCOLON? RBRACE
    ;

layouts: layoutProp (identifier1)? '=' layoutsBlock;
layoutProp: '[' 'layouts' ']';
layoutsBlock
    : LBRACE (positionDef)* RBRACE
    ;
positionDef: callPath '=' xywh;
    callPath: identifier3;
    xywh: LPARENTHESIS x COMMA y (COMMA w COMMA h)? RPARENTHESIS (SEIMCOLON)?;
    x: INTEGER;
    y: INTEGER;
    w: INTEGER;
    h: INTEGER;

addresses: addressesProp (identifier1)? '=' addressesBlock;
addressesProp: '[' 'addresses' ']';
addressesBlock
    : LBRACE (addressDef)* RBRACE
    ;
addressDef: segmentPath '=' address;
    segmentPath: identifier3;
    address: LPARENTHESIS (startTag)? COMMA (resetTag)? COMMA (endTag)? RPARENTHESIS (SEIMCOLON)?;
    startTag: TAG_ADDRESS;
    resetTag: TAG_ADDRESS;
    endTag: TAG_ADDRESS;


/*
// global safety property
[prop] {
    [safety] = {
        L.F.Main = {A.F.Vm; B.F.Vm}
    }
}
// local safety property
[sys] L = {
    [flow] F = {
        Main = { T.Cp > T.Cm; }
        [safety] = {
            Main = {A.F.Vm; B.F.Vm}
        }
    }
}
 */
properties: '[' 'prop' ']' EQ LBRACE (propertyBlock)* RBRACE;
propertyBlock: (safetyBlock);
safetyBlock: '[' 'safety' ']' EQ LBRACE (safetyDef)* RBRACE;
safetyDef: safetyKey EQ LBRACE safetyValues RBRACE;
safetyKey: identifier123;
safetyValues: identifier123 (SEIMCOLON identifier123)*;


flow
    : flowProp identifier1 '=' LBRACE (causal|parenting|call|identifier1Listing|safetyBlock|alias)* RBRACE     // |flowTask
    ;
flowProp : '[' 'flow' ('of' identifier1)? ']';

alias
    : aliasProp (identifier1)? '=' LBRACE (aliasListing)* RBRACE
    ;
aliasProp: '[' 'alias' ']';
aliasListing:
    aliasDef '=' LBRACE (aliasMnemonic)? ( ';' aliasMnemonic)* (';')+ RBRACE
    ;
aliasDef: identifier123;     //(identifier2|identifier3);
aliasMnemonic: identifier1;


identifier1Listing: identifier1 SEIMCOLON;     // A;
identifier2Listing: identifier2 SEIMCOLON;     // A;
identifier12Listing: (identifier1Listing | identifier2Listing);
parenting: identifier1 EQ LBRACE (causal|identifier12Listing)* RBRACE;

// A23 = { M.U ~ S.S3U ~ _ }
call: identifier1 EQ LBRACE callPhrase RBRACE;
callPhrase: callComponents TILDE callComponents;    // (TILDE callComponents)?;
    callComponents: identifier123DNF*;
calls: (call SEIMCOLON)+ ;

buttons:emergencyButtons|autoButtons|startButtons|resetButtons;
emergencyButtons :'[' ('emg_in'|'emg') ']'     EQ buttonBlock;
autoButtons      :'[' ('auto_in'|'auto') ']'   EQ buttonBlock;
startButtons     :'[' ('start_in'|'start') ']' EQ buttonBlock;
resetButtons     :'[' ('reset_in'|'reset') ']' EQ buttonBlock;
buttonBlock: LBRACE (() | ((SEIMCOLON)* buttonDef)* (SEIMCOLON)*) RBRACE;
buttonDef: buttonName EQ LBRACE (() | flowName (SEIMCOLON flowName)* (SEIMCOLON)?) RBRACE;
buttonName: identifier1;
flowName : identifier1;



// B.F1 > Set1F <| T.A21;
causal
    : causalPhrase SEIMCOLON
    ;


// debugging purpose {
causals: causal* (causalPhrase)?;

expressions: (expression SEIMCOLON)+ ;
// } debugging purpose


causalPhrase
    : causalTokensDNF (causalOperator causalTokensDNF)+
    ;


causalTokensDNF
    : causalTokensCNF ('?' causalTokensCNF)*
    ;
causalTokensCNF
    : causalToken (',' causalToken)*
    ;

causalToken
    : proc
    | func
    | expression
    | identifier1
//  | segmentValue  // '(A)' or '(A.B)'
    ;
//segmentValue: LPARENTHESIS identifier123 RPARENTHESIS;


causalOperator
    : '>>'  // CAUSAL_FWD_STRONG
    | '>'   // CAUSAL_FWD
    | '||>'  // CAUSAL_RESET_FWD_STRONG
    | '|>'  // CAUSAL_RESET_FWD
    | '>|>'  //CAUSAL_FWD_AND_RESET_FWD
    | '<<'   // CAUSAL_BWD_STRONG
    | '<'   // CAUSAL_BWD
    | '<||'  // CAUSAL_RESET_BWD_STRONG
    | '<|'  // CAUSAL_RESET_BWD
    | '<|<' // CAUSAL_BWD_AND_RESET_BWD
    | '<<||>>'        // CAUSAL_RESET_FB_STRONG
    | '<||>'        // CAUSAL_RESET_FB
    | '><|'         // CAUSAL_FWD_AND_RESET_BWD
    | '=>'          // CAUSAL_FWD_AND_RESET_BWD
    | '|><'         // CAUSAL_BWD_AND_RESET_FWD
    ;

CAUSAL_FWD: GT; // '>'
CAUSAL_BWD: LT; // '<'
CAUSAL_RESET_FWD: '|>';
CAUSAL_RESET_BWD: '<|';
CAUSAL_RESET_FB: '<||>';
CAUSAL_FWD_AND_RESET_BWD: '><|' | '=>';
CAUSAL_FWD_AND_RESET_FWD: '>|>' | '|>>';
CAUSAL_BWD_AND_RESET_BWD: '<<|' | '<|<';
CAUSAL_BWD_AND_RESET_FWD: '|><';


// TOKEN
//    : ('0' .. '9' | 'a' .. 'z' | 'A' .. 'Z' | '-' | ' ' | '/' | '_' | ':' | ',')+
//    ;
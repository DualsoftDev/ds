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

model: (system|properties|comment)* EOF;        // importStatement|cpus

system: '[' 'sys' (('ip'|'host') '=' host)? ']' systemName '=' (sysBlock|sysCopySpec);    // [sys] Seg = {..}
    sysBlock
        : LBRACE (flow | interfaces | buttons)* RBRACE       // identifier1Listing|parenting|causal|call
        ;
    host: ipv4 | domainName;
    domainName: identifier1234;
    ipv4: IPV4;
    systemName:identifier1;

//[sys] B = @copy_system(A);
sysCopySpec: '@' 'copy_system' LPARENTHESIS sourceSystemName RPARENTHESIS SEIMCOLON;
    sourceSystemName:identifier1;
layouts: '[' 'layouts' ']' (identifier1)? '=' layoutsBlock;
    layoutsBlock
        : LBRACE (positionDef)* RBRACE
        ;
positionDef: apiPath '=' xywh;
    apiPath: identifier2;
    xywh: LPARENTHESIS x COMMA y (COMMA w COMMA h)? RPARENTHESIS (SEIMCOLON)?;
    x: INTEGER;
    y: INTEGER;
    w: INTEGER;
    h: INTEGER;

addresses: '[' 'addresses' ']' (identifier1)? '=' addressesBlock;
addressesBlock
    : LBRACE (addressDef)* RBRACE
    ;
addressDef: segmentPath '=' address;
    segmentPath: identifier2;
    address: LPARENTHESIS (startTag)? COMMA (endTag)? RPARENTHESIS (SEIMCOLON)?;
    startTag: TAG_ADDRESS;
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
propertyBlock: (addresses|safety|layouts);
    safety: '[' 'safety' ']' EQ LBRACE (safetyDef)* RBRACE;
    safetyDef: safetyKey EQ LBRACE safetyValues RBRACE;
    safetyKey: identifier123;
    safetyValues: identifier123 (SEIMCOLON identifier123)*;


flow
    : '[' 'flow' ']' identifier1 '=' LBRACE (
        causal | parenting | identifier12Listing
        | alias
        | safety)* RBRACE     // |flowTask|callDef
    ;

interfaces
    : '[' 'interfaces' ']' (identifier1)? '=' LBRACE (interfaceListing)* RBRACE;
    interfaceListing: (interfaceDef (';')?) | interfaceResetDef;

    // A23 = { M.U ~ S.S3U ~ _ }
    interfaceDef: interfaceName EQ LBRACE serPhrase RBRACE;
    interfaceName: identifier1;
    serPhrase: callComponents TILDE callComponents (TILDE callComponents)?;
        callComponents: identifier123DNF*;
    //callDefs: (callDef SEIMCOLON)+ ;
    interfaceResetDef: identifier1 (causalOperatorReset identifier1)+ (';')?;


alias: '[' 'aliases' ']' (identifier1)? '=' LBRACE (aliasListing)* RBRACE;
    aliasListing:
        aliasDef '=' LBRACE (aliasMnemonic)? ( ';' aliasMnemonic)* (';')+ RBRACE
        ;
    aliasDef: identifier12;     // {타시스템}.{interface명} or { (my system / flow /) segment 명}
    aliasMnemonic: identifier1;


identifier1Listing: identifier1 SEIMCOLON;     // A;
identifier2Listing: identifier2 SEIMCOLON;     // A;
identifier12Listing: (identifier1Listing | identifier2Listing);
parenting: identifier1 EQ LBRACE (causal|identifier12Listing)* RBRACE;


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
    | '>|>'  //CAUSAL_FWD_AND_RESET_FWD
    | '<<'   // CAUSAL_BWD_STRONG
    | '<'   // CAUSAL_BWD
    | '<|<' // CAUSAL_BWD_AND_RESET_BWD
    | '><|'         // CAUSAL_FWD_AND_RESET_BWD
    | '=>'          // CAUSAL_FWD_AND_RESET_BWD
    | '|><'         // CAUSAL_BWD_AND_RESET_FWD
    | causalOperatorReset
    ;
causalOperatorReset
    : '||>'  // CAUSAL_RESET_FWD_STRONG
    | '|>'  // CAUSAL_RESET_FWD
    | '<||'  // CAUSAL_RESET_BWD_STRONG
    | '<|'  // CAUSAL_RESET_BWD
    | '<<||>>'        // CAUSAL_RESET_FB_STRONG
    | '<||>'        // CAUSAL_RESET_FB
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
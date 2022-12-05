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

parser grammar dsParser;

options { tokenVocab=dsLexer; } // use tokens from dsLexer.g4


// model: (system|)* EOF;        // importStatement|cpus
comment: BLOCK_COMMENT | LINE_COMMENT;

system: '[' sysHeader ']' systemName '=' (sysBlock) EOF;    // [sys] Seg = {..}
    sysHeader: SYS ipSpec?;
    sysBlock
        : LBRACE (  flowBlock | jobBlock | loadDeviceBlock | loadExternalSystemBlock
                    | interfaceBlock | buttonsBlocks | propsBlock
                    | codeBlock
                    | variableBlock | commandBlock | observeBlock )*
          RBRACE       // identifier1Listing|parenting|causal|call
          ;
    systemName:identifier1;

ipSpec: ('ip'|'host') '=' host;
    host: ipv4 | etcName;
    etcName: IDENTIFIER1 | IDENTIFIER2 | IDENTIFIER3 | IDENTIFIER4;  // identifier1234;
    ipv4: IPV4;
//[device file="c:\my.ds"] B;
loadDeviceBlock: '[' 'device' fileSpec ']' deviceName SEIMCOLON;
    deviceName:identifier1;
    fileSpec: 'file' '=' filePath;
        etcName1: IDENTIFIER1;
        filePath: etcName1;

//[external file="c:\my.ds"] B;
loadExternalSystemBlock: '[' EXTERNAL_SYSTEM fileSpec ipSpec ']' externalSystemName SEIMCOLON;
    externalSystemName:identifier1;



// addresses: '[' 'addresses' ']' (identifier12)? '=' addressesBlock;
//     addressesBlock
//         : LBRACE (addressDef)* RBRACE
//         ;
//     addressDef: apiPath '=' addressTxRx;        // A.+ = (%Q1234.2343; %I1234.2343)
        addressTxRx: LPARENTHESIS tx COMMA rx RPARENTHESIS (SEIMCOLON)?;
        tx: addressItem;
        rx: addressItem;
        addressItem: tagAddress | funAddress;
        tagAddress: TAG_ADDRESS;
        funAddress: IDENTIFIER1;



/*
// global safety property
    [prop] = {
        [safety] = {
            F.Main = { F.Ap; F.Am; }
            F.Ap = { F.Main; }
        }
    }
 */
propsBlock: '[' 'prop' ']' EQ LBRACE (safetyBlock|layoutBlock)* RBRACE;
    safetyBlock: '[' 'safety' ']' EQ LBRACE (safetyDef)* RBRACE;
        safetyDef: safetyKey EQ LBRACE safetyValues RBRACE;
            // Real|Call = { ((Real|Call);)* }
            safetyKey: identifier23;
            safetyValues: identifier23 (SEIMCOLON identifier23)* (SEIMCOLON)?;

    layoutBlock: '[' 'layouts' ']' '=' LBRACE (positionDef)* RBRACE;
        positionDef: callName '=' xywh;
            callName: identifier23;
            xywh: LPARENTHESIS x COMMA y (COMMA w COMMA h)? RPARENTHESIS (SEIMCOLON)?;
            x: INTEGER;
            y: INTEGER;
            w: INTEGER;
            h: INTEGER;

flowBlock
    : '[' 'flow' ']' identifier1 '=' LBRACE (
        causal | parentingBlock | identifier12Listing
        | aliasBlock
        // | safetyBlock
        )* RBRACE     // |flowTask|callDef
    ;
    parentingBlock: identifier1 EQ LBRACE (causal|identifier12Listing)* RBRACE;

    identifier12Listing: (identifier1Listing | identifier2Listing);
        identifier1Listing: identifier1 SEIMCOLON;     // A;
        identifier2Listing: identifier2 SEIMCOLON;     // A;



    // [aliases] = { X; Y; Z } = P          // {MyFlowReal} or {Call}
    // [aliases] = { X; Y; Z } = P.Q        // {OtherFlow}.{real}
    aliasBlock: '[' 'aliases' ']' '=' LBRACE (aliasListing)* RBRACE;
        aliasListing:
            aliasDef '=' LBRACE (aliasMnemonic)? ( ';' aliasMnemonic)* (';')+ RBRACE (';')?
            ;
        aliasDef: identifier12;     // {OtherFlow}.{real} or {MyFlowReal} or {Call}
        aliasMnemonic: identifier1;

jobBlock: '[' 'jobs' ']' '=' LBRACE (callListing)* RBRACE;
    callListing:
        jobName '=' LBRACE (callApiDef)? ( ';' callApiDef)* (';')+ RBRACE;
    jobName: etcName1;
    callApiDef: callKey addressTxRx;
    callKey: identifier12;



interfaceBlock
    : '[' 'interfaces' ']' (identifier1)? '=' LBRACE (interfaceListing)* RBRACE;
    interfaceListing: (interfaceDef (';')?) | interfaceResetDef;

    // A23 = { M.U ~ S.S3U ~ _ }
    interfaceDef: interfaceName EQ LBRACE serPhrase RBRACE;
    interfaceName: identifier1;
    serPhrase: callComponents TILDE callComponents (TILDE callComponents)?;
        callComponents: identifier123CNF*;
    //callDefs: (callDef SEIMCOLON)+ ;
    interfaceResetDef: identifier1 (causalOperatorReset identifier1)+ (';')?;



buttonsBlocks:emergencyButtonBlock|autoButtonBlock|startButtonBlock|resetButtonBlock;
    emergencyButtonBlock :'[' ('emg_in'|'emg') ']'     EQ buttonBlock;
    autoButtonBlock      :'[' ('auto_in'|'auto') ']'   EQ buttonBlock;
    startButtonBlock     :'[' ('start_in'|'start') ']' EQ buttonBlock;
    resetButtonBlock     :'[' ('reset_in'|'reset') ']' EQ buttonBlock;
    buttonBlock: LBRACE (() | ((SEIMCOLON)* buttonDef)* (SEIMCOLON)*) RBRACE;
    buttonDef: buttonName EQ LBRACE (() | flowName (SEIMCOLON flowName)* (SEIMCOLON)?) RBRACE;
    buttonName: identifier1;
    flowName : identifier1;



// B.F1 > Set1F <| T.A21;
causal: causalPhrase SEIMCOLON;
    causalPhrase: causalTokensCNF (causalOperator causalTokensCNF)+;
    causalTokensCNF:  causalToken (',' causalToken)* ;
    causalToken: identifier12;

    causalOperator
        : '>'   // CAUSAL_FWD
        | '>>'  // CAUSAL_FWD_STRONG
        | '>|>'    | '|>>'
        | '<<'   // CAUSAL_BWD_STRONG
        | '<'   // CAUSAL_BWD
        | '<<|' | '<|<'
        // | '=>'          // CAUSAL_FWD_AND_RESET_BWD
        | '|><'         // CAUSAL_BWD_AND_RESET_FWD
        | '><|' | '=>'

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

identifier1234: (identifier1 | identifier2 | identifier3 | identifier4);
    identifier1: IDENTIFIER1;
    identifier2: IDENTIFIER2;
    identifier3: IDENTIFIER3;
    identifier4: IDENTIFIER4;

    identifier12: (identifier1 | identifier2);
    identifier23: (identifier2 | identifier3);
    identifier123: (identifier1 | identifier2 | identifier3);

    identifier123CNF: identifier123 (COMMA identifier123)*;

    flowPath: identifier2;

codeBlock: CODE_BLOCK;

variableBlock: '[' 'variables' ']' '=' '{' variableDef* '}';
    variableDef: varName '=' '(' varType ',' argumentGroups ')';     // R100   = (Word, 0)
    varName: IDENTIFIER1;
    argumentGroups: argumentGroup ('~' argumentGroup)*;
    argumentGroup: argument (',' argument)*;
    argument: intValue | floatValue | varIdentifier;
    varIdentifier: IDENTIFIER1;
    intValue: INTEGER;
    floatValue:FLOAT;
    varType: 'int' | 'word' | 'float' | 'dword';


funApplication: funName '=' argumentGroups;

commandBlock: '[' 'commands' ']' '=' '{' commandDef* '}';
    commandDef: cmdName '=' '(' '@' funApplication ')';     // CMD1 = (@Delay= 0)
    cmdName: IDENTIFIER1;
    funName:IDENTIFIER1;
observeBlock: '[' 'observes' ']' '=' '{' observeDef* '}';
    observeDef: observeName '=' '(' '@' funApplication ')';     // CMD1 = (@Delay= 0)
    observeName:IDENTIFIER1;
    //funName:IDENTIFIER1;

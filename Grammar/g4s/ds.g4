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

SYS: ('sys'|'system');
EXTERNAL_SYSTEM: 'external';
IP: 'ip';
HOST: 'host';
FILE: 'file';
DEVICE: 'device';
COPY_SYSTEM: 'copy_system';
LAYOUTS: 'layouts';
ADDRESSES: 'addresses';
PROP: 'prop';
SAFETY: 'safety';
FLOW: 'flow';
INTERFACES: 'interfaces';
ALIASES: 'aliases';
JOBS: 'jobs';
BUTTONS: 'buttons';
LAMPS: 'lamps';
CONDITIONS: 'conditions';
E_IN: 'e_in';
E: 'e';
A_IN: 'a_in';
A: 'a';
D_IN: 'd_in';
D: 'd';
C_IN: 'c_in';
C: 'c';
M_IN: 'm_in';
M: 'm';
S_IN: 's_in';
S: 's';
T_IN: 't_in';
T: 't';
H_IN: 'h_in';
H: 'h';
R_IN: 'r_in';
R: 'r';
I_IN: 'i_in';
I: 'i';
VARIABLES: 'variables';
COMMANDS: 'commands';
OBSERVES: 'observes';
WORDTYPE: 'word';
DWORDTYPE: 'dword';
INTTYPE: 'int';
FLOATTYPE: 'float';
FUNC: 'func';


WS: [ \t\r\n]+ -> skip;
BLOCK_COMMENT : '/*' (BLOCK_COMMENT|.)*? '*/' -> channel(HIDDEN) ;
LINE_COMMENT  : '//' .*? ('\n'|EOF) -> channel(HIDDEN) ;

fragment CODE_BLOCK_START: '<@{';
fragment CODE_BLOCK_END: '}@>';
CODE_BLOCK: CODE_BLOCK_START (BLOCK_COMMENT|LINE_COMMENT|CODE_BLOCK|.)*? CODE_BLOCK_END;

fragment Identifier: ValidIdStart ValidIdChar*;
   // lexical rule for hangul characters
    fragment HangulChar: [\u3131-\u3163|\uAC00-\uD7A3]+;

    fragment ValidIdStart
    : ('a' .. 'z') | ('A' .. 'Z') | '_' | HangulChar
    ;

    fragment ValidIdChar
    : ValidIdStart | ('0' .. '9') | HangulChar
    ;

fragment QuotedStringLiteral : '"' (~('"' | '\\' | '\r' | '\n') | '\\' ('"' | '\\'))* '"';

fragment Compo: Identifier|QuotedStringLiteral;

IDENTIFIER1: Compo;
IDENTIFIER2: Compo '.' Compo;
IDENTIFIER3: Compo '.' Compo '.' Compo;
IDENTIFIER4: Compo '.' Compo '.' Compo '.' Compo;

IPV4: [1-9][0-9]*'.'('0'|[1-9][0-9]*)'.'('0'|[1-9][0-9]*)'.'('0'|[1-9][0-9]*);
// IPV4: (INTEGER)(DOT) INTEGER DOT INTEGER DOT INTEGER;

TAG_ADDRESS: ValidTagStart ValidTagChar*;
   fragment ValidTagStart
      : '%' | ('a' .. 'z') | ('A' .. 'Z') // '%' | '_' | HANGUL_CHAR
      ;
   fragment ValidTagChar
      : ('a' .. 'z') | ('A' .. 'Z') | ('a' .. 'z')('a' .. 'z') | ('A' .. 'Z')('A' .. 'Z') | 
        ('0' .. '9')(('0' .. '9'))* DOT | ('0' .. '9')
      ;

SQUOTE: '\'';
DQUOTE: '"';
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
EXCLAMATION: '!';
OR: '|';
AT: '@';
FNC: '$';
POUND: '#';
PLUS: '+';
MINUS: '-';
MUL: '*';
DIV: '/';
MOD: '%';

GTE: '>=';
LTE: '<=';

OR2: '||';
EQ2: '==';
NEQ: '!=';
//NEWLINE: '\r'? '\n';

INTEGER: '0'|[1-9][0-9]*;
FLOAT: [1-9][0-9]*('.'[0-9]+)?;

// Close Angle Bracket
Cab: '>';
CabCab: '>>';

// Open Angle Bracket
Oab: '<';
OabOab: '<<';

PipeCab: '|>';
CabPipe: '<|';
OabPipePipeCab: '<||>';
CabOabPipe: '><|';
EqualCab: '=>';
CabPipeCab: '>|>';
PipeCabCab: '|>>';
OabOabPipe: '<<|';
OabPipeOab: '<|<';
PipeCabOab: '|><';
PipePipeOab: '||>';
CabPipePipe: '<||';
OabOabPipePipeCabCab: '<<||>>';

QUESTION: '?';

//--------------------------------------------------------------------------------

identifier1234: (identifier1 | identifier2 | identifier3 | identifier4);
    identifier1: IDENTIFIER1 | lexerTokenIdentifierCandidate;
    identifier2: IDENTIFIER2;
    identifier3: IDENTIFIER3;
    identifier4: IDENTIFIER4;

    identifier12: (identifier1 | identifier2);
    identifier23: (identifier2 | identifier3);
    identifier123: (identifier1 | identifier2 | identifier3);

    identifier123CNF: identifier123 (COMMA identifier123)*;

    flowPath: identifier2;

addressInOut: LPARENTHESIS inAddr COMMA outAddr RPARENTHESIS (SEIMCOLON)?;
inAddr: addressItem;
outAddr: addressItem;
addressItem: tagAddress | funAddress;
tagAddress: TAG_ADDRESS;
funAddress: IDENTIFIER1;

// model: (system|)* EOF;        // importStatement|cpus
comment: BLOCK_COMMENT | LINE_COMMENT;

system: '[' sysHeader ']' systemName '=' (sysBlock) EOF;    // [sys] Seg = {..}
    sysHeader: SYS ipSpec?;
    sysBlock
        : LBRACE (  flowBlock | jobBlock | loadDeviceBlock | loadExternalSystemBlock
                    | interfaceBlock | buttonBlock | lampBlock | conditionBlock | propsBlock
                    | codeBlock | variableBlock )*
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


/*
// global safety property
    [prop] = {
        [safety] = {
            F.Main = { F.Ap; F.Am; }
            F.Ap = { F.Main; }
        }
    }
 */
propsBlock: '[' 'prop' ']' EQ LBRACE (safetyBlock|layoutBlock|finishBlock|disableBlock)* RBRACE;
    safetyBlock: '[' 'safety' ']' EQ LBRACE (safetyDef)* RBRACE;
        safetyDef: safetyKey EQ LBRACE safetyValues RBRACE;
            // Real|CallDev = { ((Real|CallDev);)* }
            safetyKey: identifier23;
            safetyValues: identifier23 (SEIMCOLON identifier23)* (SEIMCOLON)?;

    layoutBlock: '[' 'layouts' ']' '=' LBRACE (positionDef)* RBRACE;
        positionDef: deviceOrApiName '=' xywh;
            deviceOrApiName: identifier12;
            xywh: LPARENTHESIS x COMMA y (COMMA w COMMA h)? RPARENTHESIS (SEIMCOLON)?;
            x: INTEGER;
            y: INTEGER;
            w: INTEGER;
            h: INTEGER;
    finishBlock: '[' 'finish' ']' '=' LBRACE (finishListing)* RBRACE;
        finishTarget: identifier2;
        finishListing: finishTarget (SEIMCOLON finishTarget)* (SEIMCOLON)?;
    disableBlock: '[' 'disable' ']' '=' LBRACE (disableListing)* RBRACE;
        disableTarget: identifier23;
        disableListing: disableTarget (SEIMCOLON disableTarget)* (SEIMCOLON)?;

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

    // [aliases] = { X; Y; Z } = P          // {MyFlowReal} or {CallDev}
    // [aliases] = { X; Y; Z } = P.Q        // {OtherFlow}.{real}
    aliasBlock: '[' 'aliases' ']' '=' LBRACE (aliasListing)* RBRACE;
        aliasListing:
            aliasDef '=' LBRACE (aliasMnemonic)? ( ';' aliasMnemonic)* (';')+ RBRACE (';')?
            ;
        aliasDef: identifier12;     // {OtherFlow}.{real} or {MyFlowReal} or {CallDev}
        aliasMnemonic: identifier1;

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

jobBlock: '[' 'jobs' ']' '=' LBRACE (callListing|linkListing|funcSet)* RBRACE;
    callListing:
        jobName '=' LBRACE (callApiDef)? ( ';' callApiDef)* (';')+ RBRACE;
    linkListing:
        jobName '=' interfaceLink SEIMCOLON;
    jobName: etcName1;
    callApiDef: interfaceCall addressInOut;
    interfaceCall: identifier12;
    interfaceLink: identifier12;

    funcSet: identifier12 '=' LBRACE (() | funcDef (SEIMCOLON funcDef)* (SEIMCOLON)?) RBRACE;
    funcDef:  '$' funcName (argument (argument)*)?;
    funcName: identifier1;

codeBlock: CODE_BLOCK;

interfaceBlock
    : '[' 'interfaces' ']' '=' LBRACE (interfaceListing)* RBRACE;
    interfaceListing: (interfaceDef (';')?) | interfaceResetDef;

    // A23 = { M.U ~ S.S3U ~ _ }
    interfaceDef: interfaceName EQ LBRACE (serPhrase|linkPhrase) RBRACE;
    interfaceName: identifier1;
    serPhrase: callComponents TILDE callComponents (TILDE callComponents)?;
        callComponents: identifier123CNF*;
    //callDefs: (callDef SEIMCOLON)+ ;
    linkPhrase: identifier12;
    interfaceResetDef: identifier1 (causalOperatorReset identifier1)+ (';')?;

categoryBlocks:autoBlock|manualBlock|driveBlock|clearBlock|stopBlock|emergencyBlock|testBlock|homeBlock|readyBlock|idleBlock;
    autoBlock      :'[' ('a_in'|'a') ']' EQ categoryBlock;
    manualBlock    :'[' ('m_in'|'m') ']' EQ categoryBlock;
    driveBlock     :'[' ('d_in'|'d') ']' EQ categoryBlock;
    stopBlock      :'[' ('s_in'|'s') ']' EQ categoryBlock;
    clearBlock     :'[' ('c_in'|'c') ']' EQ categoryBlock;
    emergencyBlock :'[' ('e_in'|'e') ']' EQ categoryBlock;
    testBlock      :'[' ('t_in'|'t') ']' EQ categoryBlock;
    homeBlock      :'[' ('h_in'|'h') ']' EQ categoryBlock;
    readyBlock     :'[' ('r_in'|'r') ']' EQ categoryBlock;
    idleBlock      :'[' ('i_in'|'i') ']' EQ categoryBlock;

    categoryBlock: LBRACE (() | (buttonDef|lampDef|funcSet)*) RBRACE;

    buttonDef: btnNameAddr EQ LBRACE (() | flowName (SEIMCOLON flowName)* (SEIMCOLON)?) RBRACE;
    btnNameAddr: buttonName addressInOut;

    buttonName: identifier12;

    lampDef: (lampName|lampName addrDef) EQ LBRACE (() | flowName (SEIMCOLON flowName)* (SEIMCOLON)?) RBRACE;
    addrDef: LPARENTHESIS addressItem? RPARENTHESIS;
    lampName: identifier12;

    flowName: identifier1;

buttonBlock: '[' 'buttons' ']' '=' LBRACE (categoryBlocks)* RBRACE;
lampBlock: '[' 'lamps' ']' '=' LBRACE (categoryBlocks)* RBRACE;
conditionBlock: '[' 'conditions' ']' '=' LBRACE (categoryBlocks)* RBRACE;

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

    lexerTokenIdentifierCandidate:
        SYS | EXTERNAL_SYSTEM | IP | HOST
        | FILE | DEVICE | COPY_SYSTEM
        | LAYOUTS | ADDRESSES | PROP | SAFETY | FLOW
        | INTERFACES | ALIASES | VARIABLES
        | JOBS | BUTTONS | LAMPS | CONDITIONS
        | E_IN | E | A_IN | A | D_IN | D
        | C_IN | C | M_IN | M | S_IN | S
        | T_IN | T | H_IN | H | R_IN | R
        | WORDTYPE | DWORDTYPE | INTTYPE | FLOATTYPE
        | FUNC
        ;
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
O_IN: 'o_in';
O: 'o';
P_IN: 'p_in';
P: 'p';
VARIABLES: 'variables';
OBSERVES: 'observes';
WORDTYPE: 'word';
DWORDTYPE: 'dword';
INTTYPE: 'int';
FLOATTYPE: 'float';
COMMANDS: 'commands';
OPERATORS : 'operators';


WS: [ \t\r\n]+ -> skip;
BLOCK_COMMENT : '/*' (BLOCK_COMMENT|.)*? '*/' -> channel(HIDDEN) ;
LINE_COMMENT  : '//' .*? ('\n'|EOF) -> channel(HIDDEN) ;

fragment CODE_BLOCK_START: '<@{';
fragment CODE_BLOCK_END: '}@>';
CODE_BLOCK: CODE_BLOCK_START (BLOCK_COMMENT|LINE_COMMENT|CODE_BLOCK|.)*? CODE_BLOCK_END;

fragment Identifier: ValidIdStart ValidIdChar*;
   // lexical rule for hangul characters
    fragment HangulChar: [\u3131-\u314E\u314F-\u3163|\uAC00-\uD7A3]+;

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
      : '%' | ('a' .. 'z') | ('A' .. 'Z') // '%' | '_' | HangulChar
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
SEMICOLON: ';';
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
OabPipePipeCab: '<|>';
CabOabPipe: '><|';
EqualCab: '=>';
CabPipeCab: '>|>';
PipeCabCab: '|>>';
OabOabPipe: '<<|';
OabPipeOab: '<|<';
PipeCabOab: '|><';
PipePipeOab: '||>';
CabPipePipe: '<||';
OabOabPipePipeCabCab: '<||>';

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

    identifier1Func: '$'identifier1;
    identifier123CNF: identifier123 (COMMA identifier123)*;

    flowPath: identifier2;

addressInOut: LPARENTHESIS inAddr COMMA outAddr RPARENTHESIS (SEMICOLON)?;
inAddr: addressItem;
outAddr: addressItem;
addressItem: tagAddress | funAddress | '-';
tagAddress: TAG_ADDRESS;
funAddress: IDENTIFIER1;

// model: (system|)* EOF;        // importStatement|cpus
comment: BLOCK_COMMENT | LINE_COMMENT;

system: '[' SYS ']' systemName '=' (sysBlock) EOF;    // [sys] Seg = {..}
    sysBlock
        : LBRACE (  flowBlock | jobBlock | commandBlock | operatorBlock | loadDeviceBlock | loadExternalSystemBlock
                    | interfaceBlock | buttonBlock | lampBlock | conditionBlock | propsBlock
                    | codeBlock | variableBlock )*
          RBRACE       // identifier1Listing|parenting|causal|call
          (SEMICOLON)?;
    systemName:identifier1;


//[device file="c:\my.ds"] A, B, C;
loadDeviceBlock: '[' 'device' fileSpec ']' deviceNameList SEMICOLON;
    deviceNameList: deviceName (COMMA deviceName)*;
    deviceName:identifier1;
    fileSpec: 'file' '=' filePath;
        etcName1: IDENTIFIER1;
        filePath: etcName1;

//[external file="c:\my.ds"] B;
loadExternalSystemBlock: '[' EXTERNAL_SYSTEM fileSpec ']' externalSystemName SEMICOLON;
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
            // Real|Call = { ((Real|Call);)* }
            safetyKey: identifier23;
            safetyValues: identifier23 (SEMICOLON identifier23)* (SEMICOLON)?;

    layoutBlock: '[' 'layouts' fileSpec? ']' '=' LBRACE (positionDef)* RBRACE;
        positionDef: deviceOrApiName '=' xywh;
            deviceOrApiName: identifier12;
            xywh: LPARENTHESIS x COMMA y (COMMA w COMMA h)? RPARENTHESIS (SEMICOLON)?;
            x: INTEGER;
            y: INTEGER;
            w: INTEGER;
            h: INTEGER;
    finishBlock: '[' 'finish' ']' '=' LBRACE (finishListing)* RBRACE;
        finishTarget: identifier2;
        finishListing: finishTarget (SEMICOLON finishTarget)* (SEMICOLON)?;
    disableBlock: '[' 'disable' ']' '=' LBRACE (disableListing)* RBRACE;
        disableTarget: identifier23;
        disableListing: disableTarget (SEMICOLON disableTarget)* (SEMICOLON)?;

flowBlock
    : '[' 'flow' ']' identifier1 '=' LBRACE (
        causal | parentingBlock | identifier1Listing | identifier1sListing 
        | identifier1Func | identifier1Funcs
        | aliasBlock
        // | safetyBlock
        )* RBRACE  (SEMICOLON)?   // |flowTask|callDef
    ;
    parentingBlock: identifier1 EQ LBRACE (identifier1sListing | causal)* RBRACE;
    identifier1Funcs: (identifier1Func (COMMA identifier1Func)*)?  SEMICOLON; 
    identifier1Listing: identifier1  SEMICOLON;     // A;
    identifier1sListing: (identifier1 (COMMA identifier1)*)?  SEMICOLON;     // A, B, C;
        
    // [aliases] = { X; Y; Z } = P          // {MyFlowReal} or {Call}
    // [aliases] = { X; Y; Z } = P.Q        // {OtherFlow}.{real}
    aliasBlock: '[' 'aliases' ']' '=' LBRACE (aliasListing)* RBRACE;
        aliasListing:
            aliasDef '=' LBRACE (aliasMnemonic)? ( ';' aliasMnemonic)* (';')+ RBRACE (';')?
            ;
        aliasDef: identifier12;     // {OtherFlow}.{real} or {MyFlowReal} or {Call}
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
    varType: IDENTIFIER1;




functionNameOnly: identifier1 SEMICOLON;
operatorBlock: '[' 'operators' ']' EQ LBRACE (functionDef|functionNameOnly)+ RBRACE;
    functionDef: functionName EQ functionOperator SEMICOLON;
    functionName: identifier1;
    functionOperator: functionType (argument (argument)*)?;
    functionType: identifier1;


commandBlock: '[' 'commands' ']' '=' '{' functionNameOnly* | functionCommandDef* '}' ;
functionCommandDef :  functionName '=' functionCommand;

functionCommand : '{' functionCommandCode '}'; 
functionCommandCode :  .*? ; //  any character between braces

jobBlock: '[' 'jobs' ']' '=' LBRACE (callListing|linkListing)* RBRACE;
    callListing:
        jobName '=' LBRACE (callApiDef|funcCall)? ( ';' callApiDef|funcCall)* (';')+ RBRACE (SEMICOLON)?;
    linkListing:
        jobName '=' interfaceLink SEMICOLON;
    jobName: etcName1;
    callApiDef: (interfaceCall addressInOut|interfaceCall);

    interfaceCall: identifier12;
    interfaceLink: identifier12;


funcCall: identifier1Func;



codeBlock: CODE_BLOCK;

interfaceBlock
    : '[' 'interfaces' ']' '=' LBRACE (interfaceListing)* RBRACE;
    interfaceListing: (interfaceDef (';')?) | interfaceResetDef;

    // A23 = { M.U ~ S.S3U ~ _ }
    interfaceDef: interfaceName EQ LBRACE (serPhrase|linkPhrase) RBRACE;
    interfaceName: identifier1;
    serPhrase: callComponents TILDE callComponents (TILDE callComponents)?;
        callComponents: identifier123CNF*;
    //callDefs: (callDef SEMICOLON)+ ;
    linkPhrase: identifier12;
    interfaceResetDef: identifier1 (causalOperatorReset identifier1)+ (';')?;

categoryBlocks:autoBlock|manualBlock|driveBlock|clearBlock|pauseBlock|errorOrEmgBlock|testBlock|homeBlock|readyBlock|idleBlock|originBlock;
    autoBlock      :'[' ('a_in'|'a') ']' EQ categoryBlock;
    manualBlock    :'[' ('m_in'|'m') ']' EQ categoryBlock;
    driveBlock     :'[' ('d_in'|'d') ']' EQ categoryBlock;
    errorOrEmgBlock:'[' ('e_in'|'e') ']' EQ categoryBlock;
    pauseBlock     :'[' ('p_in'|'p') ']' EQ categoryBlock;
    clearBlock     :'[' ('c_in'|'c') ']' EQ categoryBlock;
    testBlock      :'[' ('t_in'|'t') ']' EQ categoryBlock;
    homeBlock      :'[' ('h_in'|'h') ']' EQ categoryBlock;
    readyBlock     :'[' ('r_in'|'r') ']' EQ categoryBlock;
    idleBlock      :'[' ('i_in'|'i') ']' EQ categoryBlock;
    originBlock    :'[' ('o_in'|'o') ']' EQ categoryBlock;
    
    categoryBlock: LBRACE (() | (hwSysItemDef)*) RBRACE;
  
    hwSysItemDef:  hwSysItemNameAddr '=' LBRACE hwSysItems? RBRACE (SEMICOLON)?;
    hwSysItems: (flowName|funcCall)? ( ';' flowName|funcCall)* (';')+; 
    hwSysItemNameAddr: hwSysItemName addressInOut;
    hwSysItemName: identifier12;

    flowName: identifier1;

buttonBlock: '[' 'buttons' ']' '=' LBRACE (categoryBlocks)* RBRACE;
lampBlock: '[' 'lamps' ']' '=' LBRACE (categoryBlocks)* RBRACE;
conditionBlock: '[' 'conditions' ']' '=' LBRACE (categoryBlocks)* RBRACE;

// B.F1 > Set1F <| T.A21;
causal: causalPhrase SEMICOLON;
    causalPhrase: causalTokensCNF (causalOperator causalTokensCNF)+;
    causalTokensCNF: causalToken (',' causalToken)* ;
    causalToken: identifier12|identifier1Func;

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
        | '<||>'        // CAUSAL_RESET_FB_STRONG
        | '<|>'        // CAUSAL_RESET_FB
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
        | COMMANDS | OPERATORS
        ;
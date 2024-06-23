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

fragment CODE_BLOCK_START: '#{';
fragment CODE_BLOCK_END: '}';
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



SQUOTE: '\'';
DQUOTE: '"';
LBRACKET: '[';
RBRACKET: ']';
LBRACE: '{';
RBRACE: '}';
LPARENTHESIS: '(';
RPARENTHESIS: ')';
EQ: '==';
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
EqualPipeCab: '=|>';
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
    identifier123CNF: identifier123 (COMMA identifier123)*;

    identifierCommandName : IDENTIFIER1 ;
    identifierCommandPara : IDENTIFIER1 ;
    identifierOperatorName : IDENTIFIER1 ;

    identifierOperator: '#'identifierOperatorName;
    identifierCommand: identifierCommandName '(' identifierCommandPara? ')';

    flowPath: identifier2;

devParamInOut: '(' devParamInOutBody ')' (SEMICOLON)?;
devParamInOutBody:  inParam COMMA outParam;
content : .+?;
inParam:  content (':'content)*;
outParam:  content (':'content)*;

// model: (system|)* EOF;        // importStatement|cpus
comment: BLOCK_COMMENT | LINE_COMMENT;

system: '[' SYS ']' systemName '=' (sysBlock) EOF;    // [sys] Seg = {..}
    sysBlock
        : '{' (  flowBlock | jobBlock | commandBlock | operatorBlock | loadDeviceBlock | loadExternalSystemBlock
                    | interfaceBlock | buttonBlock | lampBlock | conditionBlock | propsBlock
                    | variableBlock )*
          '}'       // identifier1Listing|parenting|causal|call
          (SEMICOLON)?;
    systemName:identifier1;


//[device file="c:\my.ds"] A, B, C;
loadDeviceBlock: '[' 'device' fileSpec? ']' deviceNameList SEMICOLON;
    deviceNameList: deviceName (COMMA deviceName)*;
    deviceName:identifier1;
    fileSpec: 'file' '=' filePath;
        etcName1: IDENTIFIER1;
        filePath: etcName1;

//[external file="c:\my.ds"] B;
loadExternalSystemBlock: '[' EXTERNAL_SYSTEM fileSpec ']' externalSystemName SEMICOLON;
    externalSystemName:identifier1;


/*
// global  property
    [prop] = {
        [safety] = {
            F.Main = { F.Ap; F.Am; }
            F.Ap = { F.Main; }
        }

        [times] = {
            F.WorkA = {M:1000, S:200, D:30}; // average, std, onDelay msec
            F.WorkB = {M:1000, S:200, D:30}; // average, std, onDelay msec
        }
        [actions] = {
            F.WorkA  = {./Assets/dsLib/Cylinder/Robot.fbx:Load};
            F.WorkB  = {./Assets/dsLib/Cylinder/Robot.obj:Unload};
            F.WorkC  = {./Assets/dsLib/Cylinder/Robot.obj:Home};
            F.Work1  = {./Assets/dsLib/Cylinder/DoubleType.obj:ADV};
            F.Work2  = {./Assets/dsLib/Cylinder/DoubleType.obj:RET};
        }
        [scripts] = {                                         
            F.WorkA = {ThirdParty.AddressInfo.Provider.testFunc1()}; // 
            F.WorkB = {ThirdParty.AddressInfo.Provider.testFunc2()}; // 
        }
    }

 */
propsBlock: '[' 'prop' ']' '=' '{' (safetyBlock|layoutBlock|finishBlock|disableBlock|notransBlock|timesBlock|actionsBlock|scriptsBlock)* '}';
    safetyBlock: '[' 'safety' ']' '=' '{' (safetyDef)* '}';    
        safetyDef: safetyKey '=' '{' safetyValues '}';
            // Real|Call = { ((Real|Call);)* }
            safetyKey: identifier23;
            safetyValues: identifier23 (SEMICOLON identifier23)* (SEMICOLON)?;

    layoutBlock: '[' 'layouts' fileSpec? ']' '=' '{' (positionDef)* '}';
        positionDef: deviceOrApiName '=' xywh;
            deviceOrApiName: identifier12;
            xywh: LPARENTHESIS x COMMA y (COMMA w COMMA h)? RPARENTHESIS (SEMICOLON)?;
            x: INTEGER;
            y: INTEGER;
            w: INTEGER;
            h: INTEGER;
    finishBlock: '[' 'finish' ']' '=' '{' (finishListing)* '}';
        finishTarget: identifier2;
        finishListing: finishTarget (SEMICOLON finishTarget)* (SEMICOLON)?;
    notransBlock: '[' 'notrans' ']' '=' '{' (notransListing)* '}';
        notransTarget: identifier2;
        notransListing: notransTarget (SEMICOLON notransTarget)* (SEMICOLON)?;
    disableBlock: '[' 'disable' ']' '=' '{' (disableListing)* '}';
        disableTarget: identifier23;
        disableListing: disableTarget (SEMICOLON disableTarget)* (SEMICOLON)?;

    timesBlock: '[' 'times' ']' '=' '{' (timeDef)* '}';
    timeDef: timeKey '{' timeParams '}' SEMICOLON;
    timeKey: identifier23;
    timeParams: timeParamList;
    timeParamList: (timeParam (COMMA timeParam)*)?;
    timeParam: ('M' ':' INTEGER | 'S' ':' INTEGER | 'D' ':' INTEGER); // average, std, onDelay msec

    actionsBlock: '[' 'actions' ']' '=' '{' (actionDef)* '}';
    actionDef: actionKey '{' actionParams '}' SEMICOLON;
    actionKey: identifier23;
    actionParams: content ':' identifier1;

    scriptsBlock: '[' 'scripts' ']' '=' '{' (scriptDef)* '}';
    scriptDef: scriptKey '{' scriptParams '}' SEMICOLON;
    scriptKey: identifier23;
    scriptParams: content;


flowBlock
    : '[' 'flow' ']' identifier1 '=' '{' (
        causal | parentingBlock 
        | nonCausal | nonCausals 
        | aliasBlock
        )* '}'  (SEMICOLON)?   // |flowTask|callDef
    ;
    parentingBlock: identifier1 '=' '{' (causal | nonCausal | nonCausals)* '}';
    nonCausal : (identifier1 | identifier12 | identifierCommand);
    nonCausals: (nonCausal (COMMA nonCausal)*)?  SEMICOLON;     // A, B(), C;
        
    // [aliases] = { X; Y; Z } = P          // {MyFlowReal} or {Call}
    // [aliases] = { X; Y; Z } = P.Q        // {OtherFlow}.{real}
    aliasBlock: '[' 'aliases' ']' '=' '{' (aliasListing)* '}';
        aliasListing:
            aliasDef '=' '{' (aliasMnemonic)? ( ';' aliasMnemonic)* (';')+ '}' (';')?
            ;
        aliasDef: identifier12;     // {OtherFlow}.{real} or {MyFlowReal} or {Call}
        aliasMnemonic: identifier1;

codeBlock: CODE_BLOCK;

varType: identifier1;

    // identifier1에서 varType 이름과 같게 사용하면 예외처리 필요  ex) [sys] Single = {..} <- 예외
   //    'int8' | 'sbyte' | 'Int8' | 'Sbyte'
   //    | 'uint8' | 'byte' | 'UInt8' | 'Byte'
   //    | 'int16' | 'short' | 'word' | 'Int16' | 'Short' | 'Word' |     
   //    | 'uint16'| 'ushort'| 'UInt16'| 'Ushort'
   //    | 'int32' | 'int'   | 'dword' | 'Int32' | 'Int'   | 'Dword'
   //    | 'uint32'| 'uint' |    'UInt32'| 'Uint' 
   //    | 'int64' | 'long' | 'Int64' | 'Long'
   //    | 'uint64'| 'ulong' | 'UInt64'| 'Ulong'
   //    | 'double' | 'float64' | 'Double' | 'Float64'
   //    | 'single' | 'float32' | 'Single' | 'Float32'
   //    | 'char' | 'Char'
   //    | 'string' | 'String'
   //    | 'bool' | 'boolean'| 'Bool'   | 'Boolean' ;



variableBlock: '[' 'variables' ']' '=' '{' (constDef | variableDef)* '}';
    variableDef: varType (varName | varName '=' initValue) SEMICOLON;
    constDef: 'const' varType (constName | constName '=' initValue) SEMICOLON;
    varName: identifier1;
    constName: identifier1;
    initValue: content;

operatorBlock: '[' 'operators' ']' '=' '{' (operatorNameOnly | operatorDef)* '}' ;
    operatorNameOnly: operatorName SEMICOLON;
    operatorDef :  operatorName '=' operator;
    operatorName: identifier1;
    operator : codeBlock;
    
commandBlock:  '[' 'commands' ']'  '=' '{' (commandNameOnly | commandDef)* '}' ;
    commandNameOnly: commandName SEMICOLON;
    commandDef :  commandName '=' command;
    commandName: identifier1;
    command : codeBlock;
    


jobBlock: '[' 'jobs' ']' '=' '{' (callListing)* '}';
    callListing:
        jobName ('('jobTypeOption')')? '=' '{' (callApiDef ';')*'}' (SEMICOLON)?;

    jobName: identifier1;
    jobTypeOption : identifier1;

    callApiDef: (interfaceCall devParamInOut | interfaceCall);

    interfaceCall: identifier12;



funcCall: identifierOperator | identifierCommand;




interfaceBlock
    : '[' 'interfaces' ']' '=' '{'  (interfaceListing)* '}';
    interfaceListing: (interfaceDef (';')?) | interfaceResetDef;

    // A23 = { M.U ~ S.S3U ~ _ }
    interfaceDef: interfaceName '=' '{' (serPhrase|linkPhrase) '}';
    interfaceName: identifier1;
    serPhrase: callComponents TILDE callComponents (TILDE callComponents)?;
        callComponents: identifier123CNF*;
    //callDefs: (callDef SEMICOLON)+ ;
    linkPhrase: identifier12;
    interfaceResetDef: identifier1 (causalOperatorReset identifier1)+ (';')?;

categoryBlocks:autoBlock|manualBlock|driveBlock|clearBlock|pauseBlock|errorOrEmgBlock|testBlock|homeBlock|readyBlock|idleBlock|originBlock;
    autoBlock      :'[' ('a_in'|'a') ']' '=' categoryBlock;
    manualBlock    :'[' ('m_in'|'m') ']' '=' categoryBlock;
    driveBlock     :'[' ('d_in'|'d') ']' '=' categoryBlock;
    errorOrEmgBlock:'[' ('e_in'|'e') ']' '=' categoryBlock;
    pauseBlock     :'[' ('p_in'|'p') ']' '=' categoryBlock;
    clearBlock     :'[' ('c_in'|'c') ']' '=' categoryBlock;
    testBlock      :'[' ('t_in'|'t') ']' '=' categoryBlock;
    homeBlock      :'[' ('h_in'|'h') ']' '=' categoryBlock;
    readyBlock     :'[' ('r_in'|'r') ']' '=' categoryBlock;
    idleBlock      :'[' ('i_in'|'i') ']' '=' categoryBlock;
    originBlock    :'[' ('o_in'|'o') ']' '=' categoryBlock;
    
    categoryBlock: '{' (() | (hwSysItemDef)*) '}';
  

    hwSysItemDef:  hwSysItemNameAddr '=' '{' hwSysItems? '}' (SEMICOLON)?;
    hwSysItems: flowName? ( ';' flowName)* (';')+; 
    hwSysItemNameAddr: hwSysItemName devParamInOut;
    hwSysItemName: identifier1;
    flowName: identifier1;

buttonBlock: '[' 'buttons' ']' '=' '{' (categoryBlocks)* '}';
lampBlock: '[' 'lamps' ']' '=' '{' (categoryBlocks)* '}';
conditionBlock: '[' 'conditions' ']' '=' '{' (categoryBlocks)* '}';

// B.F1 > Set1F > T.A21;
causal: causalPhrase SEMICOLON;
    causalPhrase: causalTokensCNF (causalOperator causalTokensCNF)+;
    causalTokensCNF: causalToken (',' causalToken)* ;
    causalToken: identifier12 | identifierOperator | identifierCommand;

    causalOperator
        : '>'   // CAUSAL_FWD
        | '>>'  // CAUSAL_FWD_STRONG
        | '>|>'    | '|>>'
        | '<<'   // CAUSAL_BWD_STRONG
        | '<'   // CAUSAL_BWD
        | '<<|' | '<|<'
        | '|><'         // CAUSAL_BWD_AND_RESET_FWD
        | '><|' | '=>'| '=|>'
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
        | LAYOUTS | ADDRESSES | PROP | FLOW
        | INTERFACES | ALIASES | VARIABLES
        | JOBS | BUTTONS | LAMPS | CONDITIONS
        | E_IN | E | A_IN | A | D_IN | D
        | C_IN | C | M_IN | M | S_IN | S
        | T_IN | T | H_IN | H | R_IN | R
        | WORDTYPE | DWORDTYPE | INTTYPE | FLOATTYPE
        | COMMANDS | OPERATORS
        ;
namespace Dsu.PLCConverter.FS.XgiSpecs
open System



/// 래더에서 타입 체크할때 사용하기 위한 타입
// RecordType.h : LS 산전에서 전달받은 header file
// #define BOOL_CHECKTYPE			0x00000001
[<Flags>]
type CheckType =
    | NONE          = 0x00000000  // dual에서 임시 추가
    | BOOL          = 0x00000001
    | BYTE          = 0x00000002
    | WORD          = 0x00000004
    | DWORD         = 0x00000008
    | LWORD         = 0x00000010
    | SINT          = 0x00000020
    | INT           = 0x00000040
    | DINT          = 0x00000080
    | LINT          = 0x00000100
    | USINT         = 0x00000200        
    | UINT          = 0x00000400
    | UDINT         = 0x00000800
    | ULINT         = 0x00001000
    | REAL          = 0x00002000
    | LREAL         = 0x00004000
    | TIME          = 0x00008000
    | DATE          = 0x00010000        
    | TOD           = 0x00020000
    | DT            = 0x00040000
    | STRING        = 0x00080000
    | WSTRING       = 0x00100000
    | CONSTANT      = 0x00200000
    | ARRAY         = 0x00400000
    | STRUCTURE     = 0x00800000   // 사용자 평션 및 펑션 블록에서  구조체 타입        05.10.24
    | FBINSTANCE    = 0x01000000   // 사용자펑션 블록에서 FB_INST 타입                05.10.24
    | ANYARRAY      = 0x02000000       
    | ONLYDIRECTVAR = 0x04000000
    | NIBBLE        = 0x08000000
    | SAFEBOOL      = 0x10000000   // SAFEBOOL 추가 2012.10.8    
    | ONLYCONSTANT  = 0x20000000   // 상수만 가능 추가 2012.10.8    
    | ARRAYSIZE     = 0x40000000   // 배열 포인터 타입 (배열 포인터의 사이즈 체크 처리)
    | POINTER       = 0x80000000   // 포인터 타입 (시작주소, 타입크기, 사이즈)

/// function, function Block의 ANY 타입을 구별하기 위함 
type AnyType =
   | REAL_LREAL                                                                                                      = 101
   | STRING_WSTRING                                                                                                  = 102
   | BYTE_WORD_DWORD_LWORD                                                                                           = 103
   | WORD_UINT_STRING_WSTRING                                                                                        = 104
   | DWORD_UDINT_STRING_WSTRING                                                                                      = 105
   | INT_DINT_LINT_UINT_UDINT_ULINT                                                                                  = 106
   | SINT_INT_DINT_LINT_USINT_UINT_UDINT_ULINT                                                                       = 107
   | SINT_INT_DINT_LINT_USINT_UINT_UDINT_ULINT_REAL_LREAL                                                            = 108
   | BOOL_BYTE_WORD_DWORD_LWORD                                                                                      = 109            
   | BYTE_WORD_DWORD_LWORD_SINT_INT_DINT_LINT_USINT_UINT_UDINT_ULINT_STRING_WSTRING                                  = 110
   | BOOL_WORD_DWORD_LWORD_SINT_INT_DINT_LINT_USINT_UINT_UDINT_ULINT_STRING_WSTRING                                  = 111
   | BOOL_BYTE_DWORD_LWORD_SINT_INT_DINT_LINT_USINT_UINT_UDINT_ULINT_DATE_STRING_WSTRING                             = 112
   | BOOL_BYTE_WORD_LWORD_SINT_INT_DINT_LINT_USINT_UINT_UDINT_ULINT_REAL_TIME_TOD_STRING_WSTRING                     = 113
   | BOOL_BYTE_WORD_DWORD_SINT_INT_DINT_LINT_USINT_UINT_UDINT_ULINT_LREAL_DT_STRING_WSTRING                          = 114
   | BOOL_BYTE_WORD_DWORD_LWORD_INT_DINT_LINT_USINT_UINT_UDINT_ULINT_REAL_LREAL_STRING_WSTRING                       = 115
   | BOOL_BYTE_WORD_DWORD_LWORD_SINT_DINT_LINT_USINT_UINT_UDINT_ULINT_REAL_LREAL_STRING_WSTRING                      = 116
   | BOOL_BYTE_WORD_DWORD_LWORD_SINT_INT_LINT_USINT_UINT_UDINT_ULINT_REAL_LREAL_STRING_WSTRING                       = 117
   | BOOL_BYTE_WORD_DWORD_LWORD_SINT_INT_DINT_USINT_UINT_UDINT_ULINT_REAL_LREAL_STRING_WSTRING                       = 118
   | BOOL_BYTE_WORD_DWORD_LWORD_SINT_INT_DINT_LINT_UINT_UDINT_ULINT_REAL_LREAL_STRING_WSTRING                        = 119
   | BOOL_BYTE_WORD_DWORD_LWORD_SINT_INT_DINT_LINT_USINT_UDINT_ULINT_REAL_LREAL_DATE_STRING_WSTRING                  = 120
   | BOOL_BYTE_WORD_DWORD_LWORD_SINT_INT_DINT_LINT_USINT_UINT_ULINT_REAL_LREAL_TIME_TOD_STRING_WSTRING               = 121
   | BOOL_BYTE_WORD_DWORD_LWORD_SINT_INT_DINT_LINT_USINT_UINT_UDINT_REAL_LREAL_STRING_WSTRING                        = 122
   | BOOL_BYTE_WORD_DWORD_LWORD_SINT_INT_DINT_LINT_USINT_UINT_UDINT_ULINT_REAL_LREAL_TIME_DATE_TOD_DT                = 123
   | BOOL_BYTE_WORD_DWORD_LWORD_SINT_INT_DINT_LINT_USINT_UINT_UDINT_ULINT_REAL_LREAL_TIME_DATE_TOD_DT_WSTRING        = 124
   | BOOL_BYTE_WORD_DWORD_LWORD_SINT_INT_DINT_LINT_USINT_UINT_UDINT_ULINT_REAL_LREAL_TIME_DATE_TOD_DT_STRING         = 125
   | BOOL_BYTE_WORD_DWORD_LWORD_SINT_INT_DINT_LINT_USINT_UINT_UDINT_ULINT_REAL_LREAL_TIME_DATE_TOD_DT_STRING_WSTRING = 126
   | DWORD_SINT_INT_DINT_LINT_USINT_UINT_UDINT_ULINT_LREAL_STRING_WSTRING                                            = 127
   | LWORD_SINT_INT_DINT_LINT_USINT_UINT_UDINT_ULINT_REAL_STRING_WSTRING                                             = 128
   | LWORD_DATE_TOD_STRING_WSTRING                                                                                   = 129
   | TIME_TOD_DT                                                                                                     = 130
   | DINT_LINT                                                                                                       = 131
   | DWORD_REAL                                                                                                      = 132
   | INT_REAL                                                                                                        = 133
   | UINT_REAL                                                                                                       = 134
   | BYTE_WORD_DWORD_LWORD_SINT_INT_DINT_LINT_USINT_UINT_UDINT_ULINT_REAL_LREAL_TIME_DATE_TOD_DT                     = 135
   | BOOL_BYTE_WORD_DWORD_LWORD_SINT_INT_DINT_LINT_USINT_UINT_UDINT_ULINT_REAL_LREAL                                 = 136
   | BOOL_SAFEBOOL                                                                                                   = 137
   | TIMER_CONSTANT                                                                                                  = 138
   | UINT_CONSTANT                                                                                                   = 139
   | INT_CONSTANT                                                                                                    = 140
   | BOOL_ONLY                                                                                                       = 141
   | BYTE_WORD_DWORD_LWORD_SINT_INT_DINT_LINT_USINT_UINT_UDINT_ULINT_REAL_LREAL_TIME_DATE_TOD_DT_STRING_WSTRING      = 142
   | BOOL_BYTE_WORD_DWORD_LWORD_SINT_INT_DINT_LINT_USINT_UINT_UDINT_ULINT_REAL_LREAL_TIME_DATE_TOD_DT_STRING_PTR     = 143
   | SINT_INT_DINT_LINT_USINT_UINT_UDINT_ULINT_REAL_LREAL_TIME_DATE_TOD_DT_STRING                                    = 144

/// XG5000내에서 컨피그레이션(Configuration)은 PLC, HMI, DRIVE 등 기기 단위의 구성을 의미한다
[<AutoOpen>]
module Config =
    /// 현재는 0x103(=259)으로 고정
    let version = 0x103
    
    /// 컴파일러에게 전달하기 위한 변수 타입 (XGI 변수 타입과 기본타입 인덱스 동일함)
    // RecordType.h : LS 산전에서 전달받은 header file
    // #define BOOL_VARTYPE	1      // 1 비트
    // ....
    [<Flags>]
    type VarType =
        | NONE          = 0     
        | BOOL          = 1     
        | BYTE          = 2     
        | WORD          = 3         
        | DWORD         = 4    
        | LWORD         = 5    
        | SINT          = 6     
        | INT           = 7      
        | DINT          = 8     
        | LINT          = 9     
        | USINT         = 10   
        | UINT          = 11    
        | UDINT         = 12   
        | ULINT         = 13   
        | REAL          = 14    
        | LREAL         = 15   
        | TIME          = 16    
        | DATE          = 17    
        | TIME_OF_DAY   = 18     
        | DATE_AND_TIME = 19    
        | STRING        = 20
        | WSTRING       = 21
        | ARRAY         = 22
        | STRUCT        = 23
        | FB_INST       = 24
        | SAFEBOOL      = 25
          //추가 항목 개발 중  :1000번 부터 인덱스 의미없음
        | TON           = 1000 //일반 타이머 Melsec : T
        | TON_UINT      = 1001 //1msec 타이머 Melsec : T
        | TMR           = 1002 //적산 타이머 Melsec : ST
        | CTU_UINT      = 1003 //일반 카운터 Melsec : C

    /// PLC에 특정 기능이 있는지에 대한 속성값으로, 다음의 값의 OR된 값이 저장된다. 
    // Bitmask OR'ing --> |||
    [<Flags>]
    type AttributeFlag =
        | HAS_CONFIG_GLOBAL_VAR     = 0x00000001 
        | HAS_CONFIG_ACCESS_VAR     = 0x00000002 
        | HAS_CONFIG_DIRECT_COMMENT = 0x00000004 

        | HAS_RESOURCE_GLOBAL_VAR   = 0x00000010 
        | HAS_USER_FUN_FB           = 0x00000020 
        | HAS_USER_TASK             = 0x00000040 

        | HAS_USER_LIBRARY          = 0x00000100 
        | HAS_USER_DATA_TYPE        = 0x00000200 
        | HAS_LOCAL_VAR             = 0x00000400 

        | HAS_BASIC_PARA            = 0x00001000 
        | HAS_IO_PARA               = 0x00002000 
        | HAS_INTERNAL_PARA         = 0x00004000 // 2005.7 XGB 추가 
        | HAS_LOCAL_ETH_PARA        = 0x00008000 // NGP2000 Ethernet 

        | HAS_REDUNDANCY_PARA       = 0x00010000 // 2007.8.7 이중화 para 
        | HAS_PB_POOL               = 0x00020000 // 2011.5.26 
        | HAS_NETWORK_PARA          = 0x00020000 // 2010.10.27 CANopen 추가 
        | HAS_EXTRA_INFO            = 0x00080000 // serialize extension 

        | IEC_CONFIG                = 0x10000000 // IEC 형 
        | SAFETY_CONFIG             = 0x20000000 //SAFETY 형 

    /// 컨피그레이션에 대한 유형 값 
    type Kind =
        | SYSCON_TYPE_UNKNOWN     = 0
        | SYSCON_TYPE_PLC         = 1
        | SYSCON_TYPE_HMI         = 2
        | SYSCON_TYPE_INV         = 3
        | SYSCON_TYPE_NETWORK     = 4
        | SYSCON_TYPE_MOTION      = 5
        | SYSCON_TYPE_EXT_ADAPTER = 6
        | SYSCON_TYPE_FAKE        = 100
    /// 변수 유형 (LocalVar 및 GlobalVariable 둘다 동일한 유형을 사용
    module Variable = 
        type Kind = 
            | VAR_NONE              = 0
            | VAR                   = 1
            | VAR_CONSTANT          = 2
            | VAR_INPUT             = 3
            | VAR_OUTPUT            = 4
            | VAR_IN_OUT            = 5
            | VAR_GLOBAL            = 6
            | VAR_GLOBAL_CONSTANT   = 7
            | VAR_EXTERNAL          = 8
            | VAR_EXTERNAL_CONSTANT = 9
            | VAR_RETURN            = 10
            | VAR_GOTO_S1           = 11
            | VAR_TRANS             = 12
            ///// NEVER change the enum value of NULL1 !!! 
            //| NULL1               = 0 
            ///// It influences GlobalVarMem allocation! 
            //| TYPE                = 1
            //| VAR_IN_OUT          = 2
            //| VAR_IN              = 3
            //| VAR_OUT             = 4
            //| VAR_OUT_RETAIN      = 5
            //| VAR                 = 6
            //| VAR_CONSTANT        = 7
            //| VAR_CONSTANT_RETAIN = 8
            //| VAR_RETAIN          = 9
            ///// if PLC_TYPE != 1 
            //| VAR_EXTERN          = 10
            //| VAR_GLOBAL          = 11
            //| VAR_GLOBAL_CONSTANT = 12
            ///// used only in BODY compile 
            //| I_DIRECT_ADDRESS    = 13
            ///// used only in BODY compile 
            //| Q_DIRECT_ADDRESS    = 14
            ///// used only in BODY compile 
            //| M_DIRECT_ADDRESS    = 15
            ///// used only in BODY compile 
            //| FUNCTION_KIND       = 16
            ///// used as Function block name & output of FUN 
            //| FUNCNAME            = 17
            //| SFC_TRANS           = 18
            ///// when string constant is used in FUN 
            //| FUN_STRCONST        = 19
            ///// following are for GM1 extern var!! 
            //| VAR_EXT_IN          = 20
            //| VAR_EXT_OUT         = 21 
            //| VAR_EXT_IN_OUT      = 22 
            //| ARY_EXT_IN          = 23 
            //| ARY_EXT_OUT         = 24 
            //| ARY_EXT_IN_OUT      = 25
            ///// 98/2/12 초기스텝으로 돌아가는 GOTO_S1 변수에만 쓰이는 타입 . 
            //| SFC_STEP1           = 26
        //type Type =
        //    FB명, UDT명, ARRAY[1..2,1..3] OF BOOL, WORD, DWORD,...

        ///변수의 내부 속성 
        [<Flags>]
        type State =
            | STATE_RETAIN   = 1
            | STATE_USEDIT   = 2 // 사용 유무는 PLC 에 다운로드하지 않는다 . ( 체크섬이 변경되는 현상발생 )(2016.12.08) 
            | STATE_READONLY = 4
            | STATE_SPECIAL  = 8
    module POU =
        module Program =
            /// 프로그램에 대한 버전으로 현재는 0x100
            let version = 0x100
            /// 프로그램 유형
            type Kind =
                | LD_EDITOR          = 0 
                | IL_EDITOR          = 1 
                | SFC_EDITOR         = 2 
                | SFC_MANAGER_EDITOR = 3 
                | ST_EDITOR          = 4 
                | FBD_EDITOR         = 5 
                | PD_EDITOR          = 6 
                | GCODE_EDITOR       = 7 
                | LIB_EDITOR         = 8 
                | ILT_EDITOR         = 9 // IL Text editor 
            // Program / Body / LDRoutine
            module LDRoutine =
                /// LD 프로그램 상에서의 구성요소에 대한 식별자 
                type ElementType =
                    | LDElementMode_Start    = 0
                    | LineType_Start         = 0
                    | VertLineMode           = 0 // LineType_Start '|' 
                    | HorzLineMode           = 1 // '-' 
                    | MultiHorzLineMode      = 2 // '-->>' 
                    ///addonly hereadditional line type device. 
                    | LineType_End           = 5

                    | ContactType_Start      = 6
                    | ContactMode            = 6 // ContactType_Start // '-| |-' 
                    | ClosedContactMode      = 7 // '-|/|-' 
                    | PulseContactMode       = 8 // '-|P|-' 
                    | NPulseContactMode      = 9// '-|N|-' 
                    | ClosedPulseContactMode = 10 // '-|P/|-' 
                    | ClosedNPulseContactMode= 11// '-|N/|-' 
                    ///addonly hereadditional contact type device. 
                    | ContactType_End        = 13

                    | CoilType_Start         = 14
                    | CoilMode               = 14 // CoilType_Start // '-( )-' 
                    | ClosedCoilMode         = 15 // '-(/)-' 
                    | SetCoilMode            = 16 // '-(S)-' 
                    | ResetCoilMode          = 17 // '-(R)-' 
                    | PulseCoilMode          = 18 // '-(P)-' 
                    | NPulseCoilMode         = 19 // '-(N)-' 
                    ///addonly hereadditional coil type device. 
                    | CoilType_End           = 30

                    | FunctionType_Start     = 31
                    | FuncMode               = 32
                    | FBMode                 = 33 // '-[F]-' 
                    | FBHeaderMode           = 34 // '-[F]-' : Header 
                    | FBBodyMode             = 35 // '-[F]-' : Body 
                    | FBTailMode             = 36 // '-[F]-' : Tail 
                    | FBInputMode            = 37
                    | FBOutputMode           = 38
                    ///addonly hereadditional function type device. 
                    | FunctionType_End       = 45

                    | BranchType_Start       = 51
                    | SCALLMode              = 52
                    | JMPMode                = 53
                    | RetMode                = 54
                    | SubroutineMode         = 55
                    | BreakMode              = 56
                    | ForMode                = 57
                    | NextMode               = 58
                    ///addonly hereadditional branch type device. 
                    | BranchType_End         = 60

                    | CommentType_Start      = 61
                    | InverterMode           = 62 // '-*-' 
                    | RungCommentMode        = 63 // 'rung comment' 
                    | OutputCommentMode      = 64 // 'output comment' 
                    | LabelMode              = 65
                    | EndOfPrgMode           = 66
                    | RowCompositeMode       = 67 // 'row' 
                    | ErrorComponentMode     = 68
                    | NullType               = 69
                    | VariableMode           = 70
                    | CellActionMode         = 71
                    | RisingContact          = 72 //add dual    xg5000 4.52
                    | FallingContact         = 73 //add dual    xg5000 4.52
                    ///addonly hereadditional comment type device. 
                    | CommentType_End        = 90

                    /// vertical function(function & function block) related 
                    | VertFunctionType_Start = 100
                    | VertFuncMode           = 101
                    | VertFBMode             = 102
                    | VertFBHeaderMode       = 103
                    | VertFBBodyMode         = 104
                    | VertFBTailMode         = 105
                    /// add additional vertical function type device here 
                    | VertFunctionType_End   = 109
                    | LDElementMode_End      = 110

                    | Misc_Start             = 120
                    | ArrowMode              = 121
                    | Misc_End               = 122

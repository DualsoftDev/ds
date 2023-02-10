/**
  **************************************************************************************************
  * @file       NMF.cs
  * @author     PAIX Lab.
  * @version
  * @date       2020-01-16
  * @brief      Network Multi Function 장치 지원파일 입니다.
  * @note       C#
  * @copyright  2019. PAIX Co., Ltd. All rights reserved.
  * @see        http://www.paix.co.kr/
  **************************************************************************************************
  * @note       2019-06-26  Initial release
  *             2019-07-29  주석 오류 수정
  *             2019-09-09  기능 추가 수정
  *             2019-11-27  반환값 추가 및 기능 수정
  *             2020-01-16  함수명 및 주석 수정
  *				2021-06-25	NMF DO,DI8 추가 확인, NMF 구성 정합성 확인
  *				2021-12-21	카운트 함수 nmf_ExtCountStartEx를 추가 (Do상태에 Pulse형태 추가)
  *				2022-01-14  카운트 반복 모드 추가
  **************************************************************************************************
  */

using System.Runtime.InteropServices;     // DLL support

namespace Server.Common.NMF;

public class NMF
{
    /**
      * @brief  NMF 정의값
      */
    public const short  MAX_DEV         = 255;  /*!< 네트워크 최대 장치 수량 */
    public const short  MAX_BRD         = 8;    /*!< 최대 보드 개수 */
    public const short  EXT_IN_PINS     = 8;    /*!< External 입력 핀 개수 */
    public const short  BRD_VS_DIO_PINS = 16;   /*!< DIO보드의 핀 개수 */
    public const short  BRD_VS_AO_PINS  = 8;    /*!< AO보드의 핀 개수 */
    public const short  BRD_VS_AI_PINS  = 8;    /*!< AI보드의 핀 개수 */
    public const short  BRD_VS_AO_GROUP = 2;    /*!< AO보드의 그룹 개수 */
    public const short  MAX_DIO_PINS    = (MAX_BRD*BRD_VS_DIO_PINS);    /*!< 최대 DIO 핀수 */
    public const short  MAX_AIO_PINS    = (MAX_BRD*BRD_VS_AI_PINS );    /*!< 최대 AIO 핀수 */

    public const int    AI_MAX_SRATE            = (100000);     /*!< 최대 샘플링 주파수(100KHz) */
    public const int    AI_MEM_BLK_VS_DATAS     = 2048;         /*!< AI 메모리 블럭당 개수 2048개(1개당 4Byte) */
    public const int    AI_MEM_BLOCKS           = 64;           /*!< AI 메모리 총 블럭 개수 65개 */
    public const int    AI_MEM_MAX_DATAS        = (AI_MEM_BLK_VS_DATAS*(AI_MEM_BLOCKS+1));      /*!< AI 메모리 최대 데이터 개수 133120=2048*65(1개는 채널 메모리) */

    public const int    COUNTER_MTIME_MAX_DATAS = 1024;        /*!< Count 시간측정 데이터 최대 개수 */

    /**************************************************************************************************/
    /**
      * @brief  함수 반환값
      */
    public enum Enmf_FUNC_RESULT : short
    {
        nmf_R_OK                        =  0,       /*!< 정상 */
        nmf_R_NOT_CONNECT               = -1,       /*!< 장치와 연결이 끊어진 경우 */
        nmf_R_SOCKET_ERR                = -2,       /*!< 소켓 에러 */
        nmf_R_UNKOWN                    = -3,       /*!< 지원되지 않는 함수 호출 */
        nmf_R_INVALID                   = -4,       /*!< 함수 인자값에 오류발생 */
        nmf_R_SYNTAX_ERR                = -5,       /*!< 함수 호출 시, 구문 오류 발생 */
        nmf_R_CONN_ERR                  = -6,       /*!< 연결되어 있지 않음 */
        nmf_R_INVALID_NMF               = -7,       /*!< 유효하지 않은 NMF장치번호 */
        nmf_R_FUNC_NOT_ENOUGH           = -8,       /*!< 함수별 지정된 길이만큼 전송/수신하여야 하나 부족한 경우(예:수행 결과로 수신한 응답 데이터가 원하는 길이보다 부족한 경우) */
        nmf_R_NOT_RESPONSE              = -9,       /*!< 함수 호출 시, 응답이 없는 경우 */
        nmf_R_CMDNO_ERR                 = -10,      /*!< 함수 호출 시, 식별코드에 오류 발생 */
        nmf_R_NOT_EXISTS                = -11,      /*!< 네트워크 장치가 식별되지 않는 경우, 방화벽이나 연결 상태를 확인 */
        nmf_R_ICMP_LOAD_FAIL            = -12,      /*!< ICMP.DLL 로드 실패, nmf_PingCheck 호출시 발생. PC에 DLL유무를 확인 */
        nmf_R_FILE_LOAD_FAIL            = -13,      /*!< F/W file 로드 실패 */
        nmf_R_SYNCHRONIZE               = -14,      /*!< 동기화 오류(뮤텍스,세마포어등에서 대기오류) */
        nmf_R_SOCKET_TX_TMO             = -15,      /*!< 소켓 데이터 전송중 Time-out */
        nmf_R_SOCKET_RX_TMO             = -16,      /*!< 소켓 데이터 수신중 Time-out */
        nmf_R_BUF_OVERFLOW              = -17,      /*!< 지정 Buffer크기를 초과(ex.수신버퍼 초과) */
        nmf_R_MCU_FAIL                  = -18,      /*!< MCU동작상의 실패 */
        nmf_R_FUNC_FAIL                 = -19,      /*!< 함수수행에 실패한 경우 */
        nmf_R_HW_PART_NONE              = -20,      /*!< 수행하려고 하는 기능의 H/W장치나 구성이 없는경우 예)CPU모델에서 Count 기능 수행 */

        nmf_R_BRD_INVALID               = -100,     /*!< 함수에서 지정한 보드가 없거나 범위를 초과한 경우 예)DO보드가 2개인데 3번 보드에 1Pin에 ON명령 하달시 */
        nmf_R_BRD_MISMATCH_DO           = -101,     /*!< 함수에서 지정한 보드가 없거나 DO가 아닌 경우 예)DI, AO, AI 보드에 DO명령이 전달됨 */
        nmf_R_BRD_MISMATCH_DI           = -102,     /*!< 함수에서 지정한 보드가 없거나 DI가 아닌 경우 예)AO, AI, DO 보드에 DI명령이 전달됨 */
        nmf_R_BRD_MISMATCH_AI           = -103,     /*!< 함수에서 지정한 보드가 없거나 AI가 아닌 경우 예)DO, DI, AO 보드에 AI명령이 전달됨 */
        nmf_R_BRD_MISMATCH_AO           = -104,     /*!< 함수에서 지정한 보드가 없거나 AO가 아닌 경우 예)DO, DI, AI 보드에 AO명령이 전달됨 */
        nmf_R_BRD_INVALID_PIN           = -105,     /*!< 해당 보드의 Pin 범위를 초과 */
			nmf_R_BRD_MISMATCH_COMPO        = -106,     /*!< 구성 정보의 정합성이 일치 하지 않는 경우 */

        nmf_R_AI_SRATE_ERR              = -200,     /*!< AI 샘플링 적용 시, 샘플링 속도 설정 범위 초과 (0~ 100000) */
        nmf_R_AI_INVALID_RANGE          = -201,     /*!< AI 입력 전압 Range 설정 범위 초과 (0 ~ 1) */
        nmf_R_AI_MEM_BLOCK_RANGE_ERR    = -202,     /*!< AI 외부 메모리 설정 시 최대 Block 초과 외부 메모리 참고 */

        nmf_R_AO_RANGE_OVER             = -300,     /*!< AO에서 출력 전압 Range를 넘어선 경우 예) ±5V Range에 6V가 설정 될 경우) */

        nmf_R_ARG_RNG_OVER              = -1001,    /*!< 지정한 함수의 인자가 설정 범위를 초과한 경우
                                        ~ -1500,         인자의 순서대로 에러코드가 증가함. 예) 3번째 인자 오류 시 (-1003) */
        nmf_R_ARG_ERR                   = -1501,    /*!< 지정한 함수의 인자 오류가 있는 경우
                                        ~ -1999,         인자의 순서대로 에러코드가 증가함. 예) 3번째 인자 오류 시 (-1503) */

        // 기기에서만 생성되는 에러코드=================================================================================
        nmf_R_FW_ARG_RNG_OVER           = -20001,   /*!< 지정한 함수를 수행하기위해 전송되는 프로토콜에 인자가 설정 범위를 초과한 경우
                                        ~ -20500,        인자의 순서대로 에러코드가 증가함. 예) 3번째 인자 오류 시 (-20003) */
        nmf_R_FW_ARG_ERR                = -20501,   /*!< 지정한 함수를 수행하기위해 전송되는 프로토콜에 인자 오류가 있는 경우
                                        ~ -20999,        인자의 순서대로 에러코드가 증가함. 예) 3번째 인자 오류 시 (-20503) */
    };
    //------------------------------------------------------------------------------
    /**
      * @brief NMF의 보드 타입 열거형
      */
    public enum Enmf_BoardType : short
    {
        nmf_btNone		= 0,	/*!< 없음 */
        nmf_btDO16		= 1,	/*!< DO 16핀 보드 */
        nmf_btDI16		= 2,	/*!< DI 16핀 보드 */
        nmf_btAI8		= 3,	/*!< AI 8핀 보드 */
        nmf_btAO8		= 4,	/*!< AO 8핀 보드 */
        nmf_btAI8AO8	= 5,	/*!< AI 8핀, AO 8핀 보드 */
	        nmf_btDO8		= 6,	/*!< DO 8핀 보드 */
	        nmf_btDI8		= 7,	/*!< DI 8핀 보드 */
	        nmf_btDI8DO8	= 8,	/*!< DI 8핀 DO 8핀 보드 */
    };

    //------------------------------------------------------------------------------
    /**
      * @brief  보드 형태의 장치(NMF)구성 정보
      */
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public struct TNMF_COMPO
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=4)]
        public short[]  nIP;                /*!< IP주소 */
			public short   	nModelPart;         /*!< 모델 Part 정보 (0=NMF, 1=UDIO) 지원되는 신(NMF),구(UDIO)의 구분 */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=MAX_BRD)]
	    public short[]  nType;     			/*!< 보드 형태 (0=없음, 1=D016, 2=DI16, 3=AI8, 4=AO8, 5=AI8AO8, 6=DO8, 7=DI8, 8=DI8DO8, 9~49=예약) */
			[MarshalAs(UnmanagedType.ByValArray, SizeConst=50)]
        public short[]  nCntBrd;   			/*!< 형태별 보드 개수(Max.8) 배열 번호는 보드 형태 값 예)AI8=2개, DO16=3개 일 경우 nCntBrd[1]=3, nCntBrd[3]=2가 됨. */
		public short   	nTotalCntBrd;       /*!< 연결된 전체 보드 개수 (Max.8) */
		public short   	nReserved;          /*!< 예약 */
    };

    /**
      * @brief  DIO보드별 상태 정보
      */
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public struct TNMF_BRD_DIO_STS
    {
        public short    nID;                /*!< 보드 ID (0~7) -1일 경우 부적합 보드타입 또는 보드 없음 */
        public short    nReserved;          /*!< 예약 */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=BRD_VS_DIO_PINS)]
        public short[]  nPinStatus;         /*!< DI : 0=입력OFF, 1=입력ON
                                                 DO : 0=출력OFF, 1=출력ON */
    };

    /**
      * @brief  AI 샘플링 상태 정보
      */
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public struct TNMF_BRD_AI_SMPL
    {
        public short    nID;                /*!< 보드 ID (0~7) -1일 경우 부적합 보드타입 또는 보드 없음 */
        public short    nReserved;          /*!< 예약 */
        public short    nStatus;            /*!< 샘플링 상태 (0=미실행, 1=트리거 입력 대기중, 2=실행 중, 3=Read중 샘플링 대기 중, 4=사용자 정지, 5=메모리 Full 정지) */
        public short    nTrgType;           /*!< 샘플링 트리거 타입 (0=임의 시점, 1=Trg Rising, 2=Trg Falling, 3=Trg Both) */
        public short    nExtPin;            /*!< 샘플링에 트리거를 사용하는 경우 External Pin 번호 (0 ~ 3) */
        public short    nRange;             /*!< AI 보드의 입력 범위 (0=±5V, 1=±10V) */
        public int      lRate;              /*!< AD 샘플링 주기 (0 ~ 100000Hz) */
        public short    nMemFullOvW;        /*!< 메모리 Overwrite여부(0=Ovewrite안함(샘플링 정지), 1=Overwrite(샘플링 정지안함) */
        public short    nMemFullFlag;       /*!< 메모리 Full Flag (0=Full X, 1=Full O) */
        public int      lMemCntData;        /*!< 데이터로 채워져 있는 메모리 개수 (최대 133120개) */
        public int      lMemCntEmpty;       /*!< 비어 있는 메모리 개수 (최대 133120개) */
        public int      lRespSize;          /*!< 응답 데이터 개수 (최대 133120개) */
    };

    /**
      * @brief  AI보드 상태 정보
      */
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public struct TNMF_BRD_AI_STS
    {
        public short    nID;                /*!< 보드 ID (0~7) -1일 경우 부적합 보드타입 또는 보드 없음 */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=BRD_VS_AI_PINS)]
        public short[]  nStatus;            /*!< 샘플링 상태 (0=미실행, 1=트리거 입력 대기중, 2=실행 중, 3=Read중 샘플링 대기 중, 4=사용자 정지, 5=메모리 Full 정지) */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=BRD_VS_AI_PINS)]
        public short[]  nTrgType;           /*!< 샘플링 트리거 타입 (0=임의 시점, 1=Trg Rising, 2=Trg Falling, 3=Trg Both) */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=BRD_VS_AI_PINS)]
        public short[]  nExtPin;            /*!< 샘플링에 트리거를 사용하는 경우 External Pin 번호 (0 ~ 3) */

        public short    nRange;             /*!< AI 보드의 입력 범위 (0=±5V, 1=±10V) */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=BRD_VS_AI_PINS)]
        public int[]    lRate;              /*!< AD 샘플링 주기 (0 ~ 100000Hz) */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=BRD_VS_AI_PINS)]
        public short[]  nMemFullOvW;        /*!< 메모리 Overwrite여부(0=Ovewrite안함(샘플링 정지), 1=Overwrite(샘플링 정지안함) */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=BRD_VS_AI_PINS)]
        public short[]  nMemFullFlag;       /*!< 메모리 Full Flag (0=Full X, 1=Full O) */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=BRD_VS_AI_PINS)]
        public short[]  nMemExists;         /*!< 메모리의 상태 (0=데이터 없음, 1=데이터 있음) */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=BRD_VS_AI_PINS)]
        public int[]    lADValue;           /*!< 설정된 입력 범위에 따른 Pin에 입력된 전압의 디지털 값 (-32768 ~ 32767) */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=BRD_VS_AI_PINS)]
        public double[] dVolt;              /*!< Pin에 입력된 전압 값 */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=BRD_VS_AI_PINS)]
        public double[] dCurrent;           /*!< Pin에 입력된 전류 값 */
    };

    /**
      * @brief  AO보드 상태 정보
      */
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public struct TNMF_BRD_AO_STS
    {
        short   nID;                        /*!< 보드 ID (0~7) -1일 경우 다른 보드타입 또는 보드 없음 */
        public short    nReserved;          /*!< 예약 */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=BRD_VS_AO_PINS)]
        public short[]  nRange;             /*!< AO 보드의 각 Pin별 출력범위
                                                (설정값 : 0=0~5V, 1=0~10V, 2=0~10.86V, 3=±5V, 4=±10V, 5=±10.86V) */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=BRD_VS_AO_PINS)]
        public int[]    lDAValue;           /*!< 설정범위(nRange)에 따른 최근 출력 전압의 디지털 값
                                                 (0,1,2 = 0~65535, 3,4,5 = -32768 ~ 32767 ) */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=BRD_VS_AO_PINS)]
        public double[] dVolt;              /*!< 설정된 전압값 */
    };

    /**
      * @brief  External Pin 정보
      */
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public struct TNMF_EXT_STS
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=EXT_IN_PINS)]
        public short[]  nStatus;            /*!< External Pin의 입력 상태 */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=EXT_IN_PINS)]
        public short[]  nCountEnable;       /*!< External Pin의 Count 기능 사용 여부 */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=EXT_IN_PINS)]
        public uint[]   dwCountValue;       /*!< External Pin의 Count 기능 사용시, Count(횟수) */
        public ulong    ddwCountTime;       /*!< External Pin의 Count 동작시, 시간(us) */
    };

    /**
      * @brief  External Pin Count 상태 정보
      */
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public struct TNMF_EXTC_INFO
    {
        public ulong    ddwCountTime;       /*!< Count 동작시간(us) */
        public short    nReserved0;         /*!< 예약0 */
        public short    nReserved1;         /*!< 예약1 */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=EXT_IN_PINS)]
        public short[]  nStatus;            /*!< Count의 해당 External Pin 입력상태 */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=EXT_IN_PINS)]
        public short[]  nEdge;              /*!< 설정 신호 Edge(0=Rising, 1=Falling, 2=Both) */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=EXT_IN_PINS)]
        public short[]  nAddMode;            /*!< 추가 기능 모드 Bit Mask(Bit 0=시간측정, Bit 1=Count 반복 모드) */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=EXT_IN_PINS)]
        public short[]  nEnable;            /*!< Count 사용 여부 */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=EXT_IN_PINS)]
        public short[]  nDO_ID;             /*!< 연계 DO보드 ID */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=EXT_IN_PINS)]
        public short[]  nDO_PinNo;          /*!< 연계 DO보드의 Pin No */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=EXT_IN_PINS)]
        public short[]  nDO_PinStatus;      /*!< 연계 DO보드의 Pin 상태 */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=EXT_IN_PINS)]
        public uint[]   dwSetCount;         /*!< 설정 Count 값 (DO 연계에 적용) */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=EXT_IN_PINS)]
        public uint[]   dwCurCount;         /*!< 현재 Count 값 */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=EXT_IN_PINS)]
        public short[]  nInterval;          /*!< 측정간격(us) */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=EXT_IN_PINS)]
        public uint[]   dwMTime;            /*!< 측정시간 */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=EXT_IN_PINS)]
        public short[]  nEReserved0;        /*!< Ext Pin 별 예약0 */
        [MarshalAs(UnmanagedType.ByValArray, SizeConst=EXT_IN_PINS)]
        public short[]  nEReserved1;        /*!< Ext Pin 별 예약1 */
    };

    /**
      * @brief  Count 시간측정 데이터 상태 정보
      */
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public struct TNMF_CMT_STS
    {
        public short    nStatus;            /*!< Count의 해당 External Pin 입력상태 */
        public short    nEdge;              /*!< 설정 신호 Edge(0=Rising, 1=Falling, 2=Both) */
        public short    nAddMode;           /*!< 추가 기능 모드 Bit Mask(Bit 0=시간측정, Bit 1=Count 반복 모드) */
        public short    nEnable;            /*!< Count 사용 여부 */
        public uint     dwValue;            /*!< Count(횟수) */
        public short    nInterval;          /*!< 측정간격(us) */
        public short    nMemCntData;        /*!< 데이터로 채워져 있는 메모리 개수 (최대 1024개) */
        public short    nMemCntEmpty;       /*!< 비어 있는 메모리 개수 (최대 1024개) */
        public short    nRespSize;          /*!< 응답 데이터 개수 (최대 1024개) */
        public short    nReserved0;         /*!< 예약0 */
        public short    nReserved1;         /*!< 예약1 */
    };

    /**
      * @brief  보드를 기준으로 한 전체 데이터 정보 (배열 개수: 최대 보드 수)
      */
    [StructLayout(LayoutKind.Sequential, Pack=1)]
    public struct TNMF_ALL_STS
    {
        public short                nCntDIBrd;      /*!< DI 보드 개수 */
        public short                nCntDOBrd;      /*!< DO 보드 개수 */
        public short                nCntAIBrd;      /*!< AI 보드 개수 */
        public short                nCntAOBrd;      /*!< AO 보드 개수 */
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType=UnmanagedType.Struct, SizeConst=MAX_BRD)]
        public TNMF_BRD_DIO_STS[]   tDI;            /*!< DI 데이터 (배열의 Index는 ID를 나타냄) */
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType=UnmanagedType.Struct, SizeConst=MAX_BRD)]
        public TNMF_BRD_DIO_STS[]   tDO;            /*!< DO 데이터 (배열의 Index는 ID를 나타냄) */
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType=UnmanagedType.Struct, SizeConst=MAX_BRD)]
        public TNMF_BRD_AI_STS[]    tAI;            /*!< AI 데이터 (배열의 Index는 ID를 나타냄) */
        [MarshalAs(UnmanagedType.ByValArray, ArraySubType=UnmanagedType.Struct, SizeConst=MAX_BRD)]
        public TNMF_BRD_AO_STS[]    tAO;            /*!< AO 데이터 (배열의 Index는 ID를 나타냄) */
        public TNMF_EXT_STS         tExtSts;        /*!< External Pin 정보 */
    };
    /**************************************************************************************************/

    /**
      * @brief      장치의 네트워크 연결상태를 확인합니다. (Ping Check)
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nIpAddr0        IP Address Field 0
      * @param[in]  nIpAddr1        IP Address Field 1
      * @param[in]  nIpAddr2        IP Address Field 2
      * @param[in]  lWaitTime       응답대기시간(ms)
      * @return     Enmf_FUNC_RESULT
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_PingCheck(short nNmfNo, short nIpAddr0, short nIpAddr1, short nIpAddr2, int lWaitTime);

    /**
      * @brief      장치와 연결을 수행합니다.
      * @param[in]  nNmfNo          장치번호(로터리 스위치 번호이자 IP번호)
      * @param[in]  nIpAddr0        IP Address Field 0
      * @param[in]  nIpAddr1        IP Address Field 1
      * @param[in]  nIpAddr2        IP Address Field 2
      * @return     Enmf_FUNC_RESULT
      * @see        nmf_Disconnect
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_Connect(short nNmfNo, short nIpAddr0, short nIpAddr1, short nIpAddr2);

    /**
      * @brief      장치와 연결을 해제합니다.
      * @param[in]  nNmfNo          장치번호
      * @see        nmf_Connect
      */
    [DllImport("NMF.dll")]
    public static extern void nmf_Disconnect(short nNmfNo);

    /**
      * @brief      장치와의 연결, 네트워크 데이터 송수신의 제한시간을 설정합니다.(ms단위)
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nTMO_Conn       연결 제한시간(ms)
      * @param[in]  nTMO_Tx         송신 제한시간(ms)
      * @param[in]  nTMO_Rx         수신 제한시간(ms)
      * @return     Enmf_FUNC_RESULT
      * @see        nmf_TMOGet
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_TMOSet(short nNmfNo, short nTMO_Conn, short nTMO_Tx, short nTMO_Rx);

    /**
      * @brief      설정된 제한시간을 확인합니다.(ms단위)
      * @param[in]  nNmfNo          장치번호
      * @param[out] pnRetTMO_Conn   연결 제한시간 반환 포인터(ms)
      * @param[out] pnRetTMO_Tx     송신 제한시간 반환 포인터(ms)
      * @param[out] pnRetTMO_Rx     수신 제한시간 반환 포인터(ms)
      * @return     Enmf_FUNC_RESULT
      * @see        nmf_TMOSet
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_TMOGet(short nNmfNo, out short pnRetTMO_Conn, out short pnRetTMO_Tx, out short pnRetTMO_Rx);

    /**
      * @brief      장치의 통신방식을 설정합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nMethod         통신방식(0=TCP, 1=UDP)
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_ProtocolMethodSet(short nNmfNo, short nMethod);

    /**
      * @brief      장치의 통신방식을 확인합니다.
      * @param[in]  nNmfNo          장치번호
      * @return     통신방식(0=TCP, 1=UDP)
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_ProtocolMethodGet(short nNmfNo);

    /**
      * @brief      [ DI보드 ]를 통합하여 전체Pin의 입력상태를 확인합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  pnInStatus      입력Pin의 상태(0=Off, 1=On) 배열 포인터(배열 개수 128개 고정)
      * @return     Enmf_FUNC_RESULT
      * @warning    DI보드 수량과 전체 입력Pin수량을 확인하십시오.
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_DIGet(short nNmfNo, short[] pnInStatus);

    /**
      * @brief      [ DO보드 ]를 통합하여 전체Pin의 출력상태를 확인합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  pnOutStatus     출력Pin의 상태(0=Off, 1=On) 배열 포인터(배열 개수 128개 고정)
      * @return     Enmf_FUNC_RESULT
      * @warning    DO보드 수량과 전체 출력Pin수량을 확인하십시오.
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_DOGet(short nNmfNo, short[] pnOutStatus);

    /**
      * @brief      [ DO보드 ]를 통합하여 전체Pin의 출력상태를 설정합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[out] pnOutStatus     출력Pin의 상태(0=Off, 1=On) 배열 포인터(배열 개수 128개 고정)
      * @return     Enmf_FUNC_RESULT
      * @warning    DO보드 수량과 전체 출력Pin수량을 확인하십시오.
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_DOSet(short nNmfNo, short[] pnOutStatus);

    /**
      * @brief      [ DO보드 ]를 통합하여 전체 Pin중 하나의 출력상태를 설정합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nPinNo          Pin 번호(0~127)
      * @param[in]  nOutStatus      출력pin 상태 (0=Off, 1=On)
      * @return     Enmf_FUNC_RESULT
      * @warning    DO보드 수량과 전체 출력Pin수량을 확인하십시오.
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_DOSetPin(short nNmfNo, short nPinNo, short nOutStatus);

    /**
      * @brief      [ DO보드 ]를 통합하여 출력Pin을 다중으로 선택하고 출력상태를 설정합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nPinCount       출력 Pin 수량(Max. 128)
      * @param[in]  pnPinNo         Pin 번호(0~127) 배열 포인터
      * @param[in]  pnStatus        출력상태(0=Off, 1=On) 배열 포인터
      * @return     Enmf_FUNC_RESULT
      * @warning    DO보드 수량과 전체 출력Pin수량을 확인하십시오.
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_DOSetPins(short nNmfNo, short nPinCount, short[] pnPinNo, short[] pnStatus);

    /**
      * @brief      [ DO보드 ]를 통합하여 전체 Pin중 하나의 출력상태를 반전시킵니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nPinNo          Pin 번호(0~127)
      * @return     Enmf_FUNC_RESULT
      * @warning    DO보드 수량과 전체 출력Pin수량을 확인하십시오.
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_DOSetTogPin(short nNmfNo, short nPinNo);

    /**
      * @brief      [ DO보드 ]를 통합하여 출력Pin을 다중으로 선택하고 출력상태를 반전시킵니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nPinCount       Pin 수량(Max. 128)
      * @param[in]  pnPinNo         Pin 번호(0~127) 배열 포인터
      * @return     Enmf_FUNC_RESULT
      * @warning    DO보드 수량과 전체 출력Pin수량을 확인하십시오.
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_DOSetTogPins(short nNmfNo, short nPinCount, short[] pnPinNo);

    /**
      * @brief      [ DO보드 ]를 통합하여 단일 출력Pin의 유지시간 제한기능을 설정하고 출력 On/Off를 합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nPinNo          Pin 번호(0~127)
      * @param[in]  nOutStatus      설정할 출력값 (0=Off, 1=On)
      * @param[in]  lTime           설정할 유지제한시간(ms)
      * @return     Enmf_FUNC_RESULT
      * @warning    DO보드 수량과 전체 출력Pin수량을 확인하십시오.
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_DOSetLimitTime(short nNmfNo, short nPinNo, short nOutStatus, int lTime);

    /**
      * @brief      [ DO보드 ]를 통합하여 단일 출력 Pin에 설정된 유지시간 제한기능을 확인합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nPinNo          Pin 번호(0~127)
      * @param[out] pnSet           설정 상태를 읽어올 포인터 변수 (0=설정안됨, 1=On, 2=Off)
      * @param[out] pnStatus        현재 제한시간의 상태를 읽어올 포인터 변수
      *                             (0=설정안됨, 1=제한시간 진행중, 2=제한 시간 종료)
      * @param[out] pnOutStatus     현재 Pin 출력상태를 읽어올 포인터 변수 (0=Off, 1=On)
      * @param[out] plRemainTime    남은 시간을 읽어올 포인터 변수(ms)
      * @return     Enmf_FUNC_RESULT
      * @warning    DO보드 수량과 전체 출력Pin수량을 확인하십시오.
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_DOGetLimitTime(short nNmfNo, short nPinNo, out short pnSet, out short pnStatus, out short pnOutStatus, out int plRemainTime);

    /**
      * @brief      [ DO보드 ]를 지정하고 단일Pin의 출력상태를 설정합니다.
      * @param[in]  nNmfNo          장치 번호
      * @param[in]  nID             보드 ID(0 ~ 7)
      * @param[in]  nPinNo          Pin 번호(0~ 15)
      * @param[in]  nOutStatus      출력상태(0=Off, 1=On)
      * @return     Enmf_FUNC_RESULT
      * @warning    보드 ID와 Pin번호를 확인하십시오.
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_DOBrdSetPin(short nNmfNo, short nID, short nPinNo, short nOutStatus);

    /**
      * @brief      [ DO보드 ]를 지정하고 다중Pin의 출력상태를 설정합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nID             보드 ID(0 ~ 7)
      * @param[in]  nPinCount       출력 Pin 개수(Max. 16)
      * @param[in]  pnPinNo         출력 Pin 번호(0 ~ 15) 배열 포인터
      * @param[in]  pnOutStatus     출력상태 (0=Off, 1=On) 배열 포인터
      * @return     Enmf_FUNC_RESULT
      * @warning    보드 ID와 Pin의 개수 및 번호를 확인하십시오.
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_DOBrdSetPins(short nNmfNo, short nID, short nPinCount, short[] pnPinNo, short[] pnOutStatus);

    /**
      * @brief      [ DO보드 ]를 여러개 선택하여, 다중Pin의 출력상태를 설정합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nBrdCount       보드 수량(Max. 8Pin)
      * @param[in]  pnID            보드 ID(0 ~ 7) 배열 포인터
      * @param[in]  plOnPins        보드별 출력 On Mask (16Bit 0 ~ 0xFFFF) 배열 포인터
      * @param[in]  plOffPins       보드별 출력 Off Mask (16Bit 0 ~ 0xFFFF) 배열 포인터
      * @return     Enmf_FUNC_RESULT
      * @warning    보드 개수와 ID, Pin Mask를 확인하십시오.
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_DOmBrdSetPins(short nNmfNo, short nBrdCount, short[] pnID, int[] plOnPins, int[] plOffPins);

    /**
      * @brief      [ DO보드 ]를 지정하고 단일Pin의 출력상태를 반전시킵니다.
      * @param[in]  nNmfNo          장치 번호
      * @param[in]  nID             보드 ID(0 ~ 7)
      * @param[in]  nPinNo          Pin 번호(0~ 15)
      * @return     Enmf_FUNC_RESULT
      * @warning    보드 ID와 Pin번호를 확인하십시오.
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_DOBrdSetTogPin(short nNmfNo, short nID, short nPinNo);

    /**
      * @brief      [ DO보드 ]를 지정하고 다중Pin의 출력상태를 반전시킵니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nID             보드 ID(0 ~ 7)
      * @param[in]  nPinCount       출력 Pin 개수(Max. 16)
      * @param[in]  pnPinNo         반전할 Pin 번호(0 ~ 15) 배열 포인터
      * @return     Enmf_FUNC_RESULT
      * @warning    보드 개수와 Pin의 개수를 확인하십시오.
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_DOBrdSetTogPins(short nNmfNo, short nID, short nPinCount, short[] pnPinNo);

    /**
      * @brief      [ DO보드 ]를 여러개 선택하여, 다중Pin의 출력상태를 반전시킵니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nBrdCount       보드 수량(Max. 8)
      * @param[in]  pnID            보드 ID(0 ~ 7) 배열 포인터
      * @param[in]  plPins          보드별 반전 Mask (16Bit 0 ~ 0xFFFF) 배열 포인터
      * @return     Enmf_FUNC_RESULT
      * @warning    보드 개수와 ID, Pin Mask를 확인하십시오.
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_DOmBrdSetTogPins(short nNmfNo, short nBrdCount, short[] pnID, int[] plPins);

    /**
      * @brief      [ AO보드 ]를 통합하여 단일Pin의 Analog 출력을 설정합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nPinNo          설정할 Pin 번호(0~63)
      * @param[in]  nRange          전압 범위 (0=0~5V, 1=0~10V, 2=0~10.86V, 3=±5V, 4=±10V, 5=±10.86V)
      * @param[in]  dValue          출력 전압값
      * @return     Enmf_FUNC_RESULT
      * @warning    AO보드 수량과 전체 Analog 출력Pin수량을 확인하십시오.
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_AOSetPin(short nNmfNo, short nPinNo, short nRange, double dValue);

    /**
      * @brief      [ AO보드 ]를 통합하여 다중Pin의 Analog 출력을 설정합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nPinCount       설정 Pin 개수(Max. 전체 AO Pin 개수)
      * @param[in]  pnPinNo         설정할 Pin 번호 short 형 배열 포인터 (전체 AO Pin 개수 기준)
      * @param[in]  pnRange         전압 범위 배열 포인터(0=0~5V, 1=0~10V, 2=0~10.86V, 3=±5V, 4=±10V, 5=±10.86V)
      * @param[in]  pdValue         출력 전압값 배열 포인터
      * @return     Enmf_FUNC_RESULT
      * @warning    AO보드 수량과 전체 Analog 출력Pin수량을 확인하십시오.
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_AOSetPins(short nNmfNo, short nPinCount, short[] pnPinNo, short[] pnRange, double[] pdValue);

    /**
      * @brief      [ AO보드 ]를 지정하여 단일Pin의 Analog 출력을 설정합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nID             보드 ID(0 ~ 7)
      * @param[in]  nPinNo          Pin 번호(0 ~ 7)
      * @param[in]  nRange          전압 범위(0=0~5V, 1=0~10V, 2=0~10.86V, 3=±5V, 4=±10V, 5=±10.86V)
      * @param[in]  dValue          출력 전압값
      * @return     Enmf_FUNC_RESULT
      * @warning    보드 ID와 Pin번호를 확인하십시오.
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_AOBrdSetPin(short nNmfNo, short nID, short nPinNo, short nRange, double dValue);

    /**
      * @brief      [ AO보드 ]를 지정하여 다중Pin의 Analog 출력을 설정합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nID             보드 ID(0 ~ 7)
      * @param[in]  nPinCount       설정 Pin 개수(Max. 8Pin)
      * @param[in]  pnPinNo         Pin 번호(0 ~ 7) 배열 포인터
      * @param[in]  pnRange         전압 범위 배열 포인터(0=0~5V, 1=0~10V, 2=0~10.86V, 3=±5V, 4=±10V, 5=±10.86V)
      * @param[in]  pdValue         출력 전압값 배열 포인터
      * @return     Enmf_FUNC_RESULT
      * @warning    보드 ID와 Pin의 개수 및 번호를 확인하십시오.
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_AOBrdSetPins(short nNmfNo, short nID, short nPinCount, short[] pnPinNo, short[] pnRange, double[] pdValue);

    /**
      * @brief      [ AO보드 ]를 여러개 선택하여 다중Pin의 Analog 출력을 설정합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nPinCount       설정 Pin 개수(Max. 8)
      * @param[in]  pnID            보드 ID(0 ~ 7) 배열 포인터
      * @param[in]  pnPinNo         보드별 Pin 번호(0 ~ 7) 배열 포인터
      * @param[in]  pnRange         전압 범위 배열 포인터(0=0~5V, 1=0~10V, 2=0~10.86V, 3=±5V, 4=±10V, 5=±10.86V)
      * @param[in]  pdValue         출력 전압값 배열 포인터
      * @return     Enmf_FUNC_RESULT
      * @warning    보드 개수와 Pin의 개수를 확인하십시오.
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_AOmBrdSetPins(short nNmfNo, short nPinCount, short[] pnID, short[] pnPinNo, short[] pnRange, double[] pdValue);

    /**
      * @brief      [Ext. Count] 외부 입력 핀에 Count 기능을 설정하고, 시작합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nEPinNo         신호를 받을 Ext Pin 번호(0 ~ 7)
      ' @param[in]  nAddMode        추가 기능 모드 Bit Mask(Bit 0=시간측정)
      * @param[in]  nEdge           신호 Edge(0=Rising, 1=Falling, 2=Both)
      * @param[in]  nDO_ID          연계 DO 보드 ID(0 ~ 7)
      * @param[in]  nDO_PinNo       연계 DO 핀의 번호(0 ~ 15)
      * @param[in]  nDO_On          연계 DO 시작 출력 상태(0=Off, 1=On)
      * @param[in]  dwCount         Count할 수량(1 ~ 4,294,967,295(32Bit)), 0은 Count 무제한
      * @return     Enmf_FUNC_RESULT
      * @warning    DO보드 연계기능은 매뉴얼을 참고하여 주십시오.
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_ExtCountStart(short nNmfNo, short nEPinNo, short nAddMode, short nEdge, short nDO_ID, short nDO_PinNo, short nDO_On, uint dwCount);

    /**
      * @brief      [Ext. Count] Count기능에서 Count완료 시 DO연계 출력을 짧게 반전하도록 설정.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nEPinNo         신호를 받을 Ext Pin 번호(0 ~ 7)
      * @param[in]  nAddMode        추가 기능 모드 Bit Mask(Bit 0=시간측정, Bit 1=Count 반복 모드)
      * @param[in]  nEdge           신호 Edge(0=Rising, 1=Falling, 2=Both)
      * @param[in]  nDO_ID          연계 DO 보드 ID(0 ~ 7)
      * @param[in]  nDO_PinNo       연계 DO 핀의 번호(0 ~ 15)
      * @param[in]  nDO_On          연계 DO 시작 출력 상태(0=Off, 1=On, 2=Pulse) Pulse는 종료시 DO가 Pulse처럼 짧게 반전이 됩니다.
      * @param[in]  nDO_Width		연계 DO Pulse의 Width (Width는 2ms 이하로 설정하여 주십시오.)
      * @param[in]  dwCount         Count할 수량(1 ~ 4,294,967,295(32Bit)), 0은 Count 무제한
      * @return     Enmf_FUNC_RESULT
      * @warning    DO보드 연계기능은 매뉴얼을 참고하여 주십시오.
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_ExtCountStart(short nNmfNo, short nEPinNo, short nAddMode, short nEdge, short nDO_ID, short nDO_PinNo, short nDO_On, short nDO_Width, uint dwCount);
    
    /**
      * @brief      [Ext. Count] 외부 입력 핀의 Count 기능 정보를 확인합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[out] ptExtCInfo      전체 Ext Pin의 Count 정보 구조체 포인터
      * @return     Enmf_FUNC_RESULT
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_ExtCountGetInfo(short nNmfNo, out TNMF_EXTC_INFO ptExtCInfo);

    /**
      * @brief      [Ext. Count] Count 기능이 시간측정모드인 경우 측정된 데이터를 확인합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nEPinNo         읽어올 Ext Pin 번호(0 ~ 7)
      * @param[out] ptCMT           Count 값을 확인할 TNMF_CMT_STS 구조체 포인터
      * @param[in]  nBufNum         버퍼(pdwRetBuf)의 개수(최대 COUNTER_MTIME_MAX_DATAS=1024개)
      * @param[out] pdwRetBuf       측정된 시간을 읽어올 배열 포인터(us)
      * @return     Enmf_FUNC_RESULT
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_ExtCountGetTime(short nNmfNo, short nEPinNo, out TNMF_CMT_STS ptCMT, short nBufNum, out uint pdwRetBuf);

    /**
      * @brief      [Ext. Count] 외부 입력 핀의 Count 기능을 해제합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nEPinNo         정지할 Ext Pin 번호(0 ~ 7)
      * @return     Enmf_FUNC_RESULT
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_ExtCountStop(short nNmfNo, short nEPinNo);

    /**
      * @brief      [Ext. Count] 외부 입력 핀에 필터 값을 설정합니다. (고속 핀만 해당)
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nWriteFlash     전원 투입 후 적용되는 필터값 적용 여부(Flash에 저장 여부)
      * @param[in]  pnFilter        필터 Index의 배열 포인터 (고속 Ext Pin 3~7)
      * @return     Enmf_FUNC_RESULT
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_ExtCountFilterSet(short nNmfNo, short nWriteFlash, short[] pnFilter);

    /**
      * @brief      [Ext. Count] 외부 입력 핀에 설정된 필터 값을 확인합니다.(고속 핀만 해당)
      * @param[in]  nNmfNo          장치번호
      * @param[out] pnRetFilter     필터 Index의 배열(고속 Ext Pin 3~7)의 배열 포인터
      * @return     Enmf_FUNC_RESULT
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_ExtCountFilterGet(short nNmfNo, short[] pnRetFilter);

    /**
      * @brief      [ AI보드 ]를 지정하여 입력 Range를 설정합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nID             보드 ID(0 ~ 7)
      * @param[in]  nRange          AI의 입력 범위 (0=±5V, 1=±10V)
      * @return     Enmf_FUNC_RESULT
      * @warning    설정 시 샘플링 데이터등 해당 보드의 정보가 초기화 됩니다.
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_AIBrdSetRange(short nNmfNo, short nID, short nRange);

    /**
      * @brief      [ AI보드 ]를 통합하여 단일 Analog 입력Pin의 샘플링 기능을 시작합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nPinNo          설정할 Pin 번호 (전체 AI Pin 개수 기준)
      * @param[in]  lRate           AD 샘플링 주기(0 ~ 100000)
      * @param[in]  nTrgType        시작 트리거 모드(0=임의 시점(즉시), 1=Trg Rising, 2=Trg Falling, 3=Trg Both)
      * @param[in]  nExtPin         시작 트리거에 사용될 External Pin 번호 (0 ~ 3)
      * @param[in]  nMemFullOvW     메모리 Full시 Overwrite여부 (0= 가장 오래된 샘플링 데이터부터 Overwrite(샘플링 정지 안함),
      *                                                          1= Overwrite안함(샘플링 정지))
      * @return     Enmf_FUNC_RESULT
      * @warning    설정 시 해당 Pin의 메모리등이 초기화 됩니다.
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_AISmplStartPin(short nNmfNo, short nPinNo, int lRate, short nTrgType, short nExtPin, short nMemFullOvW);

    /**
      * @brief      [ AI보드 ]를 통합하여 다중 Analog 입력Pin의 샘플링 기능을 시작합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nPinCount       설정할 Pin 개수(전체 AI Pin 개수 기준)
      * @param[in]  pnPinNo         AI 입력 Pin 번호 배열 포인터 (전체 AI Pin 개수 기준)
      * @param[in]  plRate          AD 샘플링 주기(0~100000) 배열 포인터
      * @param[in]  pnTrgType       시작 트리거 모드(0=임의 시점, 1=Trg Rising, 2=Trg Falling, 3=Trg Both) 배열 포인터
      * @param[in]  pnExtPin        시작 트리거에 사용될 External Pin 번호 (0 ~ 3) 배열 포인터
      * @param[in]  pnMemFullOvW    메모리 Full시 Overwrite여부 배열 포인터 (0= 가장 오래된 샘플링 데이터부터 Overwrite(샘플링 정지 안함),
      *                                                                      1= Overwrite안함(샘플링 정지))
      * @return     Enmf_FUNC_RESULT
      * @warning    AI보드 수량과 전체 Analog 입력Pin수량을 확인하십시오.
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_AISmplStartPins(short nNmfNo, short nPinCount, short[] pnPinNo, int[] plRate, short[] pnTrgType, short[] pnExtPin, short[] pnMemFullOvW);

    /**
      * @brief      [ AI보드 ]를 지정하여 단일 Analog 입력Pin의 샘플링 기능을 시작합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nID             보드 ID
      * @param[in]  nPinNo          AI 입력 Pin 번호(0 ~ 7)
      * @param[in]  lRate           AD 샘플링 주기(0 ~ 100000)
      * @param[in]  nTrgType        시작 트리거 모드(0=임의 시점, 1=Trg Rising, 2=Trg Falling, 3=Trg Both)
      * @param[in]  nExtPin         시작 트리거에 사용될 External Pin 번호 (0 ~ 3)
      * @param[in]  nMemFullOvW     메모리 Full시 Overwrite여부 (0= 가장 오래된 샘플링 데이터부터 Overwrite(샘플링 정지 안함),
      *                                                          1= Overwrite안함(샘플링 정지))
      * @return     Enmf_FUNC_RESULT
      * @warning    AI보드 ID와 Pin번호를 확인하십시오.
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_AIBrdSmplStartPin(short nNmfNo, short nID, short nPinNo, int lRate, short nTrgType, short nExtPin, short nMemFullOvW);

    /**
      * @brief      [ AI보드 ]를 지정하여 다중 Analog 입력Pin의 샘플링 기능을 시작합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  pnID            보드 ID(0 ~ 7)
      * @param[in]  nPinCount       설정할 Pin 개수 (Max. 8Pin)
      * @param[in]  pnPinNo         AI 입력 Pin 번호(0 ~ 7) 배열 포인터
      * @param[in]  plRate          AD 샘플링 주기(0~100000) 배열 포인터
      * @param[in]  pnTrgType       시작 트리거 모드(0=임의 시점, 1=Trg Rising, 2=Trg Falling, 3=Trg Both) 배열 포인터
      * @param[in]  pnExtPin        시작 트리거에 사용될 External Pin 번호 (0 ~ 3) 배열 포인터
      * @param[in]  pnMemFullOvW    메모리 Full시 Overwrite여부 배열 포인터 (0= 가장 오래된 샘플링 데이터부터 Overwrite(샘플링 정지 안함),
      *                                                                      1= Overwrite안함(샘플링 정지))
      * @return     Enmf_FUNC_RESULT
      * @warning    설정 시 해당 Pin의 메모리등이 초기화 됩니다.
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_AIBrdSmplStartPins(short nNmfNo, short nID, short nPinCount, short[] pnPinNo, int[] plRate, short[] pnTrgType, short[] pnExtPin, short[] pnMemFullOvW);

    /**
      * @brief      [ AI보드 ]를 여러개 선택하여 다중 Analog 입력Pin의 샘플링 기능을 시작합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nPinCount       설정할 Pin 개수(Max. 전체 AI Pin 개수)
      * @param[in]  pnID            보드 ID(0 ~ 7) 배열 포인터
      * @param[in]  pnPinNo         보드별 AI 입력 Pin 번호(0 ~ 7) 배열 포인터
      * @param[in]  plRate          보드별 AD 샘플링 주기(0~100000) 배열 포인터
      * @param[in]  pnTrgType       시작 트리거 모드(0=임의 시점, 1=Trg Rising, 2=Trg Falling 3=Trg Both) 배열 포인터
      * @param[in]  pnExtPin        시작 트리거에 사용될 External Pin 번호 (0 ~ 3) 배열 포인터
      * @param[in]  pnMemFullOvW    메모리 Full시 Overwrite여부 배열 포인터 (0= 가장 오래된 샘플링 데이터부터 Overwrite(샘플링 정지 안함),
      *                                                                      1= Overwrite안함(샘플링 정지))
      * @return     Enmf_FUNC_RESULT
      * @warning    설정 시 해당 Pin의 메모리등이 초기화 됩니다.
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_AImBrdSmplStartPins(short nNmfNo, short nPinCount, short[] pnID, short[] pnPinNo, int[] plRate, short[] pnTrgType, short[] pnExtPin, short[] pnMemFullOvW);

    /**
      * @brief      [ AI보드 ]를 통합하여 단일 Analog 입력Pin의 입력상태를 정지합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nPinNo          AI 입력 Pin 번호(전체 AI Pin 개수 기준)
      * @return     Enmf_FUNC_RESULT
      * @warning    AI보드 수량과 전체 Analog 입력Pin수량을 확인하십시오.
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_AISmplStopPin(short nNmfNo, short nPinNo);

    /**
      * @brief      [ AI보드 ]를 통합하여 다중 Analog 입력Pin의 입력상태를 정지합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nPinCount       설정할 Pin 개수(Max. 전체 AI Pin 개수)
      * @param[in]  pnPinNo         AI 입력 Pin 번호 배열 포인터 (전체 AI Pin 개수 기준)
      * @return     Enmf_FUNC_RESULT
      * @warning    AI보드 수량과 전체 Analog 입력Pin수량을 확인하십시오.
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_AISmplStopPins(short nNmfNo, short nPinCount, short[] pnPinNo);

    /**
      * @brief      [ AI보드 ]를 지정하여 단일 Analog 입력Pin의 입력상태를 정지합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nID             보드ID
      * @param[in]  nPinNo          AI 입력 Pin 번호(0 ~ 7)
      * @return     Enmf_FUNC_RESULT
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_AIBrdSmplStopPin(short nNmfNo, short nID, short nPinNo);

    /**
      * @brief      [ AI보드 ]를 지정하여 다중 Analog 입력Pin의 입력상태를 정지합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nID             보드 ID(0 ~ 7)
      * @param[in]  nPinCount       설정할 Pin 개수(Max. 8Pin)
      * @param[in]  pnPinNo         AI 입력 Pin 번호(0 ~ 7) 배열 포인터
      * @return     Enmf_FUNC_RESULT
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_AIBrdSmplStopPins(short nNmfNo, short nID, short nPinCount, short[] pnPinNo);

    /**
      * @brief      [ AI보드 ]를 여러개 선택하여 다중 Analog 입력Pin의 입력상태를 정지합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nPinCount       설정할 Pin 개수(Max. 전체 AI Pin 개수)
      * @param[in]  pnID            보드 ID(0 ~ 7) 배열 포인터
      * @param[in]  pnPinNo         AI 입력 Pin 번호(0 ~ 7) 배열 포인터
      * @return     Enmf_FUNC_RESULT
      * @warning    설정 시 해당 Pin의 메모리등이 초기화 됩니다.
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_AImBrdSmplStopPins(short nNmfNo, short nPinCount, short[] pnID, short[] pnPinNo);

    /**
      * @brief      [ AI보드 ]를 통합하여 단일 Analog 입력Pin의 Sampling Rate 값을 확인합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nPinNo          AI 입력 Pin 번호(전체 AI Pin 개수 기준)
      * @param[out] plRetSmplRate   해당 AI보드 Pin의 실제 Sampling Rate를 읽어올 포인터 변수
      * @return     Enmf_FUNC_RESULT
      * @warning    AI보드 수량과 전체 Analog 입력Pin수량을 확인하십시오.
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_AISmplRateGetPin(short nNmfNo, short nPinNo, out int plRetSmplRate);

    /**
      * @brief      [ AI보드 ]를 통합하여 다중 Analog 입력Pin의 Sampling Rate 값을 확인합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nPinCount       확인할 Pin 개수(Max. 전체 AI Pin 개수)
      * @param[in]  pnPinNo         AI 입력 Pin 번호 배열 포인터 (전체 AI Pin 개수 기준)
      * @param[out] plRetSmplRate   해당 AI보드 Pin의 실제 Sampling Rate를 읽어올 배열 포인터 (Pin Count)
      * @return     Enmf_FUNC_RESULT
      * @warning    AI보드 수량과 전체 Analog 입력Pin수량을 확인하십시오.
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_AISmplRateGetPins(short nNmfNo, short nPinCount, short[] pnPinNo, int[] plRetSmplRate);

    /**
      * @brief      [ AI보드 ]를 지정하여 단일 Analog 입력Pin의 Sampling Rate 값을 확인합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nID             보드 ID(0 ~ 7)
      * @param[in]  nPinNo          AI 입력 Pin 번호(0 ~ 7)
      * @param[out] plRetSmplRate   해당 AI보드 Pin의 실제 Sampling Rate를 읽어올 포인터 변수
      * @return     Enmf_FUNC_RESULT
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_AIBrdSmplRateGetPin(short nNmfNo, short nID, short nPinNo, out int plRetSmplRate);

    /**
      * @brief      [ AI보드 ]를 지정하여 다중 Analog 입력Pin의 Sampling Rate 값을 확인합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nID             보드 ID(0 ~ 7)
      * @param[in]  nPinCount       설정할 Pin 개수(Max. 8)
      * @param[in]  pnPinNo         AI 입력 Pin 번호(0 ~ 7) 배열 포인터 (Size:nPinCount)
      * @param[out] plRetSmplRate   해당 AI보드 Pin의 실제 Sampling Rate를 읽어올 배열 포인터 (Size:nPinCount)
      * @return     Enmf_FUNC_RESULT
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_AIBrdSmplRateGetPins(short nNmfNo, short nID, short nPinCount, short[] pnPinNo, int[] plRetSmplRate);

    /**
      * @brief      [ AI보드 ]를 여러개 선택하여 다중 Analog 입력Pin의 Sampling Rate 값을 확인합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nPinCount       설정할 Pin 개수 (Max. 전체 AI Pin 개수)
      * @param[in]  pnID            보드 ID(0 ~ 7) 배열 포인터
      * @param[in]  pnPinNo         보드별 AI 입력 Pin 번호(0 ~ 7) 배열 포인터 (Size:nPinCount)
      * @param[out] plRetSmplRate   해당 AI보드 Pin의 실제 Sampling Rate를 읽어올 배열 포인터 (Size:nPinCount)
      * @return     Enmf_FUNC_RESULT
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_AImBrdSmplRateGetPins(short nNmfNo, short nPinCount, short[] pnID, short[] pnPinNo, int[] plRetSmplRate);

    /**
      * @brief      [ AI보드 ]를 통합하여 다중 Analog 입력Pin의 Ext. Memory Size를 설정합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nPinCount       설정할 Pin 개수 (Max. 전체 AI Pin 개수)
      * @param[in]  pnPinNo         해당 AI보드의 Pin 번호(0 ~ 7) 배열 포인터(전체 AI Pin 개수 기준)
      * @param[in]  pnMemBCount     Pin에 할당되는 외부 Memory Block 개수의 (0~64 Block) 배열 포인터
      *                             1Block = 2048개(4KByte), 0 설정 시 외부 Memory 사용 안함
      * @return     Enmf_FUNC_RESULT
      * @warning    다수의 Pin들에 설정하는 메모리의 크기는 누적된 메모리 64개를 초과할 수 없습니다.
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_AISmplExMemSet(short nNmfNo, short nPinCount, short[] pnPinNo, short[] pnMemBCount);

    /**
      * @brief      [ AI보드 ]를 여러개 선택하여 다중 Analog 입력Pin의 Ext. Memory Size를 설정합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nPinCount       설정할 Pin 개수 (Max. 전체 AI Pin 개수)
      * @param[in]  pnID            보드 ID(0 ~ 7) 배열 포인터
      * @param[in]  pnPinNo         해당 AI보드의 Pin 번호(0 ~ 7) 배열 포인터 (Size:nPinCount)
      * @param[in]  pnMemBCount     Pin에 할당되는 외부 Memory Block 개수의 (0~64 Block) 배열 포인터 (Size:nPinCount)
      *                             1Block = 2048개(4KByte), 0 설정 시 외부 Memory 사용 안함
      * @return     Enmf_FUNC_RESULT
      * @warning    다수의 Pin들에 설정하는 메모리의 크기는 누적된 메모리 64개를 초과할 수 없습니다.
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_AIBrdSmplExMemSet(short nNmfNo, short nPinCount, short[] pnID, short[] pnPinNo, short[] pnMemBCount);

    /**
      * @brief      [ AI보드 ]를 여러개 선택하여 다중 Analog 입력Pin의 Ext. Memory Size를 설정상태를 확인합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[out] pnRetPinCount   외부 Memory가 할당된 Pin 개수 (Max. 전체 AI Pin 개수)
      * @param[out] pnRetID         보드 ID(0 ~ 7) 배열 포인터
      * @param[out] pnRetPinNo      해당 AI보드의 Pin 번호(0 ~ 7) 배열 포인터
      * @param[out] pnRetMemBCount  Pin에 할당되는 외부 Memory Block 개수의 (0~64 Block) 배열 포인터
      *                             1Block = 2048개(4KByte), 0 설정 시 외부 Memory 사용 안함
      * @param[out] pnRetMemBFill   할당된 Memory Block 중 Data로 채워진 개수의 (0~64 Block) 배열 포인터
      * @return     Enmf_FUNC_RESULT
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_AIBrdSmplExMemGet(short nNmfNo, out short pnRetPinCount, short[] pnRetID, short[] pnRetPinNo, short[] pnRetMemBCount, short[] pnRetMemBFill);

    /**
      * @brief      [ AI보드 ]를 지정하여 입력된 Sampling Data를 확인합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nID             보드 ID(0 ~ 7)
      * @param[in]  nPinNo          AI 입력 Pin 번호(0 ~ 7)
      * @param[in]  nType           AI 입력 타입 (0=전압값, 1=AD값, 2=전류값)
      * @param[out] ptAISmpl        AI 샘플링 정보 구조체 포인터
      * @param[in]  lBufNum         읽어올 데이터 버퍼 개수 (0일 경우 메모리에 있는 데이터만 취득)
      * @param[out] pdCalcValue     AI 샘플링 데이터 (Max. AI_MEM_DATAS(133120))
      * @return     Enmf_FUNC_RESULT
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_AIBrdSmplValueGetPin(short nNmfNo, short nID, short nPinNo, short nType, out TNMF_BRD_AI_SMPL ptAISmpl, int lBufNum, double[] pdCalcValue);

    /**
      * @brief      NMF의 모든 정보를 확인합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[out] ptRetAllSts     NMF의 보드의 모든 정보
      * @return     Enmf_FUNC_RESULT
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_GetBrdAllStatus(short nNmfNo, out TNMF_ALL_STS ptRetAllSts);

    /**
      * @brief      장치의 구성(Composition) 정보를 확인합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[out] ptCompo         구성 정보를 읽어올 TNMF_COMPO 구조체 포인터
      * @return     Enmf_FUNC_RESULT
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_GetCompo(short nNmfNo, out TNMF_COMPO ptCompo);

    /**
      * @brief      연결된 모든 장치의 구성(Composition) 정보를 확인합니다.
      * @param[in]  pnIp            검색할 IP 대역
      * @param[in]  nListCount      요청 개수
      * @param[out] ptCompo         구성 정보를 읽어올 TNMF_COMPO 구조체 포인터
      * @return     양수:개수, 음수:Enmf_FUNC_RESULT
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_GetCompoList(short[] pnIp, short nListCount, TNMF_COMPO[] ptCompo);

    /**
      * @brief      포트 변경을 수행합니다. (1000으로 고정된 Port를 원하는 포트로 변경)
      * @param[in]  nNmfNo          장치번호
      * @param[in]  lPortNum        변경할 포트번호
      * @return     Enmf_FUNC_RESULT
      * @warning    내부 네트워크에서 사용하는 경우, 포트변경이 필요하지 않습니다.
      *             장치의 포트 번호가 변경되는 것이 아닙니다.
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_NetPortNumSet(short nNmfNo, int lPortNum);

    /**
      * @brief      장치 제어를 위한 시리얼 통신 기능을 활성화합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nMode           기능 활성화(0=비활성화, 1=활성화)
      * @return     Enmf_FUNC_RESULT
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_SerialSetBypass(short nNmfNo, short nMode);

    /**
      * @brief      시리얼 통신 환경을 설정합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nBaud           통신속도(0=9600, 1=19200, 2=38400 bps)
      * @param[in]  nData           데이터 비트 수 (설정값 0~7 = 1~8 bit)
      * @param[in]  nStop           정지 비트 수 (0 = 1, 1 = 2)
      * @param[in]  nParity         Parity 비트 (0=None, 1=Odd, 2=Even)
      * @return     Enmf_FUNC_RESULT
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_SerialSetCfg(short nNmfNo, short nBaud, short nData, short nStop, short nParity);

    /**
      * @brief      장치로 데이터를 전송합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[in]  nLen            전송 데이터의 바이트 수
      * @param[in]  szData          전송할 내용 배열 포인터
      * @return     Enmf_FUNC_RESULT
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_SerialWrite(short nNmfNo, short nLen, byte[] szData);

    /**
      * @brief      장치가 전송한 데이터를 확인합니다.
      * @param[in]  nNmfNo          장치번호
      * @param[out] pnReadLen       수신 데이터의 바이트 수(최대 384bytes)
      * @param[out] szpRetData      수신한 내용을 받을 배열 포인터
      * @return     Enmf_FUNC_RESULT
      */
    [DllImport("NMF.dll")]
    public static extern short nmf_SerialRead(short nNmfNo, out short pnReadLen, byte[] szpRetData);

};
//------------------------------------------------------------------------------

//DESCRIPTION  'NMF Windows Dynamic Link Library'     -- *def file* description ....
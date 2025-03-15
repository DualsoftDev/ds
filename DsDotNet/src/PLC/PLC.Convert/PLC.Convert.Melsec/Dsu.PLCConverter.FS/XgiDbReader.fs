namespace Dsu.PLCConverter.FS

open System
open System.IO
open System.Reflection
open System.Collections.Generic
open FSharp.Data
open Dsu.PLCConverter.FS

type Function =
    {
        Name       : string          // strName
        FuncIndex  : int             // nFuncIndex
        ColumnProp : int             // nColCount
        FBSize     : int             // nFBSize
        Kind       : int             // nKind
        InputArity : int             // nInputCount
        InputNames : string array    // strInputName
        InputTypes : int array       // strInputType
        OutputArity: int             // nOutputCount
        OutputNames: string array    // strOutputName
        OutputTypes: int array       // strOutputType
    }
    member x.Type with get() =
        match x.FBSize with
        | 0 ->
            //assert(x.InputNames.[0] = "EN") //test ahn
            "function"
        | _ -> "function_block"

module XgiDbReader =
    [<Literal>]
    let designtimeConnectionString = 
        "Data Source=" + 
        __SOURCE_DIRECTORY__ + @"/../Data/XgiDB.sqlite;" + 
        "Version=3;foreign keys=true;Read Only=false;FailIfMissing=True;"

    //let runtimeConnectionString =
    //    let dir = Path.GetDirectoryName(Assembly.GetEntryAssembly().Location)
    //    let f = Path.Combine([|dir; "Config"; "CmdDB.sqlite"|])
    //    "Data Source=" + f + ";" + "Version=3;foreign keys=true;"
    [<Literal>]
    let designtimeFunctionConnectionString = 
        "Data Source=" + 
        __SOURCE_DIRECTORY__ + @"/../Data/CommandMapping.sqlite;" + 
        "Version=3;foreign keys=true;Read Only=false;FailIfMissing=True;"


    [<Literal>]
    let xresolutionPath = __SOURCE_DIRECTORY__ + @"/../packages/System.Data.SQLite.Core.1.0.112.0/lib/net46"
    // sql 은 객체가 아니라, type 임!!!
    type sqlCmd = SqlDataProvider<
                    DatabaseVendor   = Common.DatabaseProviderTypes.SQLITE,
                    SQLiteLibrary    = Common.SQLiteLibrary.SystemDataSQLite,
                    ConnectionString = designtimeConnectionString,
                    ResolutionPath   = xresolutionPath, // the path where System.Data.SQLite.dll is stored
                    CaseSensitivityChange = Common.CaseSensitivityChange.ORIGINAL>

   
    let ctx = sqlCmd.GetDataContext()
    let dbCmd = ctx.Main

    type sqlFunc = SqlDataProvider<
                       DatabaseVendor   = Common.DatabaseProviderTypes.SQLITE,
                       SQLiteLibrary    = Common.SQLiteLibrary.SystemDataSQLite,
                       ConnectionString = designtimeFunctionConnectionString,
                       ResolutionPath   = xresolutionPath, // the path where System.Data.SQLite.dll is stored
                       CaseSensitivityChange = Common.CaseSensitivityChange.ORIGINAL>

    let ctxFunc = sqlFunc.GetDataContext()
    let dbFunc = ctxFunc.Main.Command

    let commandDic =
        let split (line:string) = line.Split([|' '|], StringSplitOptions.RemoveEmptyEntries)
        let splitNum = split >> Array.map Int32.Parse

        //query {
        //    for r in db.CCodeIecCodeDb do
        //        select (sprintf "%A\t%A\t%A\t%A\t%A\t%A\t%A\t%A" r.StrName r.NKind r.NInputCount r.StrInputName  r.StrInputType r.NOutputCount r.StrOutputName r.StrOutputType)
        //}
        //|> Seq.iter (printfn "%s\n")

        query {
            for r in dbCmd.XgiecCodeDb do
                let record = {
                    Name        = r.StrName
                    FuncIndex   = r.NFuncIndex
                    FBSize      = r.NFbSize
                    Kind        = r.NKind
                    InputArity  = r.NInputCount
                    InputNames  = r.StrInputName  |> split
                    InputTypes  = r.StrInputType  |> splitNum
                    OutputArity = r.NOutputCount
                    OutputNames = r.StrOutputName |> split
                    OutputTypes = r.StrOutputType |> splitNum
                    ColumnProp  = r.NColCount
                }
                select (r.StrName, record)
        }
        |> dict
        |> Dictionary



(*
CREATE TABLE XGIECCodeDB (                                                                      // from RecordType.h comment
    strName            VARCHAR (100),       // TON          // ABS          // MOVE             // 
    nIndex             INT,                 // 2169         // 1            // 1587             // 중간코드
    nGroup             INT,                 // 0            // 108          // 126              // 
    nOutputGroup       INT,                 // 0            // 0            // 0                // 
    nLibType           INT,                 // 2            // 1            // 1                // NORMAL COMMAND, Function, Function Block
    nKind              INT,                 // 2003         // 1001         // 1001             // Function 종류, FB 종류 
    nFunctionCall      INT,                 // 1            // 0            // 0                // 
    nPulseCommand      INT,                 // 0            // 0            // 0                // 
    strShowList        VARCHAR (100),       // 01010001     // 01010001     // 00010001         // 
 *  nInputCount        INT,                 // 2            // 2            // 2                // 입력 개수 
    nExtend            INT,                 // 0            // 0            // 0                // 입력단 확장 여부
    nExtStartInput     INT,                 // 0            // 0            // 0                // Mux 같이 확장형 펑션에서 확장이 시작되는 입력 파라미터
 *  strInputName       VARCHAR (400),       // IN PT        // EN IN        // EN IN            //
 *  strInputType       VARCHAR (200),       // 1 16         // 1 30         // 1 100030         // VarType : BOOL=1, TIME=16, ANY=30, ???=100030 
    strInputArraySize  VARCHAR (200),       // 0            // 0            // 0                // 
 *  nOutputCount       INT,                 // 2            // 2            // 2                // 출력 개수 
 *  strOutputName      VARCHAR (140),       // Q ET         // ENO OUT      // ENO OUT          // 
 *  strOutputType      VARCHAR (120),       // 1 16         // 1 30         // 1 100030         // 
    strOutputArraySize VARCHAR (200),       // 0            // 0            // 0                // 
 *  nColCount          INT,                 // 1            // 1            // 1                // 
    dwPLCCode          VARCHAR (100),       // C0190000     // 0            // 0                // PLC 코드 
    nTrigger           INT,                 // 0            // 0            // 0                // 
 *  nFuncIndex         INT,                 // 81           // 121          // 118              // 
 *  nFBSize            INT,                 // 192          // 0            // 0                // 
    strFBOutputMap     VARCHAR (100),       // 0 32         // null         // null             // 
    strFBInputMap      VARCHAR (200),       // 15 160       // null         // null             // 
    nXgrFBSize         INT,                 // 0            // 0            // 0                // 
    strXgrFBInstMap    VARCHAR (100),       // null         // null         // null             // 
    strXgrFBInputMap   VARCHAR (200),       // null         // null         // null             // 
    dwPlcType          VARCHAR (100),       // 0x000101EF   // 0x000101EF   // 0x000101EF       // 명령어를 사용하는 PLC 기종 
    dwXGIOSVersion     VARCHAR (510),       // 0x00         // 0x00         // 0x00             // XGI 명령어 지원 OS 버젼 
    dwXGI2OSVersion    VARCHAR (510),       // 0x00         // 0x00         // 0x00             // 고성능 XGI 명령어 지원 OS 버전
    strXGBOSVersion    VARCHAR (510),       // null         // null         // null             // XGB 명령어 지원 OS 버젼 
    strXGROSVersion    VARCHAR (510)        // null         // null         // null             // XGR 명령어 지원 OS 버젼 
);
*)

(*
FNAME   : MOVE                                    | FUN명
TYPE    : function                                | 유형
INSTANCE: ,                                       | FB 일 경우 이름
INDEX   : 118                                     | INDEX로 XG5000\l.kor\cmddb.mdb의 테이블 XGIECCodeDB 상의 nFuncIndex 값
COL_PROP: 1                                       | Column 너비로 위의 mdb 상에 nColCount 값
SAFETY  : 0                                       | Safety FB 유무
// 입력
VAR_IN  : EN, 0x00200001, , 0
VAR_IN  : IN, 0x022fffff, ARRAY[0..-1] OF ANY, 0  | type변환.txt
*)

(*
    ABS 함수의 xml 저장형태

<Element ElementType="102" Coordinate="4" Param="FNAME: ABS&#xA;TYPE: function&#xA;INSTANCE: ,&#xA;INDEX: 121&#xA;COL_PROP: 1&#xA;SAFETY: 0&#xA;VAR_IN: EN, 0x00200001, , 0&#xA;VAR_IN: IN, 0x00207fe0, , 0&#xA;VAR_OUT: ENO, 0x00000001, &#xA;VAR_OUT: OUT, 0x00007fe0, &#xA;"></Element>

<Element
	ElementType="102" Coordinate="4" 
	Param="
		FNAME: ABS
		TYPE: function
		INSTANCE: ,
		INDEX: 121
		COL_PROP: 1
		SAFETY: 0
		VAR_IN: EN, 0x00200001, , 0
		VAR_IN: IN, 0x00207fe0, , 0
		VAR_OUT: ENO, 0x00000001, 
		VAR_OUT: OUT, 0x00007fe0, &#xA;"></Element>
*)

(*
입력 (VAR_IN)
    EN - BOOL       EN, 0x00200001, , 0

    IN - ANY
    IN,  0x00207fe0, , 0    // ABS, ADD
    IN,  0x00206000, , 0    // ACOS
    IN1, 0x00268000, , 0    // ADD_TIME
    IN1, 0x0020001f, , 0    // AND
    IN1, 0x0020001e, , 0    // BMOV
*)
namespace PLC.CodeGen.LS


open Engine.Core

[<AutoOpen>]
module XgiTypeConvertorModule =
    /// Statement To XGI Statements. XGI 전용 Statement 확장
    let internal s2XgiSs (prjParam: XgxProjectParams) (augs:Augments) (statement: Statement) =
        s2XgxSs prjParam augs statement

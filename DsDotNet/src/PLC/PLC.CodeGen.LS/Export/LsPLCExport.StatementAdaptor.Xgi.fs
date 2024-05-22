namespace PLC.CodeGen.LS


open Engine.Core

[<AutoOpen>]
module XgiTypeConvertorModule =
    /// XGI 전용 Statement 확장
    let internal statement2XgiStatements (prjParam: XgxProjectParams) (augs:Augments) (statement: Statement) =
        statement2XgxStatements prjParam augs statement

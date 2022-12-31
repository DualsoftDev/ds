namespace Engine.Core

[<AutoOpen>]
module RuntimeGeneratorModule =
    type RuntimeTarget = WINDOWS | XGI | XGK | AB | MELSEC
    let mutable RuntimeTarget = WINDOWS

    let nameDN() =
        match RuntimeTarget with
        | XGI -> "Q"
        | _ -> "DN"

    let nameEN() =
        match RuntimeTarget with
        | XGI -> "IN"
        | _ -> "EN"

    let namePRE() =
        match RuntimeTarget with
        | XGI -> "PT"
        | _ -> "PRE"

    let nameACC() =
        match RuntimeTarget with
        | XGI -> "ET"
        | _ -> "ACC"

    let nameTT() =
        match RuntimeTarget with
        | (WINDOWS | AB) -> "TT"
        | _ -> "_TT"

    let nameRES() = "RES"

    let nameCU() =
        match RuntimeTarget with
        | (WINDOWS | AB) -> "CU"
        | _ -> "_CU"

    let nameCD() =
        match RuntimeTarget with
        | (WINDOWS | AB) -> "CD"
        | _ -> "_CD"

    let nameOV() =
        match RuntimeTarget with
        | (WINDOWS | AB) -> "OV"
        | _ -> "_OV"

    let nameUN() =
        match RuntimeTarget with
        | (WINDOWS | AB) -> "UN"
        | _ -> "_UV"


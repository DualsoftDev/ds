namespace OPC.DSServer

open System

module RandomHelper =

    let private random = Random()

    /// <summary>
    /// v1과 v2 사이의 난수를 반환합니다.
    /// </summary>
    /// <param name="v1">최소값</param>
    /// <param name="v2">최대값</param>
    /// <returns>v1과 v2 사이의 난수를 반환</returns>
    let getRandomDouble (v1: int) (v2: int) =
        if v1 > v2 then
            invalidArg "v1" "v1은 v2보다 작거나 같아야 합니다."
        else
            random.NextDouble() * float (v2 - v1) + float v1

    /// <summary>
    /// 지정된 범위 내의 정수를 반환합니다.
    /// </summary>
    /// <param name="v1">최소값</param>
    /// <param name="v2">최대값</param>
    /// <returns>v1과 v2 사이의 정수를 반환</returns>
    let getRandomInt (v1: int) (v2: int) =
        if v1 > v2 then
            invalidArg "v1" "v1은 v2보다 작거나 같아야 합니다."
        else
            random.Next(v1, v2 + 1) // v2를 포함하기 위해 +1

    /// <summary>
    /// 지정된 확률로 true/false를 반환합니다.
    /// </summary>
    /// <param name="probability">0에서 1 사이의 확률 (0은 항상 false, 1은 항상 true)</param>
    /// <returns>확률에 따라 true/false</returns>
    let getRandomBoolean (probability: float) =
        if probability < 0.0 || probability > 1.0 then
            invalidArg "probability" "확률은 0에서 1 사이여야 합니다."
        else
            random.NextDouble() < probability


namespace Dual.Web.Blazor.Shared;

/// <summary>
/// REST API 호출 결과를 받기 위한 serializable class
/// 에러 case 는 문자열
/// Dual.Common.Core.ResultSerializable 와 동일 내용이나 Templte 기반 구현이라 상속으로 구현 불가해서 새로 생성함.
/// </summary>
/// <typeparam name="T"></typeparam>
public class RestResult<T>
{
    public bool _success { get; set; }
    /// <summary>
    /// Ok value
    /// </summary>
    public T Value { get; set; }
    /// <summary>
    /// Error value
    /// </summary>
    public string Error { get; set; }

    public RestResult()
    {
        Value = default(T);
        Error = null;
        _success = false;
    }
    internal RestResult(T v, string e, bool success)
    {
        Value = v;
        Error = e;
        _success = success;
    }

    public bool IsOk => _success;
    public bool IsError => !_success;

    public static RestResult<T> Ok(T v)
    {
        return new(v, null, true);
    }

    public static RestResult<T> Err(string e)
    {
        return new(default(T), e, false);
    }

    public static implicit operator RestResult<T>(T v) => new(v, null, true);
    public static implicit operator RestResult<T>(string e) => new(default(T), e, false);

    public R Match<R>(
            Func<T, R> success,
            Func<string, R> failure) =>
        _success ? success(Value) : failure(Error);

    public void Iter(
            Action<T> success,
            Action<string> failure)
    {
        if (_success)
            success(Value);
        else
            failure(Error);
    }

    public async Task IterAsync(
            Func<T, Task> success,
            Func<string, Task> failure)
    {
        if (_success)
            await success(Value);
        else
            await failure(Error);
    }
}

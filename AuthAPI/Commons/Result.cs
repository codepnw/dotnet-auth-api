namespace AuthAPI.Commons;

public enum ErrorCode
{
    None = 0,
    Ok = 200,
    Created = 201,

    BadRequest = 400,
    Unauthorized = 401,
    Forbidden = 403,
    NotFound = 404,
    Conflict = 409
}

public class Result<T>(bool isSuccess, T? data, string? errorMessage, ErrorCode errorCode)
{
    public readonly bool IsSuccess = isSuccess;
    public readonly T? Data = data;
    public readonly string? ErrorMessage = errorMessage;
    public readonly ErrorCode ErrorCode = errorCode;

    public static Result<T> Success(T data) =>
        new(true, data, null, ErrorCode.None);

    public static Result<T> Failure(string errorMessage, ErrorCode errorCode) =>
        new(false, default, errorMessage, errorCode);
}

public class Result(bool isSuccess, string? errorMessage, int errorCode)
{
    public readonly bool IsSuccess = isSuccess;
    public readonly string? ErrorMessage = errorMessage;
    public readonly int ErrorCode = errorCode;

    public static Result Success() =>
        new(true, null, 0);

    public static Result Failure(string errorMessage, int errorCode) =>
        new(false, errorMessage, errorCode);
}
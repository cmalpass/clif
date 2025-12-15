namespace CLIF.Core;

public class OperationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public Exception? Exception { get; set; }
    public object? Data { get; set; }

    public static OperationResult Ok(string message = "", object? data = null)
    {
        return new OperationResult
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    public static OperationResult Fail(string message, Exception? ex = null)
    {
        return new OperationResult
        {
            Success = false,
            Message = message,
            Exception = ex
        };
    }
}

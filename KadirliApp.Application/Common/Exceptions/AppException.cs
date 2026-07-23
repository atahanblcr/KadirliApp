namespace KadirliApp.Application.Common.Exceptions;

public class AppException : Exception
{
    public string Code { get; }
    
    public AppException(string message, string code = "INTERNAL_ERROR") : base(message)
    {
        Code = code;
    }
}

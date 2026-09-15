using System.Data.SqlTypes;
using Error = Domain.Enums.ErrorType;
namespace Infrastructure.Responses
{
    public class Result<T> where T : class
    {
        public bool IsSuccess { get; init; }
        public T? Context { get; init; }
        public string? ErrorMessage { get; init; }
        public Error? ErrorType { get; init; }
        public Result (bool isSuccess, T? context, string? errorMessage, Error? errorType) {
            IsSuccess = isSuccess;
            Context = context;
            ErrorMessage = errorMessage;
            ErrorType = errorType;
        }
        public static Result<T> Success(T data) 
            => new Result<T>(true, data, null, null);
        public static Result<T> Fail(string errorMessage, Error errorType)
            => new Result<T>(false, null, errorMessage, errorType);
    }
}
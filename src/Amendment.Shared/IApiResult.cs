using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Amendment.Shared
{
    public interface IApiResult
    {
        HttpStatusCode StatusCode { get; set; }
        bool IsSuccess { get; set; }
    }

    public interface IApiResult<T> : IApiResult
    {
        HttpStatusCode StatusCode { get; set; }
        bool IsSuccess { get; set; }
    }

    public sealed class ApiCommandSuccessResult : IApiResult
    {
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        public bool IsSuccess { get; set; } = true;
    }

    public sealed class ApiCommandFailedResult : IApiResult
    {
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.BadRequest;
        public bool IsSuccess { get; set; } = true;
    }

    public sealed class ApiSuccessResult<T> : IApiResult<T>
    {
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;
        public bool IsSuccess { get; set; } = true;
        public T Result { get; set; }

        public ApiSuccessResult(T result)
        {
            Result = result;
        }
    }

    public sealed class ApiFailedResult : IApiResult
    {
        public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.BadRequest;
        public bool IsSuccess { get; set; } = false;
        public List<ValidationError> Errors { get; set; }// = Enumerable.Empty<ValidationError>();

        public ApiFailedResult()
        {
        }

        public ApiFailedResult(IEnumerable<ValidationError> errors)
        {
            Errors = errors.ToList();
        }

        public ApiFailedResult(HttpStatusCode statusCode)
        {
            StatusCode = statusCode;
        }

        public ApiFailedResult(IEnumerable<ValidationError> errors, HttpStatusCode statusCode)
        {
            Errors = errors.ToList();
            StatusCode = statusCode;
        }
    }

    public struct ValidationError
    {
        public string Name { get; set; }
        public string Message { get; set; }
    }
}

//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net;
//using System.Text;
//using System.Threading.Tasks;

//namespace Amendment.Shared
//{
//    public interface IApiResult
//    {
//        HttpStatusCode StatusCode { get; }
//        bool IsSuccess { get; }
//    }

//    public interface IApiResult<T> : IApiResult
//    {
//        HttpStatusCode StatusCode { get; }
//        bool IsSuccess { get; }
//    }

//    public sealed class ApiCommandSuccessResult : IApiResult
//    {
//        public HttpStatusCode StatusCode { get; } = HttpStatusCode.OK;
//        public bool IsSuccess { get; } = true;
//    }

//    public sealed class ApiCommandFailedResult : IApiResult
//    {
//        public HttpStatusCode StatusCode { get; } = HttpStatusCode.BadRequest;
//        public bool IsSuccess { get; } = true;
//    }

//    public sealed class ApiSuccessResult<T> : IApiResult<T>
//    {
//        public HttpStatusCode StatusCode { get; } = HttpStatusCode.OK;
//        public bool IsSuccess { get;} = true;
//        public T Result { get; }

//        public ApiSuccessResult(T result)
//        {
//            Result = result;
//        }
//    }

//    public sealed class ApiFailedResult : IApiResult
//    {
//        public HttpStatusCode StatusCode { get; } = HttpStatusCode.BadRequest;
//        public bool IsSuccess { get; } = false;
//        public IEnumerable<ValidationError> Errors { get; } = new List<ValidationError>();
        
//        public ApiFailedResult()
//        {
//        }

//        public ApiFailedResult(IEnumerable<ValidationError> errors)
//        {
//            Errors = errors;
//        }

//        public ApiFailedResult(IEnumerable<ValidationError> errors, HttpStatusCode statusCode)
//        {
//            Errors = errors;
//            StatusCode = statusCode;
//        }
//    }

//    public struct ValidationError
//    {
//        public string Name { get; set; }
//        public string Message { get; set; }
//    }
//}

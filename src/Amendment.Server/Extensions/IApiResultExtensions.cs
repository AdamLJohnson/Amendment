using System.Net;
using Amendment.Shared;

namespace Amendment.Server.Extensions
{
    public static class IApiResultExtensions
    {
        public static IResult ToResult(this IApiResult result)
        {
            switch (result.StatusCode)
            {
                case HttpStatusCode.OK:
                    return Results.Ok(result);
                case HttpStatusCode.BadRequest:
                    return Results.BadRequest(result);
                case HttpStatusCode.Unauthorized:
                    return Results.Unauthorized();
                case HttpStatusCode.NotFound:
                    return Results.NotFound(result);
                default:
                    throw new NotImplementedException("That status code is not supported by 'ToResults' yet.");
            }
        }

        public static IResult ToResult(this IApiResult result, string uri)
        {
            switch (result.StatusCode)
            {
                case HttpStatusCode.Created:
                    return Results.Created(uri, result);
                default:
                    return result.ToResult();
            }
        }
    }
}

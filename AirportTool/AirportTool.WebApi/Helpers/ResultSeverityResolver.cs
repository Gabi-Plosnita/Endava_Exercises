using AirportTool.Application;
using System.Net;

namespace AirportTool.WebApi;

public interface IResultSeverityResolver
{
    ErrorType? GetSeverity(Result result);

    int GetHttpStatusCode(Result result);
}

public class ResultSeverityResolver : IResultSeverityResolver
{
    private static readonly ErrorType[] SeverityOrder =
    {
        ErrorType.Unexpected,
        ErrorType.Conflict,
        ErrorType.NotFound,
        ErrorType.Validation
    };

    public ErrorType? GetSeverity(Result result)
    {
        if (result == null || result.Errors.Count == 0)
        {
            return null;
        }

        foreach (var severity in SeverityOrder)
        {
            if (result.Errors.Any(e => e.Type == severity))
                return severity;
        }

        return ErrorType.Unexpected;
    }

    public HttpStatusCode GetHttpStatusCode(Result result)
    {
        var severity = GetSeverity(result);

        if (severity is null)
        {
            return HttpStatusCode.OK;
        }

        return severity switch
        {
            ErrorType.Validation => HttpStatusCode.BadRequest,              
            ErrorType.NotFound => HttpStatusCode.NotFound,                
            ErrorType.Conflict => HttpStatusCode.Conflict,                
            ErrorType.Unexpected => HttpStatusCode.InternalServerError,      
            _ => HttpStatusCode.InternalServerError
        };
    }
}

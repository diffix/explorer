namespace Explorer.Api
{
    using System;

    using Aircloak.JsonApi.Exceptions;
    using Microsoft.AspNetCore.Mvc;

    public class AircloakApiProblemDetails : ProblemDetails
    {
        public AircloakApiProblemDetails(AircloakException aircloakException)
        {
            Title = aircloakException.Message;
            AircloakErrorContext = aircloakException switch
            {
                ApiException e => ApiExceptionContext(e),
                ResultException e => ResultExceptionContext(e),
                QueryException e => QueryExceptionContext(e),
                _ => new { },
            };
        }

        public object AircloakErrorContext { get; }

        private static object QueryExceptionContext(QueryException e) => new
        {
            e.Message,
        };

        private static object ApiExceptionContext(ApiException e) => new
        {
            e.EndPoint,
            e.ErrorDescription,
            e.Method,
            e.ResponseStatus,
        };

        private static object ResultExceptionContext(ResultException e) => new
        {
            e.QueryStatement,
            e.QueryState,
            e.QueryError,
        };
    }
}
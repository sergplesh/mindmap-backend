namespace KnowledgeMap.Backend.Services
{
    public enum ServiceResultType
    {
        Success,
        Created,
        BadRequest,
        NotFound,
        Forbidden,
        Unauthorized
    }

    public sealed class ServiceResult
    {
        private ServiceResult(ServiceResultType type, object? value = null, object? routeValues = null)
        {
            Type = type;
            Value = value;
            RouteValues = routeValues;
        }

        public ServiceResultType Type { get; }

        public object? Value { get; }

        public object? RouteValues { get; }

        public bool IsSuccess => Type == ServiceResultType.Success || Type == ServiceResultType.Created;

        public static ServiceResult Success(object? value = null) => new(ServiceResultType.Success, value);

        public static ServiceResult Created(object value, object routeValues) => new(ServiceResultType.Created, value, routeValues);

        public static ServiceResult BadRequest(object? value = null) => new(ServiceResultType.BadRequest, value);

        public static ServiceResult NotFound(object? value = null) => new(ServiceResultType.NotFound, value);

        public static ServiceResult Forbidden(object? value = null) => new(ServiceResultType.Forbidden, value);

        public static ServiceResult Unauthorized(object? value = null) => new(ServiceResultType.Unauthorized, value);
    }
}

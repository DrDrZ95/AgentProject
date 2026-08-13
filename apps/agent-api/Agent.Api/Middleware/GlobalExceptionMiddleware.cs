namespace Agent.Api.Middleware;

/// <summary>
/// Global middleware for handling exceptions, logging them, and returning a standardized error response.
/// 全局异常处理中间件，用于捕获异常、记录日志并返回标准化的错误响应。
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            if (context.Response.HasStarted)
            {
                _logger.LogWarning(ex,
                    "An unhandled exception occurred after the response started. [TraceId: {TraceId}]",
                    context.TraceIdentifier);
                throw;
            }

            context.Response.Clear();
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // 处理外部组件异常的控制台高亮输出
        ExternalComponentLogger.HandleException(exception);

        RecordExceptionSpan(exception);

        var (statusCode, errorCode, message) = CategorizeException(exception);

        // 记录详细日志
        _logger.LogError(exception,
            "Unhandled exception for {Method} {Path}. [TraceId: {TraceId}] [ErrorCode: {ErrorCode}] [StatusCode: {StatusCode}]",
            context.Request.Method, context.Request.Path, context.TraceIdentifier, errorCode, (int)statusCode);

        // Elastic APM 集成（如果已安装 SDK）
        // Elastic.Apm.Agent.Tracer.CurrentTransaction?.CaptureException(exception);

        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)statusCode;

        var response = new ApiResponse<object>
        {
            Success = false,
            Message = message,
            ErrorCode = errorCode,
            RequestId = context.TraceIdentifier
        };

        // 如果是开发环境，可以返回更详细的堆栈信息
        // if (env.IsDevelopment()) { ... }

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(response),
            context.RequestAborted);
    }

    private void RecordExceptionSpan(Exception exception)
    {
        var activity = Activity.Current;
        if (activity != null)
        {
            activity.SetStatus(ActivityStatusCode.Error, exception.Message);
        }
    }

    private (HttpStatusCode statusCode, string errorCode, string message) CategorizeException(Exception exception)
    {
        return exception switch
        {
            // 参数错误：通常由请求校验失败引发
            ArgumentException or ArgumentNullException =>
                (HttpStatusCode.BadRequest, "BAD_REQUEST_PARAMS", "请求参数无效"),

            // 认证错误：未登录或 Token 失效
            UnauthorizedAccessException =>
                (HttpStatusCode.Unauthorized, "UNAUTHORIZED_ACCESS", "未授权访问，请重新登录"),

            // 权限错误：已登录但无权访问该资源
            SecurityException =>
                (HttpStatusCode.Forbidden, "FORBIDDEN_ACCESS", "权限不足，拒绝访问"),

            // 资源未找到
            KeyNotFoundException =>
                (HttpStatusCode.NotFound, "RESOURCE_NOT_FOUND", "请求的资源不存在"),

            // 业务逻辑冲突：如重复注册、状态不符合操作要求
            InvalidOperationException =>
                (HttpStatusCode.Conflict, "BUSINESS_CONFLICT", "操作冲突或当前状态不允许该操作"),

            // 数据库并发/约束错误
            DbUpdateException =>
                (HttpStatusCode.InternalServerError, "DATABASE_ERROR", "数据持久化失败，请检查约束条件"),

            // 超时错误：下游服务或数据库响应过慢
            TimeoutException =>
                (HttpStatusCode.GatewayTimeout, "SERVICE_TIMEOUT", "系统响应超时，请稍后重试"),

            // 其他未知内部错误
            _ => (HttpStatusCode.InternalServerError, "INTERNAL_SERVER_ERROR", "系统内部异常，请联系管理员")
        };
    }
}


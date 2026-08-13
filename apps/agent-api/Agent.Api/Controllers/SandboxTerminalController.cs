namespace Agent.Api.Controllers;

/// <summary>
/// Sandbox Terminal Controller
/// 沙盒终端控制器
/// 
/// 提供本地沙盒环境的终端操作API
/// Provides terminal operation APIs for local sandbox environment
/// 
/// 基于AI-Agent项目的sandbox.py和terminal.py功能转换
/// Converted from AI-Agent project's sandbox.py and terminal.py functionality
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
[Produces("application/json")]
[RequirePermission("system.admin")]
public class SandboxTerminalController : ControllerBase
{
    private readonly ISandboxTerminalService _terminalService;
    private readonly ILogger<SandboxTerminalController> _logger;

    public SandboxTerminalController(
        ISandboxTerminalService terminalService,
        ILogger<SandboxTerminalController> logger)
    {
        _terminalService = terminalService;
        _logger = logger;
    }

    /// <summary>
    /// Execute a command in the sandbox environment
    /// 在沙盒环境中执行命令
    /// </summary>
    /// <param name="request">Command execution request - 命令执行请求</param>
    /// <returns>Command execution result - 命令执行结果</returns>
    [HttpPost("execute")]
    [SwaggerOperation(
        Summary = "Execute command",
        Description = "Executes a shell command in the sandbox environment.",
        OperationId = "ExecuteCommand",
        Tags = new[] { "Sandbox" }
    )]
    [ProducesResponseType(typeof(ApiResponse<SandboxCommandResult>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ValidationErrorResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ErrorResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ErrorResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SandboxCommandResult>>> ExecuteCommand(
        [FromBody] ExecuteCommandRequest request)
    {
        try
        {
            _logger.LogInformation("Received sandbox command execution request");

            // 验证请求 - Validate request
            if (string.IsNullOrWhiteSpace(request.Command))
            {
                return BadRequest(ApiResponse<SandboxCommandResult>.Fail("Command cannot be empty"));
            }

            // 执行命令 - Execute command
            var result = await _terminalService.ExecuteCommandAsync(
                request.Command,
                request.WorkingDirectory,
                request.Timeout,
                HttpContext.RequestAborted);

            return Ok(ApiResponse<SandboxCommandResult>.Ok(result));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute sandbox command");
            return StatusCode(500, ApiResponse<SandboxCommandResult>.Fail("Command execution failed"));
        }
    }

    /// <summary>
    /// Execute a command with streaming output
    /// 执行命令并流式输出结果
    /// 
    /// 模拟AI-Agent的异步终端输出功能
    /// Simulates AI-Agent async terminal output functionality
    /// </summary>
    /// <param name="request">Streaming command request - 流式命令请求</param>
    /// <returns>Streaming command output - 流式命令输出</returns>
    [HttpPost("execute/stream")]
    [SwaggerOperation(
        Summary = "Execute command with streaming output",
        Description = "Executes a shell command and streams the output in real-time.",
        OperationId = "ExecuteCommandStream",
        Tags = new[] { "Sandbox" }
    )]
    [Produces("text/plain")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ValidationErrorResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ErrorResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> ExecuteCommandStream(
        [FromBody] ExecuteCommandRequest request)
    {
        try
        {
            _logger.LogInformation("Received streaming sandbox command request");

            // 验证请求 - Validate request
            if (string.IsNullOrWhiteSpace(request.Command))
            {
                return BadRequest(ApiResponse<object>.Fail("Command cannot be empty"));
            }

            // 设置流式响应 - Setup streaming response
            Response.ContentType = "text/plain; charset=utf-8";
            Response.Headers.Append("Cache-Control", "no-cache");
            Response.Headers.Append("Connection", "keep-alive");

            // 流式执行命令 - Stream command execution
            await foreach (var output in _terminalService.ExecuteCommandStreamAsync(
                request.Command,
                request.WorkingDirectory,
                request.Timeout,
                HttpContext.RequestAborted))
            {
                await Response.WriteAsync($"{output}\n", HttpContext.RequestAborted);
                await Response.Body.FlushAsync(HttpContext.RequestAborted);
            }

            return new EmptyResult();
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Streaming sandbox command was cancelled");
            return new EmptyResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute streaming sandbox command");
            // If headers are already sent, we can't change status code, so just write error to stream
            if (!Response.HasStarted)
            {
                return StatusCode(500, ApiResponse<object>.Fail("Command execution failed"));
            }
            await Response.WriteAsync("Error: command execution failed\n");
            return new EmptyResult();
        }
    }

    /// <summary>
    /// Get current working directory
    /// 获取当前工作目录
    /// </summary>
    /// <returns>Current working directory - 当前工作目录</returns>
    [HttpGet("workdir")]
    [SwaggerOperation(
        Summary = "Get working directory",
        Description = "Retrieves the current working directory of the sandbox terminal.",
        OperationId = "GetWorkingDirectory",
        Tags = new[] { "Sandbox" }
    )]
    [ProducesResponseType(typeof(ApiResponse<WorkingDirectoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ErrorResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ErrorResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<WorkingDirectoryResponse>>> GetWorkingDirectory()
    {
        try
        {
            var workingDir = await _terminalService.GetWorkingDirectoryAsync();
            return Ok(ApiResponse<WorkingDirectoryResponse>.Ok(new WorkingDirectoryResponse { WorkingDirectory = workingDir }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get working directory");
            return StatusCode(500, ApiResponse<WorkingDirectoryResponse>.Fail("Failed to get working directory"));
        }
    }

    /// <summary>
    /// Set working directory
    /// 设置工作目录
    /// </summary>
    /// <param name="request">Working directory request - 工作目录请求</param>
    /// <returns>Success status - 成功状态</returns>
    [HttpPost("workdir")]
    [SwaggerOperation(
        Summary = "Set working directory",
        Description = "Sets the working directory for the sandbox terminal.",
        OperationId = "SetWorkingDirectory",
        Tags = new[] { "Sandbox" }
    )]
    [ProducesResponseType(typeof(ApiResponse<SetWorkingDirectoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ValidationErrorResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ErrorResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ErrorResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SetWorkingDirectoryResponse>>> SetWorkingDirectory(
        [FromBody] SetWorkingDirectoryRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Path))
            {
                return BadRequest(ApiResponse<SetWorkingDirectoryResponse>.Fail("Path cannot be empty"));
            }

            var success = await _terminalService.SetWorkingDirectoryAsync(request.Path);

            if (!success)
            {
                return BadRequest(ApiResponse<SetWorkingDirectoryResponse>.Fail("The specified path is invalid or inaccessible"));
            }

            return Ok(ApiResponse<SetWorkingDirectoryResponse>.Ok(new SetWorkingDirectoryResponse
            {
                Success = true,
                WorkingDirectory = await _terminalService.GetWorkingDirectoryAsync()
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set working directory: {Path}", request.Path);
            return StatusCode(500, ApiResponse<SetWorkingDirectoryResponse>.Fail("Failed to set working directory"));
        }
    }

    /// <summary>
    /// Check if a command is safe to execute
    /// 检查命令是否安全可执行
    /// </summary>
    /// <param name="request">Command safety check request - 命令安全检查请求</param>
    /// <returns>Safety check result - 安全检查结果</returns>
    [HttpPost("check-command")]
    [SwaggerOperation(
        Summary = "Check command safety",
        Description = "Analyzes a command to determine if it is safe to execute in the sandbox.",
        OperationId = "CheckCommandSafety",
        Tags = new[] { "Sandbox" }
    )]
    [ProducesResponseType(typeof(ApiResponse<CommandSafetyResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ValidationErrorResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ErrorResponse>), StatusCodes.Status500InternalServerError)]
    public ActionResult<ApiResponse<CommandSafetyResponse>> CheckCommandSafety(
        [FromBody] CommandSafetyRequest request)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(request.Command))
            {
                return BadRequest(ApiResponse<CommandSafetyResponse>.Fail("Command cannot be empty"));
            }

            var isSafe = _terminalService.IsCommandSafe(request.Command);

            return Ok(ApiResponse<CommandSafetyResponse>.Ok(new CommandSafetyResponse
            {
                IsSafe = isSafe,
                Command = request.Command,
                Message = isSafe ? "Command is safe to execute" : "Command contains potentially dangerous operations"
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check sandbox command safety");
            return StatusCode(500, ApiResponse<CommandSafetyResponse>.Fail("Failed to check command safety"));
        }
    }

    /// <summary>
    /// Get system information
    /// 获取系统信息
    /// </summary>
    /// <returns>System information - 系统信息</returns>
    [HttpGet("system-info")]
    [SwaggerOperation(
        Summary = "Get system information",
        Description = "Retrieves information about the sandbox system environment.",
        OperationId = "GetSystemInfo",
        Tags = new[] { "Sandbox" }
    )]
    [ProducesResponseType(typeof(ApiResponse<SandboxSystemInfo>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ErrorResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<SandboxSystemInfo>>> GetSystemInfo()
    {
        try
        {
            var systemInfo = await _terminalService.GetSystemInfoAsync();
            return Ok(ApiResponse<SandboxSystemInfo>.Ok(systemInfo));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get system information");
            return StatusCode(500, ApiResponse<SandboxSystemInfo>.Fail("Failed to get system information"));
        }
    }

    /// <summary>
    /// Get terminal health status
    /// 获取终端健康状态
    /// </summary>
    /// <returns>Health status - 健康状态</returns>
    [HttpGet("health")]
    [SwaggerOperation(
        Summary = "Get health status",
        Description = "Checks the health status of the sandbox terminal service.",
        OperationId = "GetHealth",
        Tags = new[] { "Sandbox" }
    )]
    [ProducesResponseType(typeof(ApiResponse<TerminalHealthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ErrorResponse>), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<TerminalHealthResponse>>> GetHealth()
    {
        try
        {
            // 执行简单的健康检查命令 - Execute simple health check command
            var result = await _terminalService.ExecuteCommandAsync(
                "echo 'health-check'",
                timeout: 5,
                cancellationToken: HttpContext.RequestAborted);

            var isHealthy = result.IsSuccess && result.StandardOutput.Contains("health-check");

            return Ok(ApiResponse<TerminalHealthResponse>.Ok(new TerminalHealthResponse
            {
                IsHealthy = isHealthy,
                Status = isHealthy ? "Healthy" : "Unhealthy",
                LastChecked = DateTime.UtcNow,
                Details = isHealthy ? "Terminal service is responding normally" : "Terminal service is not responding properly"
            }));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed");
            return Ok(ApiResponse<TerminalHealthResponse>.Ok(new TerminalHealthResponse
            {
                IsHealthy = false,
                Status = "Unhealthy",
                LastChecked = DateTime.UtcNow,
                Details = "Terminal health check failed"
            }));
        }
    }
}

#region Request/Response Models - 请求/响应模型

/// <summary>
/// Command execution request
/// 命令执行请求
/// </summary>
public class ExecuteCommandRequest
{
    /// <summary>
    /// Command to execute - 要执行的命令
    /// </summary>
    [Required]
    [StringLength(1000, ErrorMessage = "Command length cannot exceed 1000 characters")]
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Working directory (optional) - 工作目录（可选）
    /// </summary>
    [StringLength(500, ErrorMessage = "Working directory path cannot exceed 500 characters")]
    public string? WorkingDirectory { get; set; }

    /// <summary>
    /// Timeout in seconds (default: 30) - 超时时间秒数（默认：30）
    /// </summary>
    [Range(1, 300, ErrorMessage = "Timeout must be between 1 and 300 seconds")]
    public int Timeout { get; set; } = 30;
}

/// <summary>
/// Working directory response
/// 工作目录响应
/// </summary>
public class WorkingDirectoryResponse
{
    /// <summary>
    /// Current working directory - 当前工作目录
    /// </summary>
    public string WorkingDirectory { get; set; } = string.Empty;
}

/// <summary>
/// Set working directory request
/// 设置工作目录请求
/// </summary>
public class SetWorkingDirectoryRequest
{
    /// <summary>
    /// Directory path - 目录路径
    /// </summary>
    [Required]
    [StringLength(500, ErrorMessage = "Path cannot exceed 500 characters")]
    public string Path { get; set; } = string.Empty;
}

/// <summary>
/// Set working directory response
/// 设置工作目录响应
/// </summary>
public class SetWorkingDirectoryResponse
{
    /// <summary>
    /// Success status - 成功状态
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Current working directory after change - 更改后的当前工作目录
    /// </summary>
    public string WorkingDirectory { get; set; } = string.Empty;
}

/// <summary>
/// Command safety check request
/// 命令安全检查请求
/// </summary>
public class CommandSafetyRequest
{
    /// <summary>
    /// Command to check - 要检查的命令
    /// </summary>
    [Required]
    [StringLength(1000, ErrorMessage = "Command length cannot exceed 1000 characters")]
    public string Command { get; set; } = string.Empty;
}

/// <summary>
/// Command safety check response
/// 命令安全检查响应
/// </summary>
public class CommandSafetyResponse
{
    /// <summary>
    /// Whether the command is safe - 命令是否安全
    /// </summary>
    public bool IsSafe { get; set; }

    /// <summary>
    /// Original command - 原始命令
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Safety check message - 安全检查消息
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Terminal health response
/// 终端健康响应
/// </summary>
public class TerminalHealthResponse
{
    /// <summary>
    /// Whether the terminal is healthy - 终端是否健康
    /// </summary>
    public bool IsHealthy { get; set; }

    /// <summary>
    /// Health status - 健康状态
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Last health check time - 最后健康检查时间
    /// </summary>
    public DateTime LastChecked { get; set; }

    /// <summary>
    /// Health check details - 健康检查详情
    /// </summary>
    public string Details { get; set; } = string.Empty;
}

#endregion


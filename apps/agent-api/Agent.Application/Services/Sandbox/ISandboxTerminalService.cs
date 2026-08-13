namespace Agent.Application.Services.Sandbox;

/// <summary>
/// Interface for sandbox terminal operations
/// 沙盒终端操作接口
/// </summary>
public interface ISandboxTerminalService
{
    /// <summary>
    /// Execute a command in the local sandbox environment
    /// 在本地沙盒环境中执行命令
    /// </summary>
    /// <param name="command">Command to execute - 要执行的命令</param>
    /// <param name="workingDirectory">Working directory - 工作目录</param>
    /// <param name="timeout">Timeout in seconds - 超时时间（秒）</param>
    /// <param name="cancellationToken">Cancellation token - 取消令牌</param>
    /// <returns>Command execution result - 命令执行结果</returns>
    Task<SandboxCommandResult> ExecuteCommandAsync(
        string command,
        string? workingDirectory = null,
        int timeout = 30,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Execute a command with streaming output
    /// 执行命令并流式输出结果
    /// </summary>
    /// <param name="command">Command to execute - 要执行的命令</param>
    /// <param name="workingDirectory">Working directory - 工作目录</param>
    /// <param name="timeout">Timeout in seconds - 超时时间（秒）</param>
    /// <param name="cancellationToken">Cancellation token - 取消令牌</param>
    /// <returns>Streaming command output - 流式命令输出</returns>
    IAsyncEnumerable<string> ExecuteCommandStreamAsync(
        string command,
        string? workingDirectory = null,
        int timeout = 30,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get current working directory
    /// 获取当前工作目录
    /// </summary>
    /// <returns>Current working directory path - 当前工作目录路径</returns>
    Task<string> GetWorkingDirectoryAsync();

    /// <summary>
    /// Set working directory
    /// 设置工作目录
    /// </summary>
    /// <param name="path">Directory path - 目录路径</param>
    /// <returns>Success status - 成功状态</returns>
    Task<bool> SetWorkingDirectoryAsync(string path);

    /// <summary>
    /// Check if a command is safe to execute
    /// 检查命令是否安全可执行
    /// </summary>
    /// <param name="command">Command to check - 要检查的命令</param>
    /// <returns>True if safe, false otherwise - 安全返回true，否则返回false</returns>
    bool IsCommandSafe(string command);

    /// <summary>
    /// Get system information
    /// 获取系统信息
    /// </summary>
    /// <returns>System information - 系统信息</returns>
    Task<SandboxSystemInfo> GetSystemInfoAsync();
}

/// <summary>
/// Command execution result
/// 命令执行结果
/// </summary>
public class SandboxCommandResult
{
    /// <summary>
    /// Exit code of the command
    /// 命令的退出代码
    /// </summary>
    public int ExitCode { get; set; }

    /// <summary>
    /// Standard output
    /// 标准输出
    /// </summary>
    public string StandardOutput { get; set; } = string.Empty;

    /// <summary>
    /// Standard error output
    /// 标准错误输出
    /// </summary>
    public string StandardError { get; set; } = string.Empty;

    /// <summary>
    /// Execution time in milliseconds
    /// 执行时间（毫秒）
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// Whether the command was successful (exit code 0)
    /// 命令是否成功执行（退出代码为0）
    /// </summary>
    public bool IsSuccess => ExitCode == 0;

    /// <summary>
    /// Combined output (stdout + stderr)
    /// 合并输出（标准输出 + 标准错误）
    /// </summary>
    public string CombinedOutput => $"{StandardOutput}\n{StandardError}".Trim();
}

/// <summary>
/// System information for the sandbox environment
/// 沙盒环境的系统信息
/// </summary>
public class SandboxSystemInfo
{
    /// <summary>
    /// Operating system name
    /// 操作系统名称
    /// </summary>
    public string OperatingSystem { get; set; } = string.Empty;

    /// <summary>
    /// System architecture
    /// 系统架构
    /// </summary>
    public string Architecture { get; set; } = string.Empty;

    /// <summary>
    /// Available memory in MB
    /// 可用内存（MB）
    /// </summary>
    public long AvailableMemoryMB { get; set; }

    /// <summary>
    /// CPU core count
    /// CPU核心数
    /// </summary>
    public int CpuCoreCount { get; set; }

    /// <summary>
    /// Current user
    /// 当前用户
    /// </summary>
    public string CurrentUser { get; set; } = string.Empty;

    /// <summary>
    /// Home directory
    /// 主目录
    /// </summary>
    public string HomeDirectory { get; set; } = string.Empty;

    /// <summary>
    /// Environment variables explicitly safe to expose.
    /// 可安全公开的环境变量。宿主进程环境变量不会通过 API 返回。
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new();
}

/// <summary>
/// Sandbox terminal configuration options
/// 沙盒终端配置选项
/// </summary>
public class SandboxTerminalOptions
{
    /// <summary>
    /// Default working directory
    /// 默认工作目录
    /// </summary>
    public string DefaultWorkingDirectory { get; set; } = "/tmp";

    /// <summary>
    /// Default command timeout in seconds
    /// 默认命令超时时间（秒）
    /// </summary>
    public int DefaultTimeout { get; set; } = 30;

    /// <summary>
    /// Maximum command timeout in seconds
    /// 最大命令超时时间（秒）
    /// </summary>
    public int MaxTimeout { get; set; } = 300;

    /// <summary>
    /// Allowed commands (if empty, all commands are allowed)
    /// 允许的命令（如果为空，则允许所有命令）
    /// </summary>
    public List<string> AllowedCommands { get; set; } = new();

    /// <summary>
    /// Blocked commands (these commands will be rejected)
    /// 阻止的命令（这些命令将被拒绝）
    /// </summary>
    public List<string> BlockedCommands { get; set; } = new()
    {
        "rm -rf /",
        "rm -rf /*",
        "mkfs",
        "dd if=/dev/zero",
        ":(){ :|:& };:",
        "chmod -R 777 /",
        "chown -R root /",
        "sudo rm -rf",
        "format",
        "fdisk",
        "parted"
    };

    /// <summary>
    /// Environment variables to set for commands
    /// 为命令设置的环境变量
    /// </summary>
    public Dictionary<string, string> EnvironmentVariables { get; set; } = new()
    {
        ["TERM"] = "xterm-256color",
        ["LANG"] = "en_US.UTF-8",
        ["LC_ALL"] = "en_US.UTF-8"
    };

    /// <summary>
    /// Enable command logging
    /// 启用命令日志记录
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Enable command sanitization
    /// 启用命令清理
    /// </summary>
    public bool EnableSanitization { get; set; } = true;

    /// <summary>
    /// Resource limit: Maximum memory in MB
    /// 资源限制：最大内存（MB）
    /// </summary>
    public long MaxMemoryMB { get; set; } = 512;

    /// <summary>
    /// Resource limit: Maximum CPU percentage (0-100)
    /// 资源限制：最大CPU百分比（0-100）
    /// </summary>
    public int MaxCpuPercent { get; set; } = 50;

    /// <summary>
    /// Resource limit: Allow network access
    /// 资源限制：是否允许网络访问
    /// </summary>
    public bool AllowNetwork { get; set; } = false;

    /// <summary>
    /// Resource limit: Maximum output size in characters
    /// 资源限制：最大输出大小（字符数）
    /// </summary>
    public int MaxOutputSize { get; set; } = 10000;
}


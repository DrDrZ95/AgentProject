namespace Agent.Application.Services.Sandbox;

/// <summary>
/// Local sandbox terminal service implementation
/// 本地沙盒终端服务实现
/// 
/// 注意：此实现基于AI-Agent项目的terminal.py转换而来
/// Note: This implementation is converted from AI-Agent project's terminal.py
/// 
/// 主要差异：
/// Main differences:
/// 1. 使用本地进程而非Docker容器 - Uses local processes instead of Docker containers
/// 2. 简化了会话管理 - Simplified session management  
/// 3. 增加了安全检查 - Added security checks
/// </summary>
public class SandboxTerminalService : ISandboxTerminalService
{
    private readonly SandboxTerminalOptions _options;
    private readonly ILogger<SandboxTerminalService> _logger;
    private string _currentWorkingDirectory;

    public SandboxTerminalService(
        IOptions<SandboxTerminalOptions> options,
        ILogger<SandboxTerminalService> logger)
    {
        _options = options.Value;
        _logger = logger;
        _currentWorkingDirectory = _options.DefaultWorkingDirectory;

        // 确保工作目录存在 - Ensure working directory exists
        EnsureWorkingDirectoryExists();
    }

    /// <summary>
    /// Execute a command in the local sandbox environment
    /// 在本地沙盒环境中执行命令
    /// </summary>
    public async Task<SandboxCommandResult> ExecuteCommandAsync(
        string command,
        string? workingDirectory = null,
        int timeout = 30,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 验证命令安全性 - Validate command safety
            if (!IsCommandSafe(command))
            {
                _logger.LogWarning("Blocked unsafe command: {Command}", command);
                return new SandboxCommandResult
                {
                    ExitCode = -1,
                    StandardError = "Command blocked for security reasons",
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds
                };
            }

            // 清理和准备命令 - Sanitize and prepare command
            var sanitizedCommand = SanitizeCommand(command);
            var effectiveWorkingDir = workingDirectory ?? _currentWorkingDirectory;

            // 确保工作目录存在 - Ensure working directory exists
            if (!Directory.Exists(effectiveWorkingDir))
            {
                Directory.CreateDirectory(effectiveWorkingDir);
            }

            _logger.LogInformation("Executing command: {Command} in directory: {WorkingDirectory}",
                sanitizedCommand, effectiveWorkingDir);

            // Audit the input - 审计输入
            AuditCommandCall(sanitizedCommand, effectiveWorkingDir, "INPUT");

            // 创建进程配置 - Create process configuration
            var processStartInfo = CreateProcessStartInfo(sanitizedCommand, effectiveWorkingDir);

            using var process = new Process { StartInfo = processStartInfo };
            var outputBuilder = new StringBuilder();
            var errorBuilder = new StringBuilder();

            // 设置输出处理 - Setup output handling
            process.OutputDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    if (outputBuilder.Length < _options.MaxOutputSize)
                    {
                        outputBuilder.AppendLine(e.Data);
                    }
                    else if (!outputBuilder.ToString().EndsWith("... [Output Truncated]"))
                    {
                        outputBuilder.AppendLine("... [Output Truncated]");
                    }
                }
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (e.Data != null)
                {
                    if (errorBuilder.Length < _options.MaxOutputSize)
                    {
                        errorBuilder.AppendLine(e.Data);
                    }
                    else if (!errorBuilder.ToString().EndsWith("... [Output Truncated]"))
                    {
                        errorBuilder.AppendLine("... [Output Truncated]");
                    }
                }
            };

            // 启动进程 - Start process
            process.Start();

            // Apply resource limits (simplified) - 应用资源限制（简化）
            try
            {
                if (_options.MaxCpuPercent < 100)
                {
                    process.PriorityClass = ProcessPriorityClass.BelowNormal;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to set process priority for sandbox command");
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            var processTask = process.WaitForExitAsync(cancellationToken);
            var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeout), cancellationToken);

            // Monitor memory in background - 在后台监控内存
            using var memoryCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var memoryMonitorTask = MonitorMemoryAsync(process, _options.MaxMemoryMB, memoryCts.Token);

            var completed = await Task.WhenAny(processTask, timeoutTask);
            memoryCts.Cancel(); // Stop memory monitoring

            if (completed == timeoutTask)
            {
                // 超时处理 - Timeout handling
                _logger.LogWarning("Command timed out after {Timeout}s: {Command}", timeout, command);

                try
                {
                    process.Kill(true); // Kill process tree
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to kill timed out process");
                }

                return new SandboxCommandResult
                {
                    ExitCode = -1,
                    StandardError = $"Command timed out after {timeout} seconds",
                    ExecutionTimeMs = stopwatch.ElapsedMilliseconds
                };
            }

            var result = new SandboxCommandResult
            {
                ExitCode = process.ExitCode,
                StandardOutput = outputBuilder.ToString().Trim(),
                StandardError = errorBuilder.ToString().Trim(),
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            };

            // Audit the output - 审计输出
            AuditCommandCall(sanitizedCommand, effectiveWorkingDir, "OUTPUT", result);

            _logger.LogInformation("Command completed with exit code {ExitCode} in {ExecutionTime}ms",
                result.ExitCode, result.ExecutionTimeMs);

            return result;
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Command execution was cancelled: {Command}", command);
            return new SandboxCommandResult
            {
                ExitCode = -1,
                StandardError = "Command execution was cancelled",
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute command: {Command}", command);
            return new SandboxCommandResult
            {
                ExitCode = -1,
                StandardError = $"Execution failed: {ex.Message}",
                ExecutionTimeMs = stopwatch.ElapsedMilliseconds
            };
        }
    }

    /// <summary>
    /// Execute a command with streaming output
    /// 执行命令并流式输出结果
    /// 
    /// 注意：这是对AI-Agent异步输出功能的简化实现
    /// Note: This is a simplified implementation of AI-Agent async output functionality
    /// </summary>
    public async IAsyncEnumerable<string> ExecuteCommandStreamAsync(
        string command,
        string? workingDirectory = null,
        int timeout = 30,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        // 验证命令安全性 - Validate command safety
        if (!IsCommandSafe(command))
        {
            _logger.LogWarning("Blocked unsafe streaming command: {Command}", command);
            yield return "Error: Command blocked for security reasons";
            yield break;
        }

        var sanitizedCommand = SanitizeCommand(command);
        var effectiveWorkingDir = workingDirectory ?? _currentWorkingDirectory;

        // 确保工作目录存在 - Ensure working directory exists
        if (!Directory.Exists(effectiveWorkingDir))
        {
            Directory.CreateDirectory(effectiveWorkingDir);
        }

        _logger.LogInformation("Executing streaming command: {Command}", sanitizedCommand);

        // Audit the input - 审计输入
        AuditCommandCall(sanitizedCommand, effectiveWorkingDir, "INPUT_STREAM");

        var processStartInfo = CreateProcessStartInfo(sanitizedCommand, effectiveWorkingDir);

        using var process = new Process { StartInfo = processStartInfo };

        process.Start();

        // Apply resource limits (simplified) - 应用资源限制（简化）
        try
        {
            if (_options.MaxCpuPercent < 100)
            {
                process.PriorityClass = ProcessPriorityClass.BelowNormal;
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set process priority for sandbox streaming command");
        }

        // 创建读取任务 - Create reading tasks
        var outputTask = ReadStreamAsync(process.StandardOutput, "STDOUT", cancellationToken);
        var errorTask = ReadStreamAsync(process.StandardError, "STDERR", cancellationToken);

        // 等待进程完成 - Wait for process completion
        var processTask = process.WaitForExitAsync(cancellationToken);
        var timeoutTask = Task.Delay(TimeSpan.FromSeconds(timeout), cancellationToken);

        // 流式输出 - Stream output
        await foreach (var line in MergeStreams(outputTask, errorTask, cancellationToken))
        {
            yield return line;

            // 检查进程是否完成 - Check if process completed
            if (process.HasExited)
            {
                break;
            }
        }

        // 等待进程完成或超时 - Wait for completion or timeout
        var completedTask = await Task.WhenAny(processTask, timeoutTask);

        if (completedTask == timeoutTask && !process.HasExited)
        {
            _logger.LogWarning("Streaming command timed out: {Command}", command);
            try
            {
                process.Kill(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to kill timed out streaming process");
            }
            yield return "Error: Command timed out";
        }

        if (!completedTask.IsCompletedSuccessfully)
        {
            _logger.LogInformation("Streaming command was cancelled: {Command}", command);
            yield return "Command execution was cancelled";
        }

        if (completedTask.Exception != null)
        {
            _logger.LogError(completedTask.Exception, "Failed to execute streaming command: {Command}", command);
            yield return $"Error: {completedTask.Exception.Message}";
        }

        // Audit the completion - 审计完成
        AuditCommandCall(sanitizedCommand, effectiveWorkingDir, "OUTPUT_STREAM_COMPLETED");
    }

    /// <summary>
    /// Get current working directory
    /// 获取当前工作目录
    /// </summary>
    public async Task<string> GetWorkingDirectoryAsync()
    {
        return await Task.FromResult(_currentWorkingDirectory);
    }

    /// <summary>
    /// Set working directory
    /// 设置工作目录
    /// </summary>
    public async Task<bool> SetWorkingDirectoryAsync(string path)
    {
        try
        {
            // 验证路径安全性 - Validate path safety
            if (!IsPathSafe(path))
            {
                _logger.LogWarning("Blocked unsafe path: {Path}", path);
                return false;
            }

            var fullPath = Path.GetFullPath(path);

            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }

            _currentWorkingDirectory = fullPath;
            _logger.LogInformation("Working directory changed to: {Path}", fullPath);

            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set working directory: {Path}", path);
            return false;
        }
    }

    /// <summary>
    /// Check if a command is safe to execute
    /// 检查命令是否安全可执行
    /// 
    /// 基于AI-Agent的_sanitize_command方法实现
    /// Based on AI-Agent _sanitize_command method
    /// </summary>
    public bool IsCommandSafe(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
        {
            return false;
        }

        // 检查阻止列表 - Check blocked commands
        var lowerCommand = command.ToLowerInvariant();
        foreach (var blockedCommand in _options.BlockedCommands)
        {
            if (lowerCommand.Contains(blockedCommand.ToLowerInvariant()))
            {
                return false;
            }
        }

        // 检查网络命令 - Check network commands if network is not allowed
        if (!_options.AllowNetwork)
        {
            var networkCommands = new[] { "curl", "wget", "ping", "ssh", "scp", "ftp", "telnet", "nc", "netcat", "nslookup", "dig" };
            if (networkCommands.Any(nc => lowerCommand.Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(nc)))
            {
                _logger.LogWarning("Network access is disabled, blocked command: {Command}", command);
                return false;
            }
        }

        // 检查允许列表（如果配置了） - Check allowed commands (if configured)
        if (_options.AllowedCommands.Any())
        {
            var commandParts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (commandParts.Length > 0)
            {
                var baseCommand = commandParts[0];
                return _options.AllowedCommands.Any(allowed =>
                    baseCommand.Equals(allowed, StringComparison.OrdinalIgnoreCase));
            }
        }

        // 检查危险模式 - Check dangerous patterns
        var dangerousPatterns = new[]
        {
            @";\s*rm\s+-rf",
            @"&&\s*rm\s+-rf",
            @"\|\s*rm\s+-rf",
            @">\s*/dev/",
            @"<\s*/dev/random",
            @":\(\)\{.*\|\:.*\&.*\}\;",  // Fork bomb pattern
            @"while\s+true.*do",         // Infinite loop
            @"for\s+.*\s+in\s+.*\s+do.*rm", // Dangerous for loop
        };

        foreach (var pattern in dangerousPatterns)
        {
            if (Regex.IsMatch(lowerCommand, pattern, RegexOptions.IgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Get system information
    /// 获取系统信息
    /// </summary>
    public async Task<SandboxSystemInfo> GetSystemInfoAsync()
    {
        try
        {
            var systemInfo = new SandboxSystemInfo
            {
                OperatingSystem = RuntimeInformation.OSDescription,
                Architecture = RuntimeInformation.OSArchitecture.ToString(),
                CpuCoreCount = Environment.ProcessorCount,
                CurrentUser = Environment.UserName,
                HomeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
            };

            // 获取可用内存 - Get available memory
            try
            {
                var memoryInfo = GC.GetGCMemoryInfo();
                systemInfo.AvailableMemoryMB = memoryInfo.TotalAvailableMemoryBytes / (1024 * 1024);
            }
            catch
            {
                systemInfo.AvailableMemoryMB = -1; // 无法获取 - Unable to get
            }

            return await Task.FromResult(systemInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get system information");
            return new SandboxSystemInfo
            {
                OperatingSystem = "Unknown",
                Architecture = "Unknown"
            };
        }
    }

    #region Private Helper Methods - 私有辅助方法

    /// <summary>
    /// Ensure working directory exists
    /// 确保工作目录存在
    /// </summary>
    private void EnsureWorkingDirectoryExists()
    {
        try
        {
            if (!Directory.Exists(_currentWorkingDirectory))
            {
                Directory.CreateDirectory(_currentWorkingDirectory);
                _logger.LogInformation("Created working directory: {Directory}", _currentWorkingDirectory);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create working directory: {Directory}", _currentWorkingDirectory);
            // 回退到临时目录 - Fallback to temp directory
            _currentWorkingDirectory = Path.GetTempPath();
        }
    }

    /// <summary>
    /// Sanitize command string
    /// 清理命令字符串
    /// </summary>
    private string SanitizeCommand(string command)
    {
        if (!_options.EnableSanitization)
        {
            return command;
        }

        // 移除危险字符 - Remove dangerous characters
        var sanitized = command
            .Replace("$(", "\\$(")  // Escape command substitution
            .Replace("`", "\\`")    // Escape backticks
            .Replace("|", "\\|");   // Escape pipes (可选 - optional)

        return sanitized;
    }

    /// <summary>
    /// Check if path is safe
    /// 检查路径是否安全
    /// </summary>
    private bool IsPathSafe(string path)
    {
        // 检查路径遍历 - Check path traversal
        if (path.Contains("..") || path.Contains("~"))
        {
            return false;
        }

        // 检查绝对路径到敏感目录 - Check absolute paths to sensitive directories
        var sensitiveDirectories = new[] { "/etc", "/bin", "/sbin", "/usr/bin", "/usr/sbin", "/root" };
        var fullPath = Path.GetFullPath(path);

        return !sensitiveDirectories.Any(sensitive =>
            fullPath.StartsWith(sensitive, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Audit command call - 审计命令调用
    /// </summary>
    private void AuditCommandCall(string command, string workingDir, string type, SandboxCommandResult? result = null)
    {
        if (!_options.EnableLogging) return;

        var auditInfo = new
        {
            Timestamp = DateTime.UtcNow,
            Type = type,
            Command = command,
            WorkingDirectory = workingDir,
            User = Environment.UserName,
            Result = result != null ? new { result.ExitCode, result.ExecutionTimeMs } : null
        };

        _logger.LogInformation("[SANDBOX AUDIT] {AuditInfo}", JsonSerializer.Serialize(auditInfo));
    }

    /// <summary>
    /// Monitor process memory usage - 监控进程内存使用情况
    /// </summary>
    private async Task MonitorMemoryAsync(Process process, long maxMemoryMB, CancellationToken ct)
    {
        try
        {
            while (!process.HasExited && !ct.IsCancellationRequested)
            {
                process.Refresh();
                var currentMemoryMB = process.WorkingSet64 / (1024 * 1024);

                if (currentMemoryMB > maxMemoryMB)
                {
                    _logger.LogWarning("Process {ProcessId} exceeded memory limit: {CurrentMB}MB > {MaxMB}MB. Killing process.",
                        process.Id, currentMemoryMB, maxMemoryMB);
                    process.Kill(true);
                    break;
                }

                await Task.Delay(1000, ct); // Check every second
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Error monitoring process memory");
        }
    }

    /// <summary>
    /// Create process start info
    /// 创建进程启动信息
    /// </summary>
    private ProcessStartInfo CreateProcessStartInfo(string command, string workingDirectory)
    {
        var isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        var startInfo = new ProcessStartInfo
        {
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            RedirectStandardInput = true,
            CreateNoWindow = true
        };

        // 根据操作系统设置命令 - Set command based on OS
        if (isWindows)
        {
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = $"/c {command}";
        }
        else
        {
            startInfo.FileName = "/bin/bash";
            startInfo.Arguments = $"-c \"{command.Replace("\"", "\\\"")}\"";
        }

        // 设置环境变量 - Set environment variables
        foreach (var envVar in _options.EnvironmentVariables)
        {
            startInfo.EnvironmentVariables[envVar.Key] = envVar.Value;
        }

        return startInfo;
    }

    /// <summary>
    /// Read stream asynchronously
    /// 异步读取流
    /// </summary>
    private async IAsyncEnumerable<string> ReadStreamAsync(
        StreamReader reader,
        string prefix,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        Exception? error = null;
        while (!reader.EndOfStream && !cancellationToken.IsCancellationRequested)
        {
            // .NET 8 中没有统一要求 ReadLineAsync(token)，用 WaitAsync 兼容
            var line = await reader.ReadLineAsync().WaitAsync(cancellationToken);
            if (line is not null)
            {
                yield return $"[{prefix}] {line}";
            }
        }

        if (!reader.EndOfStream)
        {
            yield return $"貌似发生了错误：[{prefix}] {reader.ReadLine()}";
        }
    }

    /// <summary>
    /// Merge multiple async streams
    /// 合并多个异步流
    /// </summary>
    private async IAsyncEnumerable<string> MergeStreams(
        IAsyncEnumerable<string> stream1,
        IAsyncEnumerable<string> stream2,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var enumerator1 = stream1.GetAsyncEnumerator(cancellationToken);
        var enumerator2 = stream2.GetAsyncEnumerator(cancellationToken);

        try
        {
            var task1 = enumerator1.MoveNextAsync();
            var task2 = enumerator2.MoveNextAsync();

            while (!cancellationToken.IsCancellationRequested)
            {
                var completedTask = await Task.WhenAny(task1.AsTask(), task2.AsTask());

                if (completedTask == task1.AsTask())
                {
                    if (await task1)
                    {
                        yield return enumerator1.Current;
                        task1 = enumerator1.MoveNextAsync();
                    }
                    else
                    {
                        // Stream 1 ended
                        while (await task2)
                        {
                            yield return enumerator2.Current;
                            task2 = enumerator2.MoveNextAsync();
                        }
                        break;
                    }
                }
                else if (completedTask == task2.AsTask())
                {
                    if (await task2)
                    {
                        yield return enumerator2.Current;
                        task2 = enumerator2.MoveNextAsync();
                    }
                    else
                    {
                        // Stream 2 ended
                        while (await task1)
                        {
                            yield return enumerator1.Current;
                            task1 = enumerator1.MoveNextAsync();
                        }
                        break;
                    }
                }
            }
        }
        finally
        {
            await enumerator1.DisposeAsync();
            await enumerator2.DisposeAsync();
        }
    }

    #endregion
}


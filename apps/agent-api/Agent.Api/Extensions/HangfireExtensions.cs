namespace Agent.Api.Extensions;

/// <summary>
/// Hangfire 扩展方法，用于配置和添加 Hangfire 服务
/// Hangfire extension methods for configuring and adding Hangfire services
/// </summary>
public static class HangfireExtensions
{
    public static IServiceCollection AddHangfireServices(this IServiceCollection services, IConfiguration configuration)
    {
        var redisConnectionString = configuration.GetSection("Redis:ConnectionString").Value;

        // 添加 Hangfire 服务
        // Add Hangfire services
        services.AddHangfire(hangfireConfig =>
        {
            hangfireConfig
                .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
                .UseSimpleAssemblyNameTypeSerializer()
                .UseRecommendedSerializerSettings();

            try
            {
                if (!string.IsNullOrEmpty(redisConnectionString))
                {
                    // 尝试使用 Redis 存储 (Try to use Redis storage)
                    hangfireConfig.UseRedisStorage(redisConnectionString);
                }
                else
                {
                    throw new InvalidOperationException("Redis connection string is missing.");
                }
            }
            catch (Exception ex)
            {
                // 如果 Redis 失败，回退到内存存储 (If Redis fails, fall back to memory storage)
                ExternalComponentLogger.LogConnectionError("Hangfire Redis", ex, "Hangfire 将回退到内存存储 (MemoryStorage)。注意：重启后后台任务将丢失。");
                hangfireConfig.UseMemoryStorage();
            }
        });

        // 添加 Hangfire 服务器，用于处理后台任务
        // Add Hangfire server for processing background jobs
        services.AddHangfireServer();

        return services;
    }

    /// <summary>
    /// 配置 Hangfire Dashboard 中间件
    /// Configure Hangfire Dashboard middleware
    /// </summary>
    /// <param name="app"></param>
    public static IApplicationBuilder UseHangfireDashboard(this IApplicationBuilder app)
    {
        // 启用 Hangfire Dashboard
        // Enable Hangfire Dashboard
        app.UseHangfireDashboard("/hangfire", new DashboardOptions
        {
            Authorization = new IDashboardAuthorizationFilter[]
            {
                new AdministratorDashboardAuthorizationFilter()
            }
        });

        // RAG 缓存预热任务 (RAG cache warmup job)
        // 每天凌晨 2 点执行 (Executes at 2 AM daily)
        RecurringJob.AddOrUpdate<IRagCacheWarmer>(
            "RagCacheWarmup-Default",
            warmer => warmer.WarmupHotQueriesAsync("default", default),
            Cron.Daily(2));

        return app;
    }
}

/// <summary>
/// Restricts the Hangfire dashboard to authenticated administrators.
/// 仅允许已认证的管理员访问 Hangfire Dashboard。
/// </summary>
internal sealed class AdministratorDashboardAuthorizationFilter : IDashboardAuthorizationFilter
{
    public bool Authorize(DashboardContext context)
    {
        var httpContext = context.GetHttpContext();
        return httpContext.User.Identity?.IsAuthenticated == true
            && httpContext.User.IsInRole("Administrator");
    }
}


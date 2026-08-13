namespace Agent.Api.Extensions;

/// <summary>
/// Identity service extensions for dependency injection
/// Identity服务扩展，用于依赖注入
/// </summary>
public static class IdentityExtensions
{
    /// <summary>
    /// Add Identity services with PostgreSQL and JWT Bearer authentication
    /// 添加带有PostgreSQL和JWT Bearer认证的Identity服务
    /// </summary>
    /// <param name="services">Service collection - 服务集合</param>
    /// <param name="configuration">Configuration - 配置</param>
    /// <returns>Service collection - 服务集合</returns>
    public static IServiceCollection AddIdentityServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Add PostgreSQL DbContext
        // 添加PostgreSQL数据库上下文
        var connectionString = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection must be configured. " +
                "Use user secrets or the ConnectionStrings__DefaultConnection environment variable.");
        }

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly("Agent.Api");
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
            });

            // Enable sensitive data logging in development
            // 在开发环境中启用敏感数据日志记录
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        // Add Identity services
        // 添加Identity服务
        services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
        {
            // Password settings
            // 密码设置
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequireUppercase = true;
            options.Password.RequiredLength = 8;
            options.Password.RequiredUniqueChars = 1;

            // Lockout settings
            // 锁定设置
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // User settings
            // 用户设置
            options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
            options.User.RequireUniqueEmail = true;

            // Sign in settings
            // 登录设置
            options.SignIn.RequireConfirmedEmail = false;
            options.SignIn.RequireConfirmedPhoneNumber = false;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // Add JWT Bearer authentication
        // 添加JWT Bearer认证
        var jwtSettings = configuration.GetSection("JwtSettings");
        var secretKey = jwtSettings["SecretKey"];
        if (string.IsNullOrWhiteSpace(secretKey) || Encoding.UTF8.GetByteCount(secretKey) < 32)
        {
            throw new InvalidOperationException(
                "JwtSettings:SecretKey must be configured with at least 32 bytes. " +
                "Use user secrets or the JwtSettings__SecretKey environment variable.");
        }

        var issuer = jwtSettings["Issuer"] ?? "Agent.Api";
        var audience = jwtSettings["Audience"] ?? "Agent.ApiUsers";
        var isDevelopment = string.Equals(
            configuration["ASPNETCORE_ENVIRONMENT"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
            Environments.Development,
            StringComparison.OrdinalIgnoreCase);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.SaveToken = true;
            options.RequireHttpsMetadata = !isDevelopment;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                ClockSkew = TimeSpan.Zero
            };

            // Handle JWT events
            // 处理JWT事件
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogError("Authentication failed: {Exception}", context.Exception);
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogDebug("Token validated for user: {User}", context.Principal?.Identity?.Name);
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    logger.LogWarning("JWT Challenge: {Error} - {ErrorDescription}", context.Error, context.ErrorDescription);
                    return Task.CompletedTask;
                },
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query["access_token"];

                    // If the request is for our hub...
                    // 如果请求是针对我们的hub...
                    var path = context.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) &&
                        (path.StartsWithSegments("/aiagentHub") || path.StartsWithSegments("/chathub") || path.StartsWithSegments("/workflowHub")))
                    {
                        // Read the token out of the query string
                        // 从查询字符串中读取token
                        context.Token = accessToken;
                    }
                    return Task.CompletedTask;
                }
            };
        });

        // Add permission-based authorization
        // 添加基于权限的授权
        services.AddAuthorization();
        services.AddSingleton<IAuthorizationPolicyProvider, PermissionAuthorizationPolicyProvider>();
        services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        services.AddScoped<IAuthorizationHandler, MultiplePermissionsAuthorizationHandler>();

        // Add permission service
        // 添加权限服务
        services.AddScoped<IPermissionService, PermissionService>();

        // Add CORS for frontend integration
        // 为前端集成添加CORS
        services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        return services;
    }

    /// <summary>
    /// Use Identity middleware
    /// 使用Identity中间件
    /// </summary>
    /// <param name="app">Application builder - 应用程序构建器</param>
    /// <returns>Application builder - 应用程序构建器</returns>
    public static IApplicationBuilder UseIdentityServices(this IApplicationBuilder app)
    {
        // Use CORS
        // 使用CORS
        app.UseCors("AllowFrontend");

        // Use authentication and authorization
        // 使用认证和授权
        app.UseAuthentication();
        app.UseAuthorization();

        return app;
    }

    /// <summary>
    /// Initialize Identity database and seed data
    /// 初始化Identity数据库并播种数据
    /// </summary>
    /// <param name="app">Application builder - 应用程序构建器</param>
    /// <returns>Application builder - 应用程序构建器</returns>
    public static void InitializeIdentityDatabase(this IApplicationBuilder app)
    {
        // 异步在后台执行数据库初始化，不阻塞主线程
        // Execute database initialization asynchronously in the background, without blocking the main thread
        Task.Run(async () =>
        {
            using var scope = app.ApplicationServices.CreateScope();
            var services = scope.ServiceProvider;
            var logger = services.GetRequiredService<ILogger<Program>>();

            try
            {
                var context = services.GetRequiredService<ApplicationDbContext>();
                var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
                var roleManager = services.GetRequiredService<RoleManager<ApplicationRole>>();

                // Ensure database is created
                // 确保数据库已创建
                await context.Database.EnsureCreatedAsync();

                // Apply pending migrations
                // 应用待处理的迁移
                var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
                if (pendingMigrations.Any())
                {
                    logger.LogInformation("Applying pending database migrations...");
                    await context.Database.MigrateAsync();
                }

                // Seed default admin user if not exists
                // 如果不存在，则播种默认管理员用户
                var configuration = services.GetRequiredService<IConfiguration>();
                await SeedDefaultAdminUserAsync(userManager, roleManager, configuration, logger);

                logger.LogInformation("Identity database initialized successfully");
            }
            catch (Exception ex)
            {
                // 使用外部组件日志记录器，输出亮红色提示，但不阻塞程序启动
                ExternalComponentLogger.LogConnectionError("Identity Database", ex, "Identity 数据库连接失败。用户登录、权限验证等功能将受限。请检查 PostgreSQL 服务状态。");
                
                // 不再向上抛出异常，允许主程序继续启动
                // Do not re-throw the exception, allow the main program to continue starting
            }
        });
    }

    /// <summary>
    /// Seed default admin user
    /// 播种默认管理员用户
    /// </summary>
    private static async Task SeedDefaultAdminUserAsync(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager,
        IConfiguration configuration,
        ILogger logger)
    {
        if (!configuration.GetValue("Identity:SeedAdmin:Enabled", false))
        {
            logger.LogInformation("Default administrator seeding is disabled");
            return;
        }

        var adminEmail = configuration["Identity:SeedAdmin:Email"];
        var adminPassword = configuration["Identity:SeedAdmin:Password"];
        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogError(
                "Administrator seeding is enabled, but Identity:SeedAdmin:Email or Identity:SeedAdmin:Password is missing");
            return;
        }

        const string adminRole = "Administrator";

        // Check if admin user already exists
        // 检查管理员用户是否已存在
        var adminUser = await userManager.FindByEmailAsync(adminEmail);
        if (adminUser != null)
        {
            logger.LogInformation("Default admin user already exists");
            return;
        }

        // Ensure admin role exists
        // 确保管理员角色存在
        var role = await roleManager.FindByNameAsync(adminRole);
        if (role == null)
        {
            logger.LogWarning("Administrator role not found in database");
            return;
        }

        // Create admin user
        // 创建管理员用户
        adminUser = new ApplicationUser
        {
            UserName = adminEmail,
            Email = adminEmail,
            EmailConfirmed = true,
            FirstName = "System",
            LastName = "Administrator",
            Department = "IT",
            JobTitle = "System Administrator",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(adminUser, adminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(adminUser, adminRole);
            logger.LogInformation("Default admin user created successfully: {Email}", adminEmail);
        }
        else
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            logger.LogError("Failed to create default admin user: {Errors}", errors);
        }
    }
}


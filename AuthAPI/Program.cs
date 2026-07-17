using AuthAPI.Data;
using AuthAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Threading.RateLimiting;
using AuthAPI.Middleware;
using FluentValidation;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting AuthAPI");

    var builder = WebApplication.CreateBuilder(args);

    // Use Serilog
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
    );

    // Connect Database SQL Server
    builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlServer(
        builder.Configuration.GetConnectionString("Default"),
        x => x.MigrationsAssembly("AuthAPI"))
    );

    // Dependency Injection
    builder.Services.AddScoped<IAuthService, AuthService>();
    builder.Services.AddScoped<IOAuthService, OAuthService>();

    // Add JWT Authentication
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidAudience = builder.Configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]!)
                )
            };
        });

    builder.Services.AddControllers();
    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();

    // Add Request Validators
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();

    // =============================
    // ---  Rate Limit Config  ----
    // =============================
    builder.Services.AddRateLimiter(options =>
    {
        // Global Limit all Endpoints
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknow",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    Window = TimeSpan.FromSeconds(10),
                    PermitLimit = 100,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                })
        );

        // Login Policy (Login/Register)
        options.AddPolicy("LoginPolicy", httpContext => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknow",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 5,            // 5 requests per minutes
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }
        ));
        
        // Custom Response
        options.OnRejected = async (context, cancell) =>
        {
            context.HttpContext.Response.StatusCode = 429;  // Too many Requests
            context.HttpContext.Response.ContentType = "application/json";

            var errorResponse = new
            {
                message = "Too many requests, Please try again later",
                retryAfterSeconds = 60
            };

            await context.HttpContext.Response.WriteAsJsonAsync(errorResponse, cancell);
        };
    });

    var app = builder.Build();

    // Check Database Connection
    using (var scope = app.Services.CreateScope())
    {
        var services = scope.ServiceProvider;
        try
        {
            Log.Information("Checking database connection...");
            var context = services.GetRequiredService<AppDbContext>();

            if (context.Database.CanConnect())
            {
                Log.Information("Database connection successfully");

                context.Database.Migrate();
            }
            else
            {
                Log.Warning("Cannot connect to the database!");
            }
        }
        catch (Exception e)
        {
            Log.Fatal(e, "error connection or migrations the database");
            throw;
        }
    }

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    app.UseHttpsRedirection();

    // Global Exception Middleware
    app.UseMiddleware<GlobalExceptionMiddleware>();

    // Serilog Request Logging
    app.UseSerilogRequestLogging();
    
    // Rate Limit
    app.UseRouting();
    app.UseRateLimiter();
    
    // Authorization and Authentication
    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception e) when (e.GetType().Name == "HostAbortedException")
{
    // Skip: EF Core Migrations Exception
}
catch (Exception e)
{
    Log.Fatal(e, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
using AuthAPI.Data;
using AuthAPI.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
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

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception e) when(e.GetType().Name == "HostAbortedException")
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
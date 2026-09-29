using FluentValidation;
using Its.Api.Middleware;
using Its.Api.Services;
using Its.Application.Interfaces;
using Its.Application.Services;
using Its.Application.Validators;
using Its.Infrastructure.Data;
using Its.Infrastructure.Notifications;
using Its.Infrastructure.Repositories;
using Its.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        "logs/its_module_.log",
        rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Database
var connectionString =
    builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseNpgsql(
        connectionString,
        npgsqlOptions =>
        {
            npgsqlOptions.MigrationsAssembly(
                typeof(ApplicationDbContext).Assembly.FullName);

            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
        });
});

// JWT
var jwtKey =
    builder.Configuration["Jwt:Key"]
    ?? "ITS_Module_Super_Secret_JWT_Key_2026_For_Enterprise_Allocation_System!";

var jwtIssuer =
    builder.Configuration["Jwt:Issuer"]
    ?? "ItsApi";

var jwtAudience =
    builder.Configuration["Jwt:Audience"]
    ?? "ItsApp";

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;

        options.TokenValidationParameters =
    new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,

        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,

        IssuerSigningKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey)),

        ClockSkew = TimeSpan.Zero,

        // IMPORTANT: Tell ASP.NET which JWT claim contains the role
        RoleClaimType = ClaimTypes.Role,
        NameClaimType = ClaimTypes.Name
    };

        // SignalR JWT support
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken =
                    context.Request.Query["access_token"];

                var path =
                    context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/hubs/notifications"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

// Authorization
builder.Services.AddSingleton<IAuthorizationHandler,
    PermissionAuthorizationHandler>();

// Common services
builder.Services.AddHttpContextAccessor();

builder.Services.AddScoped<ICurrentUserService,
    CurrentUserService>();

builder.Services.AddScoped<IPasswordHasher,
    PasswordHasher>();

builder.Services.AddScoped<IJwtTokenService,
    JwtTokenService>();

builder.Services.AddScoped<IUnitOfWork,
    UnitOfWork>();

builder.Services.AddScoped<IAuditService,
    AuditService>();

builder.Services.AddScoped<INotificationPublisher,
    NotificationPublisher>();

// Business services
builder.Services.AddScoped<IAuthService,
    AuthService>();

builder.Services.AddScoped<IDashboardService,
    DashboardService>();

builder.Services.AddScoped<IEmployeeService,
    EmployeeService>();

builder.Services.AddScoped<IExecutiveService,
    ExecutiveService>();

builder.Services.AddScoped<IAssetService,
    AssetService>();

builder.Services.AddScoped<IAllocationService,
    AllocationService>();

builder.Services.AddScoped<IRequestService,
    RequestService>();

builder.Services.AddScoped<IRepairService,
    RepairService>();

builder.Services.AddScoped<IReplacementService,
    ReplacementService>();

builder.Services.AddScoped<INotificationService,
    NotificationService>();

builder.Services.AddScoped<IReportService,
    ReportService>();

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<
    LoginRequestValidator>();

// SignalR
builder.Services.AddSignalR();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5173",
                "http://localhost:3000",
                "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Controllers + JSON Enum support
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(
            new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();

// Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "IT Asset Allocation (ITS) Module API",
            Version = "v1",
            Description =
                "Complete REST API for Employee Management System (EMS) IT Asset Allocation Module"
        });

    c.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Description =
                "JWT Authorization header using the Bearer scheme. Example: Bearer {token}",

            Name = "Authorization",

            In = ParameterLocation.Header,

            Type = SecuritySchemeType.Http,

            Scheme = "Bearer",

            BearerFormat = "JWT"
        });

    c.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference =
                        new OpenApiReference
                        {
                            Type =
                                ReferenceType.SecurityScheme,

                            Id = "Bearer"
                        }
                },

                Array.Empty<string>()
            }
        });
});

var app = builder.Build();

// Database initialization
using (var scope = app.Services.CreateScope())
{
    try
    {
        await DbInitializer.InitializeDatabaseAsync(
            scope.ServiceProvider);
    }
    catch (Exception ex)
    {
        Log.Warning(
            ex,
            "Initial database connection failed. " +
            "Make sure PostgreSQL is running and " +
            "DefaultConnection is correctly configured.");
    }
}

// Middleware
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();

// Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();

    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "ITS API v1");

        c.RoutePrefix = "swagger";
    });
}

// Serilog request logging
app.UseSerilogRequestLogging();

// HTTP pipeline
app.UseRouting();

app.UseCors("AllowFrontend");

app.UseAuthentication();

app.UseAuthorization();

// Controllers
app.MapControllers();

// SignalR
app.MapHub<NotificationHub>(
    "/hubs/notifications");

// Health check
app.MapGet(
    "/api/health",
    () => Results.Ok(
        new
        {
            status = "Healthy",
            timestamp = DateTime.UtcNow,
            module = "IT Asset Allocation (ITS)"
        }));

app.Run();
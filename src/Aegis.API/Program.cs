using Aegis.Application.Assessment.Services;
using Aegis.Application.Common.Behaviors;
using Aegis.Application.Interfaces;
using Aegis.Application.Market.Services;
using Aegis.API.Middleware;
using Aegis.Domain.Interfaces;
using Aegis.Infrastructure.AI;
using Aegis.Infrastructure.Auth;
using Aegis.Infrastructure.Persistence;
using Aegis.Infrastructure.Persistence.Repositories;
using FluentValidation;
using Hangfire;
using Hangfire.PostgreSql;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Prometheus;
using Serilog;
using System.Text;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// ── Serilog ──────────────────────────────────────────────────────────────────
builder.Host.UseSerilog((ctx, cfg) =>
    cfg.ReadFrom.Configuration(ctx.Configuration));

// ── Database ─────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<AegisDbContext>(opts =>
    opts.UseNpgsql(
        builder.Configuration.GetConnectionString("Postgres"),
        npg => npg.UseVector()));

// ── JWT Authentication ────────────────────────────────────────────────────────
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key is not configured.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();

// ── MediatR + Behaviors ───────────────────────────────────────────────────────
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Aegis.Application.AssemblyMarker).Assembly);
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
    cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
});

// ── FluentValidation ──────────────────────────────────────────────────────────
builder.Services.AddValidatorsFromAssembly(
    typeof(Aegis.Application.AssemblyMarker).Assembly);

// ── Infrastructure Services ───────────────────────────────────────────────────
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddSingleton<ITokenSettings, TokenSettings>();
builder.Services.AddScoped<IAIOrchestrator, AnthropicOrchestrator>();

// ── Repositories ──────────────────────────────────────────────────────────────
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAssessmentRepository, AssessmentRepository>();
builder.Services.AddScoped<IMarketKpiRepository, MarketKpiRepository>();
builder.Services.AddScoped<ISkillRepository, SkillRepository>();
builder.Services.AddScoped<IRecommendationRepository, RecommendationRepository>();
builder.Services.AddScoped<IAlertRepository, AlertRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

// ── Application Services ──────────────────────────────────────────────────────
builder.Services.AddScoped<SeniorityScorer>();
builder.Services.AddScoped<KpiComputationService>();
builder.Services.AddScoped<AdaptiveInterviewService>();

// ── Redis / Memory Cache ──────────────────────────────────────────────────────
var redisConnection = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnection))
{
    builder.Services.AddStackExchangeRedisCache(opts =>
        opts.Configuration = redisConnection);
}
else
{
    builder.Services.AddDistributedMemoryCache();
}

// ── Hangfire ──────────────────────────────────────────────────────────────────
builder.Services.AddHangfire(cfg => cfg
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(builder.Configuration.GetConnectionString("Postgres")!));
builder.Services.AddHangfireServer();

// ── Rate Limiting ─────────────────────────────────────────────────────────────
builder.Services.AddRateLimiter(opts =>
{
    opts.AddFixedWindowLimiter("fixed", limiterOpts =>
    {
        limiterOpts.PermitLimit = 100;
        limiterOpts.Window = TimeSpan.FromMinutes(1);
        limiterOpts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOpts.QueueLimit = 10;
    });

    opts.AddFixedWindowLimiter("auth", limiterOpts =>
    {
        limiterOpts.PermitLimit = 10;
        limiterOpts.Window = TimeSpan.FromMinutes(1);
        limiterOpts.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        limiterOpts.QueueLimit = 5;
    });

    opts.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

// ── Health Checks ─────────────────────────────────────────────────────────────
var healthChecks = builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Postgres")!);

if (!string.IsNullOrWhiteSpace(redisConnection))
    healthChecks.AddRedis(redisConnection);

// ── CORS ──────────────────────────────────────────────────────────────────────
var allowedOrigins = builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:3000"];

builder.Services.AddCors(opts =>
    opts.AddPolicy("DefaultCors", policy =>
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials()));

// ── Controllers + Swagger ─────────────────────────────────────────────────────
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AEGIS - AI Career Intelligence API",
        Version = "v1",
        Description = "Backend API for the AEGIS career intelligence platform."
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT Bearer token."
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ─────────────────────────────────────────────────────────────────────────────
var app = builder.Build();

// ── Auto-migrate database at startup ─────────────────────────────────────────
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AegisDbContext>();
    try
    {
        await db.Database.MigrateAsync();
        Log.Information("Database migration completed successfully.");
    }
    catch (Exception ex)
    {
        Log.Warning(ex, "Database migration failed — continuing without migration.");
    }
}

// ── Middleware pipeline ───────────────────────────────────────────────────────
app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "AEGIS API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("DefaultCors");
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// ── Prometheus metrics ────────────────────────────────────────────────────────
app.UseMetricServer();
app.UseHttpMetrics();

// ── Controllers ───────────────────────────────────────────────────────────────
app.MapControllers();
app.MapHealthChecks("/health");

// ── Hangfire dashboard ────────────────────────────────────────────────────────
var enableHangfireDashboard = builder.Configuration.GetValue<bool>("Hangfire:EnableDashboard");
if (enableHangfireDashboard)
    app.MapHangfireDashboard("/hangfire");

app.Run();

// ── Assembly marker for MediatR/FluentValidation scanning ─────────────────────
namespace Aegis.Application { public sealed class AssemblyMarker; }

using Aegis.Application.Assessment.Services;
using Aegis.Application.Interfaces;
using Aegis.Application.Market.Services;
using Aegis.Domain.Interfaces;
using Aegis.Infrastructure.AI;
using Aegis.Infrastructure.Persistence;
using Aegis.Infrastructure.Persistence.Repositories;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using System.Text;
using Hangfire;
using Hangfire.PostgreSql;
using MediatR;
using FluentValidation;
using Prometheus;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

// Database
builder.Services.AddDbContext<AegisDbContext>(opts =>
    opts.UseNpgsql(
        builder.Configuration.GetConnectionString("Postgres"),
        npg => npg.UseVector()));

// Auth
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("Jwt:Key not configured");

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
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();

// MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Aegis.Application.AssemblyMarker).Assembly));

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Aegis.Application.AssemblyMarker).Assembly);

// Application Services
builder.Services.AddScoped<SeniorityScorer>();
builder.Services.AddScoped<KpiComputationService>();

// Infrastructure
builder.Services.AddScoped<IAIOrchestrator, AnthropicOrchestrator>();
builder.Services.AddScoped<IAssessmentRepository, AssessmentRepository>();
builder.Services.AddScoped<IMarketKpiRepository, MarketKpiRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();

// Redis
builder.Services.AddStackExchangeRedisCache(opts =>
    opts.Configuration = builder.Configuration.GetConnectionString("Redis"));

// Hangfire
builder.Services.AddHangfire(cfg => cfg
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(builder.Configuration.GetConnectionString("Postgres")));
builder.Services.AddHangfireServer();

// Rate Limiting
builder.Services.AddRateLimiter(opts =>
    opts.AddFixedWindowLimiter("api", limiterOpts =>
    {
        limiterOpts.PermitLimit = 100;
        limiterOpts.Window = TimeSpan.FromMinutes(1);
    }));

// Health Checks
builder.Services.AddHealthChecks()
    .AddNpgSql(builder.Configuration.GetConnectionString("Postgres")!)
    .AddRedis(builder.Configuration.GetConnectionString("Redis")!);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseSwagger();
app.UseSwaggerUI();
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseMetricServer();
app.UseHttpMetrics();
app.MapControllers();
app.MapHealthChecks("/health");
app.MapHangfireDashboard("/hangfire");

app.Run();

// Marker for assembly scanning
namespace Aegis.Application { public sealed class AssemblyMarker; }

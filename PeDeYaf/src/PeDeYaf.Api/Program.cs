using System;
using System.Threading.RateLimiting;
using Amazon.S3;
using Amazon.SimpleNotificationService;
using Amazon.Textract;
using Anthropic.SDK;
using FluentValidation;
using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using PeDeYaf.Api.Hubs;
using PeDeYaf.Api.Middleware;
using PeDeYaf.Application.Auth.Commands;
using PeDeYaf.Application.Common.Behaviors;
using PeDeYaf.Application.Common.Interfaces;
using PeDeYaf.Application.Documents.Commands;
using PeDeYaf.Domain.Repositories;
using PeDeYaf.Infrastructure.Ai;
using PeDeYaf.Infrastructure.BackgroundJobs;
using PeDeYaf.Infrastructure.Cache;
using PeDeYaf.Infrastructure.FileStorage;
using PeDeYaf.Infrastructure.Ocr;
using PeDeYaf.Infrastructure.Persistence;
using PeDeYaf.Infrastructure.Persistence.Repositories;
using PeDeYaf.Infrastructure.Search;
using PeDeYaf.Infrastructure.Sms;
using PeDeYaf.Infrastructure.Token;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// === MediatR + Pipeline Behaviors ===
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssemblyContaining<RequestOtpCommand>();
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
});

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<RequestOtpCommandValidator>();

// === Infrastructure ===
// Database
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"),
        npg => npg.EnableRetryOnFailure(3)));

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IFolderRepository, FolderRepository>();

// Redis Cache
builder.Services.AddStackExchangeRedisCache(opts =>
    opts.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379");
builder.Services.AddScoped<ICacheService, RedisCacheService>();

// AWS Configuration
builder.Services.AddDefaultAWSOptions(builder.Configuration.GetAWSOptions());
builder.Services.AddAWSService<IAmazonS3>();
builder.Services.AddAWSService<IAmazonTextract>();
builder.Services.AddAWSService<IAmazonSimpleNotificationService>();

using OpenSearch.Client;
using OpenSearch.Net;
using PeDeYaf.Infrastructure.Cache;

// ... Inside Program.cs dependencies ...

// External Services
builder.Services.AddScoped<IFileStorage, S3FileStorage>();
builder.Services.AddScoped<IOcrService, TextractOcrService>();
builder.Services.AddSingleton<AnthropicClient>(sp => new AnthropicClient(builder.Configuration["Anthropic:ApiKey"] ?? "dummy-key-for-build"));
builder.Services.AddScoped<IAiService, ClaudeAiService>();
builder.Services.AddScoped<ISmsService, SnsService>();
builder.Services.AddScoped<ITokenService, TokenService>();

var osUri = builder.Configuration["OpenSearch:Uri"] ?? "http://localhost:9200";
builder.Services.AddSingleton<IOpenSearchClient>(new OpenSearchClient(new ConnectionSettings(new Uri(osUri))));
builder.Services.AddScoped<ISearchIndexService, OpenSearchService>();

// Hangfire
builder.Services.AddHangfire(cfg => cfg
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UsePostgreSqlStorage(opts => opts.UseNpgsqlConnection(builder.Configuration.GetConnectionString("Postgres"))));

builder.Services.AddHangfireServer(opts =>
{
    opts.Queues = ["default", "ocr", "critical"];
    opts.WorkerCount = Environment.ProcessorCount * 2;
});

// Background Jobs interfaces
builder.Services.AddScoped<IBackgroundJobClient, HangfireJobClient>();
builder.Services.AddScoped<IPdfMergeJobClient, HangfireJobClient>();

// Real-Time Sync
builder.Services.AddSignalR(opts =>
{
    opts.EnableDetailedErrors = builder.Environment.IsDevelopment();
    opts.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<ISyncNotifier, SyncNotifier>();

// === Auth & Security ===
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opts =>
    {
        opts.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "PeDeYaf",
            ValidAudience = builder.Configuration["Jwt:Audience"] ?? "PeDeYafUsers",
            IssuerSigningKey = new SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"] ?? "superSecretKeyThatIsAtLeast32BytesLong!")),
            ClockSkew = TimeSpan.FromSeconds(30),
        };

        opts.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                var token = ctx.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(token) &&
                    ctx.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    ctx.Token = token;
                }
                return Task.CompletedTask;
            },
        };
    });

// Rate limiting
builder.Services.AddRateLimiter(opts =>
{
    opts.AddPolicy("otp", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromHours(1),
                PermitLimit = 5,
            }));

    opts.AddPolicy("api", ctx =>
        RateLimitPartition.GetFixedWindowLimiter(
            ctx.User.FindFirst("sub")?.Value ?? ctx.Connection.RemoteIpAddress?.ToString() ?? "anon",
            _ => new FixedWindowRateLimiterOptions
            {
                Window = TimeSpan.FromMinutes(1),
                PermitLimit = 120,
            }));
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionMiddleware>();

app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers().RequireRateLimiting("api");
app.MapHub<SyncHub>("/hubs/sync");

app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    // In real app use a custom authorization filter here
});

app.Run();

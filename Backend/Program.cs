using System.Text.Json;
using Backend.Endpoints;
using Backend.Options;
using Backend.Services;
using Microsoft.AspNetCore.Http.Json;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<JsonOptions>(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

builder.Services.Configure<BommyBackendOptions>(builder.Configuration.GetSection("Bommy"));
builder.Services.AddOpenApi();
builder.Services.AddSingleton<BommyState>();

var app = builder.Build();

app.Use(async (context, next) =>
{
    ILogger logger = context.RequestServices
        .GetRequiredService<ILoggerFactory>()
        .CreateLogger("Bommy.Requests");

    long started = Environment.TickCount64;
    logger.LogInformation(
        "HTTP {Method} {Path} started",
        context.Request.Method,
        context.Request.Path
    );

    try
    {
        await next();
    }
    finally
    {
        logger.LogInformation(
            "HTTP {Method} {Path} completed {StatusCode} in {ElapsedMs}ms",
            context.Request.Method,
            context.Request.Path,
            context.Response.StatusCode,
            Environment.TickCount64 - started
        );
    }
});

app.MapOpenApi();
app.MapScalarApiReference("/scalar", options =>
{
    options.WithTitle("Bommy Backend API");
    options.WithDynamicBaseServerUrl();
});

app.MapGet("/", () => Results.Redirect("/scalar"))
    .ExcludeFromDescription();

app.MapBommyEndpoints();

app.Run();

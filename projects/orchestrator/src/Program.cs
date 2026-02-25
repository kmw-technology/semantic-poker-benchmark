using Microsoft.EntityFrameworkCore;
using Serilog;
using SemanticPoker.Api.Infrastructure.Persistence;
using SemanticPoker.Api.Infrastructure.LlmAdapters;
using SemanticPoker.Api.Services;
using SemanticPoker.GameEngine;
using SemanticPoker.GameEngine.Templates;
using SemanticPoker.Shared.Interfaces;

var builder = WebApplication.CreateBuilder(args);

// Serilog
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));

// EF Core + SQLite
builder.Services.AddDbContext<BenchmarkDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("BenchmarkDb")));

// Repositories
builder.Services.AddScoped<IMatchRepository, MatchRepository>();

// Game Engine services
builder.Services.AddSingleton<IStateGenerator, StateGenerator>();
builder.Services.AddSingleton<IStateValidator, StateValidator>();
builder.Services.AddSingleton<IScoreCalculator, ScoreCalculator>();
builder.Services.AddSingleton<SentenceTemplateRegistry>();
builder.Services.AddSingleton<ISentenceEngine, SentenceTemplateEngine>();

// LLM Adapter
builder.Services.AddSingleton<ILlmAdapter, OllamaAdapter>();

// Orchestration services
builder.Services.AddSingleton<PromptBuilder>();
builder.Services.AddSingleton<LlmResponseParser>();
builder.Services.AddSingleton<AdaptiveHistoryBuilder>();
builder.Services.AddSingleton<ArchitectRotation>();
builder.Services.AddScoped<MatchRunnerService>();
builder.Services.AddSingleton<MatchExecutionQueue>();
builder.Services.AddHostedService<MatchExecutionService>();

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Semantic Poker Benchmark API",
        Version = "v1",
        Description = "REST API for 'The Oracle's Bluff' — a zero-luck, deterministic benchmark for testing Theory of Mind and Level-k deception in Large Language Models."
    });
});

// SignalR
builder.Services.AddSignalR();

// CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.SetIsOriginAllowed(_ => true)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// HttpClient for Ollama
builder.Services.AddHttpClient("Ollama", (sp, client) =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var baseUrl = config["Ollama:BaseUrl"] ?? "http://localhost:11434";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(180);
});

var app = builder.Build();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BenchmarkDbContext>();
    await db.Database.EnsureCreatedAsync();
}

// Middleware
app.UseSerilogRequestLogging();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Semantic Poker Benchmark API v1");
    });
}

app.UseCors();
app.MapControllers();
app.MapHub<SemanticPoker.Api.Hubs.MatchProgressHub>("/hubs/match-progress");

Log.Information("Semantic Poker Benchmark API starting on {Urls}", string.Join(", ", app.Urls));
app.Run();

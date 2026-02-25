using SemanticPoker.WebUI.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

// API Client
var apiBaseUrl = builder.Configuration["BenchmarkApi:BaseUrl"] ?? "http://localhost:5000";
builder.Services.AddHttpClient<IBenchmarkApiClient, BenchmarkApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl);
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

app.Run();

using ConfigVault.Api.Middleware;
using ConfigVault.Api.Services;
using ConfigVault.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddConfigVault(builder.Configuration);
builder.Services.AddSingleton<SseConnectionManager>();

var app = builder.Build();

_ = app.Services.GetRequiredService<SseConnectionManager>();

app.UseApiKeyAuth();
app.MapControllers();

app.Run();

public partial class Program
{
}

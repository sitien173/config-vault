using ConfigVault.Api.Middleware;
using ConfigVault.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddConfigVault(builder.Configuration);

var app = builder.Build();

app.UseApiKeyAuth();
app.MapControllers();

app.Run();

public partial class Program
{
}

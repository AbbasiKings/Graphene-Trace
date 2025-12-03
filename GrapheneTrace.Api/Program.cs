using GrapheneTrace.Api.Data;
using GrapheneTrace.Api.Interfaces;
using GrapheneTrace.Api.Middleware;
using GrapheneTrace.Api.Services;
using GrapheneTrace.Core.Constants;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

LoadDotEnv(builder.Environment);

builder.Services.AddCors(options =>
{
    options.AddPolicy("spa", policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetIsOriginAllowed(_ => true);
    });
});

builder.Services.AddControllers();
builder.Services.AddAuthentication();
builder.Services.AddAuthorization();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var connectionString = BuildConnectionString(builder.Configuration, builder.Environment);
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IAdminService, AdminService>();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await DatabaseSeeder.SeedAsync(dbContext);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("spa");
app.UseRouting();
app.UseAuthentication();
app.UseMiddleware<JwtMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.Run();

static void LoadDotEnv(IHostEnvironment environment)
{
    var envPath = Path.GetFullPath(Path.Combine(environment.ContentRootPath, "..", ".env"));
    if (!File.Exists(envPath))
    {
        return;
    }

    foreach (var line in File.ReadAllLines(envPath))
    {
        if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
        {
            continue;
        }

        var separatorIndex = line.IndexOf('=', StringComparison.Ordinal);
        if (separatorIndex <= 0)
        {
            continue;
        }

        var key = line[..separatorIndex].Trim();
        var value = line[(separatorIndex + 1)..].Trim();
        Environment.SetEnvironmentVariable(key, value);
    }
}

static string BuildConnectionString(ConfigurationManager configuration, IHostEnvironment environment)
{
    var existing = configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrWhiteSpace(existing))
    {
        return existing;
    }

    var host = Environment.GetEnvironmentVariable("MYSQL_HOST") ?? "localhost";
    var port = Environment.GetEnvironmentVariable("MYSQL_PORT") ?? "3306";
    var database = Environment.GetEnvironmentVariable("MYSQL_DATABASE") ?? "graphene_trace";
    var user = Environment.GetEnvironmentVariable("MYSQL_USER") ?? "root";
    var password = Environment.GetEnvironmentVariable("MYSQL_PASSWORD") ?? string.Empty;

    return $"Server={host};Port={port};Database={database};User={user};Password={password};TreatTinyAsBoolean=false;";
}

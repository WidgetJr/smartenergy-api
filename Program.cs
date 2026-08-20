using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SmartEnergy.Api.Domain.Entities;
using SmartEnergy.Api.Features.Auth;
using SmartEnergy.Api.Features.Consumption;
using SmartEnergy.Api.Features.Devices;
using SmartEnergy.Api.Features.EnergyReadings;
using SmartEnergy.Api.Features.EnergyTariffs;
using SmartEnergy.Api.Features.Homes;
using SmartEnergy.Api.Features.Spaces;
using SmartEnergy.Api.Infrastructure.Authentication;
using SmartEnergy.Api.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

var portValue = builder.Configuration["PORT"];
if (!string.IsNullOrWhiteSpace(portValue))
{
    if (!int.TryParse(portValue, out var port) || port is < 1 or > 65535)
    {
        throw new InvalidOperationException("PORT must be a number between 1 and 65535.");
    }

    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
}

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddHealthChecks();

const string corsPolicyName = "ConfiguredOrigins";
var allowedOrigins = builder.Configuration["Cors:AllowedOrigins"]?
    .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? [];

builder.Services.AddCors(options =>
{
    options.AddPolicy(corsPolicyName, policy =>
    {
        if (allowedOrigins.Length > 0)
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod();
        }
    });
});

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Connection string 'DefaultConnection' was not found. Configure it using a secure configuration provider.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
var jwtKey = jwtSection[nameof(JwtOptions.Key)]
    ?? throw new InvalidOperationException("JWT key is not configured.");
var jwtIssuer = jwtSection[nameof(JwtOptions.Issuer)]
    ?? throw new InvalidOperationException("JWT issuer is not configured.");
var jwtAudience = jwtSection[nameof(JwtOptions.Audience)]
    ?? throw new InvalidOperationException("JWT audience is not configured.");

if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("JWT key is not configured.");
}

if (string.IsNullOrWhiteSpace(jwtIssuer))
{
    throw new InvalidOperationException("JWT issuer is not configured.");
}

if (string.IsNullOrWhiteSpace(jwtAudience))
{
    throw new InvalidOperationException("JWT audience is not configured.");
}

if (Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException("JWT key must contain at least 32 bytes.");
}

builder.Services.AddOptions<JwtOptions>()
    .Bind(jwtSection)
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.Key),
        "JWT key is not configured.")
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.Issuer),
        "JWT issuer is not configured.")
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.Audience),
        "JWT audience is not configured.")
    .Validate(options => options.ExpirationMinutes > 0, "JWT expiration must be greater than zero.")
    .ValidateOnStart();

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ConsumptionService>();
builder.Services.AddScoped<DeviceService>();
builder.Services.AddScoped<EnergyReadingService>();
builder.Services.AddScoped<EnergyTariffService>();
builder.Services.AddScoped<HomeService>();
builder.Services.AddScoped<SpaceService>();
builder.Services.AddSingleton<PasswordHasher<User>>();
builder.Services.AddSingleton<JwtTokenService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseHttpsRedirection();
}

app.UseCors(corsPolicyName);
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapControllers();

app.Run();

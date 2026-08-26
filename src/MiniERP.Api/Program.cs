using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MiniERP.Api;
using MiniERP.Api.Endpoints;
using MiniERP.Api.Security;
using MiniERP.Application;
using MiniERP.Infrastructure;
using MiniERP.Infrastructure.Persistence;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Cloud host (Render/Koyeb) cấp cổng qua biến PORT; local mặc định 8080.
builder.WebHost.UseUrls($"http://0.0.0.0:{Environment.GetEnvironmentVariable("PORT") ?? "8080"}");

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection("Jwt"));
builder.Services.AddSingleton<JwtTokenIssuer>();

var jwt = builder.Configuration.GetSection("Jwt").Get<JwtOptions>()
    ?? throw new InvalidOperationException("Thiếu cấu hình Jwt trong appsettings.json.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
        };
    });
builder.Services.AddAuthorization();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddExceptionHandler<DomainExceptionHandler>();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await SeedData.SeedAsync(db);
}

// OpenAPI + Scalar UI: bật mọi môi trường để demo được trên cloud (API demo, không dữ liệu thật).
app.MapOpenApi();
app.MapScalarApiReference();

app.UseAuthentication();
app.UseAuthorization();

app.MapAuthEndpoints();
app.MapPartnerEndpoints();
app.MapContractEndpoints();
app.MapOrderEndpoints();
app.MapInventoryEndpoints();
app.MapInvoiceEndpoints();
app.MapReportEndpoints();

app.MapGet("/", () => Results.Redirect("/scalar/v1")).AllowAnonymous();   // landing → OpenAPI UI
app.MapGet("/health", () => Results.Ok(new { status = "ok" })).AllowAnonymous();

app.Run();

public partial class Program;

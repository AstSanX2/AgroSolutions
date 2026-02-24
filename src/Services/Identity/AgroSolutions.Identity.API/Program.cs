using System.Text;
using AgroSolutions.Common.Extensions;
using AgroSolutions.Identity.Domain.Interfaces;
using AgroSolutions.Identity.Infrastructure.Data;
using AgroSolutions.Identity.Infrastructure.Repositories;
using AgroSolutions.Identity.Infrastructure.Services;
using AgroSolutions.Identity.Infrastructure.Settings;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Settings
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDB"));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));

// Infrastructure
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<ITokenService, TokenService>();

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Controllers
builder.Services.AddControllers();

// JWT Authentication
var jwtSecret = builder.Configuration["JwtSettings:Secret"] ?? "AgroSolutionsDevSecretKey2024!";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "AgroSolutions",
            ValidAudience = builder.Configuration["JwtSettings:Audience"] ?? "AgroSolutions",
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });
builder.Services.AddAuthorization();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AgroSolutions Identity API",
        Version = "v1",
        Description = "Servico de autenticacao e registro de usuarios"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira o token JWT"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseRequestLogging("IDENTITY");

// PathBase para funcionar atras do Gateway (/api/identity)
var pathBase = builder.Configuration["PathBase"] ?? "";
if (!string.IsNullOrEmpty(pathBase))
    app.UsePathBase(pathBase);

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("v1/swagger.json", "Identity API v1"));

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new { service = "Identity API", status = "running" }));
app.MapControllers();

app.Run();

public partial class Program { }

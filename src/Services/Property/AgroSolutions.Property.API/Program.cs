using System.Text;
using AgroSolutions.Common.Extensions;
using AgroSolutions.Property.Domain.Interfaces;
using AgroSolutions.Property.Infrastructure.Data;
using AgroSolutions.Property.Infrastructure.Repositories;
using AgroSolutions.Property.Infrastructure.Settings;
using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// Settings
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDB"));

// Infrastructure
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddScoped<IPropertyRepository, PropertyRepository>();

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<Program>();

// Controllers
builder.Services.AddControllers();

// JWT Authentication (validates tokens issued by Identity Service)
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
        Title = "AgroSolutions Property API",
        Version = "v1",
        Description = "Servico de gerenciamento de propriedades e talhoes"
    });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Insira o token JWT obtido no Identity Service"
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

app.UseRequestLogging("PROPERTY");

// PathBase para funcionar atras do Gateway (/api/property)
var pathBase = builder.Configuration["PathBase"] ?? "";
if (!string.IsNullOrEmpty(pathBase))
    app.UsePathBase(pathBase);

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("v1/swagger.json", "Property API v1"));

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapGet("/", () => Results.Ok(new { service = "Property API", status = "running" }));
app.MapControllers();

app.Run();

public partial class Program { }

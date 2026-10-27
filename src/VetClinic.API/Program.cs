using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using VetClinic.API.Auth;
using VetClinic.API.Middleware;
using VetClinic.Application.Interfaces;
using VetClinic.Application.Services;
using VetClinic.Infrastructure;
using VetClinic.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ---------- Configuração ----------
var connectionString = builder.Configuration.GetConnectionString("Default");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException(
        "ConnectionStrings:Default não configurada. Crie appsettings.Development.json a partir de appsettings.example.json " +
        "ou defina a variável de ambiente ConnectionStrings__Default.");

var jwtSettings = builder.Configuration.GetSection("Jwt").Get<JwtSettings>() ?? new JwtSettings();
if (jwtSettings.Key.Length < 32)
    throw new InvalidOperationException("Jwt:Key ausente ou com menos de 32 caracteres (defina em appsettings ou na env Jwt__Key).");

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
builder.Services.Configure<List<AuthUser>>(builder.Configuration.GetSection("Auth:Users"));

// ---------- Camadas ----------
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddScoped<ITutorService, TutorService>();
builder.Services.AddScoped<IPetService, PetService>();
builder.Services.AddSingleton<TokenService>();

// ---------- Autenticação / Autorização (JWT) ----------
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidAudience = jwtSettings.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            NameClaimType = ClaimTypes.Name,
            RoleClaimType = ClaimTypes.Role,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });
builder.Services.AddAuthorization();

// ---------- MVC, ProblemDetails, OutputCache, Swagger ----------
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddOutputCache();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "VetClinic API",
        Version = "v1",
        Description = "CP5 .NET — Clínica Veterinária (Clean Architecture, JWT, HATEOAS, ViaCEP)."
    });

    var bearer = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Cole apenas o token JWT retornado por POST /api/auth/login.",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    c.AddSecurityDefinition("Bearer", bearer);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement { { bearer, Array.Empty<string>() } });
});

var app = builder.Build();

// ---------- Migrations no startup ----------
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<VetClinicDbContext>();
    db.Database.Migrate();
}

// ---------- Pipeline ----------
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseStatusCodePages(); // 401/403 vazios viram ProblemDetails

app.UseSwagger();
app.UseSwaggerUI(c => c.DocumentTitle = "VetClinic API");

app.UseAuthentication();
app.UseAuthorization();
app.UseOutputCache();

app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();

// Necessário para WebApplicationFactory<Program> nos testes funcionais.
public partial class Program { }

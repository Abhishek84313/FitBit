using System.Text;
using FitBit.Api.Controllers;
using FitBit.Api.Data;
using FitBit.Api.Infrastructure;
using FitBit.Api.Repositories;
using FitBit.Api.Services;
using FitBit.Api.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using MongoDB.Driver;

var builder = WebApplication.CreateBuilder(args);
var isDevelopment = builder.Environment.IsDevelopment();

// ---------- Configuration ----------
builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection(MongoDbSettings.SectionName));
builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection(JwtSettings.SectionName));

var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
                  ?? throw new InvalidOperationException("Missing the 'Jwt' configuration section.");

if (string.IsNullOrWhiteSpace(jwtSettings.Key) || jwtSettings.Key.Length < 32)
    throw new InvalidOperationException("Jwt:Key must be set and at least 32 characters long.");

// ---------- Mongo ----------
// One IMongoClient for the process: it owns the connection pool, so a per-request
// instance would open a fresh pool on every call.
builder.Services.AddSingleton<IMongoClient>(sp =>
    new MongoClient(sp.GetRequiredService<IOptions<MongoDbSettings>>().Value.ConnectionString));
builder.Services.AddSingleton<FitnessTrackerContext>();
builder.Services.AddHostedService<MongoIndexInitializer>();

// ---------- Repositories & services ----------
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IActivityRepository, ActivityRepository>();
builder.Services.AddScoped<IGoalRepository, GoalRepository>();

builder.Services.AddSingleton<ITokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IActivityService, ActivityService>();
builder.Services.AddScoped<IGoalService, GoalService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ISeedService, SeedService>();

// ---------- Auth ----------
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Without this the handler rewrites "sub" to the legacy WS-Federation
        // nameidentifier URI, and User.FindFirst("sub") silently returns null.
        options.MapInboundClaims = false;
        options.RequireHttpsMetadata = false;   // local dev runs on plain HTTP

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Key)),
            // The default is five minutes, which makes short-expiry tests lie.
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "name",
            RoleClaimType = "role",
        };
    });

builder.Services.AddAuthorization();

// ---------- CORS ----------
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                     ?? ["http://localhost:5173"];

builder.Services.AddCors(options => options.AddPolicy("spa", policy => policy
    .WithOrigins(allowedOrigins)
    .AllowAnyHeader()
    .AllowAnyMethod()));
// No AllowCredentials: we authenticate with a Bearer header, not cookies.

// ---------- MVC ----------
builder.Services.AddControllers(options =>
{
    if (!isDevelopment) options.Conventions.Add(new RemoveControllerConvention(typeof(DevController)));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "FitBit API", Version = "v1" });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the raw JWT from /api/auth/login — no 'Bearer ' prefix.",
    });

    // Without the requirement, Swagger sends unauthenticated requests to
    // [Authorize] endpoints and the resulting 401 looks like a bug.
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = [],
    });
});

var app = builder.Build();

// ---------- Pipeline ----------
// This order is load-bearing. UseCors must precede UseAuthentication: a preflight
// OPTIONS carries no Authorization header, so if auth runs first the browser gets
// a 401 with no CORS headers and reports a misleading CORS error.
// There is deliberately no UseHttpsRedirection — its 307s break the Vite proxy.
app.UseMiddleware<ApiExceptionMiddleware>();

if (isDevelopment)
{
    app.UseSwagger();
    app.UseSwaggerUI(options => options.SwaggerEndpoint("/swagger/v1/swagger.json", "FitBit API v1"));
}

app.UseCors("spa");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

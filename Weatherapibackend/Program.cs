using Microsoft.OpenApi.Models;
using Npgsql;
using Supabase;
using Weatherapibackend.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Net.Http.Json;

// --- Setup ---
var builder = WebApplication.CreateBuilder(args);

// 🔧 Fix JWT claim mapping issue in .NET 8+
JsonWebTokenHandler.DefaultMapInboundClaims = false;

// Add MVC + HTTP client
builder.Services.AddControllers();
builder.Services.AddHttpClient();

// ✅ Load Supabase settings
var supabaseUrl = builder.Configuration["Supabase:Url"];
var supabaseApiKey = builder.Configuration["Supabase:ApiKey"];
var jwksUrl = $"{supabaseUrl}/auth/v1/.well-known/jwks.json";

Console.WriteLine($"🔑 Fetching Supabase JWKS from: {jwksUrl}");

JsonWebKeySet? jwks = null;

try
{
    var httpClient = new HttpClient();
    jwks = await httpClient.GetFromJsonAsync<JsonWebKeySet>(jwksUrl);

    if (jwks == null)
        throw new Exception("Failed to parse Supabase JWKS.");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Failed to fetch Supabase JWKS: {ex.Message}");
    throw;
}

// ✅ Configure JWT Authentication using Supabase public keys


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
            ValidateIssuerSigningKey = false,
            RequireSignedTokens = false,
            SignatureValidator = (token, parameters) =>
                new JsonWebToken(token) // <- .NET 8+ requires JsonWebToken
        };
        options.Events = new JwtBearerEvents
        {
            OnAuthenticationFailed = context =>
            {
                Console.WriteLine($"JWT authentication failed: {context.Exception.Message}");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                Console.WriteLine("✅ JWT validated!");
                return Task.CompletedTask;
            }
        };
    });


// ✅ Database connection check
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

try
{
    using var connection = new NpgsqlConnection(connectionString);
    connection.Open();
    Console.WriteLine("✅ Connected to Supabase PostgreSQL database successfully!");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Database connection failed: {ex.Message}");
}

// ✅ Register DB + Supabase client
builder.Services.AddTransient<NpgsqlConnection>(_ =>
{
    var conn = new NpgsqlConnection(connectionString);
    conn.Open();
    return conn;
});

builder.Services.AddScoped<Supabase.Client>(_ =>
    new Supabase.Client(
        supabaseUrl,
        supabaseApiKey,
        new SupabaseOptions
        {
            AutoRefreshToken = true,
            AutoConnectRealtime = true
        }
    )
);

builder.Services.AddScoped<FavoritesRepository>();

// ✅ FIXED CORS — important part!
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(
                "http://localhost:7009",
                "https://localhost:7009"   // ✅ add HTTPS version too
            )
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});


// ✅ Swagger setup
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Weather API Backend", Version = "v1" });
});

// ✅ Middleware
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();

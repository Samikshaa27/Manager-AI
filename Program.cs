using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PlanAI.Agents;
using PlanAI.Data;
using PlanAI.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

///////////////////////////////////////////////////////////////
// SERVICES
///////////////////////////////////////////////////////////////

builder.Services.AddControllers()
    .AddJsonOptions(options => {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

///////////////////////////////////////////////////////////////
// AGENTS (AI pipeline)
///////////////////////////////////////////////////////////////

builder.Services.AddScoped<IAgent, CategoryDetectorAgent>();
builder.Services.AddScoped<IAgent, TaskPlannerAgent>();
builder.Services.AddScoped<IAgent, RiskAgent>();
builder.Services.AddScoped<IAgent, OptimizerAgent>();
builder.Services.AddScoped<IAgent, ResourceAgent>();
builder.Services.AddScoped<IAgent, TeamAssignmentAgent>();

builder.Services.AddScoped<ProjectOrchestrator>();
builder.Services.AddScoped<LlmService>();

///////////////////////////////////////////////////////////////
// HTTP CLIENT
///////////////////////////////////////////////////////////////

builder.Services.AddHttpClient("OpenAI", client =>
{
    client.BaseAddress = new Uri("https://api.groq.com/openai/");
});

///////////////////////////////////////////////////////////////
// DATABASE (Neon PostgreSQL)
///////////////////////////////////////////////////////////////

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure()
    ));

///////////////////////////////////////////////////////////////
// JWT AUTHENTICATION
///////////////////////////////////////////////////////////////

var jwtSecret = builder.Configuration["Auth:JwtSecret"] ?? "planai-super-secret-key-32-chars-minimum";
var key = Encoding.UTF8.GetBytes(jwtSecret);

builder.Services
.AddAuthentication(options => {
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;

    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

///////////////////////////////////////////////////////////////
// ✅ CORS
///////////////////////////////////////////////////////////////

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        policy
            // Accept any vercel.app preview URL + localhost dev servers
            .SetIsOriginAllowed(origin =>
            {
                if (string.IsNullOrEmpty(origin)) return false;
                var uri = new Uri(origin);
                return uri.Host.EndsWith(".vercel.app", StringComparison.OrdinalIgnoreCase)
                    || uri.Host == "localhost"
                    || uri.Host == "127.0.0.1";
            })
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

///////////////////////////////////////////////////////////////
// SWAGGER
///////////////////////////////////////////////////////////////

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Manager AI API",
        Version = "v1",
        Description = "AI Multi-Agent Project Planning API"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste your JWT token here"
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
            new string[] {}
        }
    });
});

///////////////////////////////////////////////////////////////
// APP PIPELINE
///////////////////////////////////////////////////////////////

var app = builder.Build();

// CORS must be the very first middleware
app.UseCors("DefaultCorsPolicy");

// Short-circuit all OPTIONS preflight requests immediately after CORS
app.Use(async (context, next) =>
{
    if (context.Request.Method == "OPTIONS")
    {
        context.Response.StatusCode = 204;
        return;
    }
    await next();
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapFallbackToFile("index.html");

using (var scope = app.Services.CreateScope())
{
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.Migrate();
    }
    catch (Exception ex)
    {
        Console.WriteLine("Migration failed: " + ex.Message);
    }
}

// Railway dynamic port
var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Run($"http://0.0.0.0:{port}");
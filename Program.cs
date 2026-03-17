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
// ✅ CORS (FIXED WITH YOUR FRONTEND URL)
///////////////////////////////////////////////////////////////

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        policy.WithOrigins(
            "https://manager-ai-lyart.vercel.app"
        )
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

app.UseCors("DefaultCorsPolicy"); // MUST be before auth

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
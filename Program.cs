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
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = string.Join("; ", context.ModelState.SelectMany(kvp => kvp.Value.Errors.Select(e => $"{kvp.Key}: {e.ErrorMessage}")));
            return new Microsoft.AspNetCore.Mvc.BadRequestObjectResult(PlanAI.Helpers.ApiResponse<object>.Fail(errors));
        };
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
        npgsqlOptions => npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(10),
            errorCodesToAdd: null
        )
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
// CORS
///////////////////////////////////////////////////////////////

builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultCorsPolicy", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? new[]
        {
            "https://projectmanager-ai.vercel.app",
            "https://manager-ai-lyart.vercel.app",
            "https://manager-ai-1lyart.vercel.app",
            "http://localhost:5173",
            "http://localhost:3000"
        };

        policy
            .WithOrigins(allowedOrigins)
            .WithMethods("GET", "POST", "PUT", "DELETE", "OPTIONS")
            .WithHeaders("Content-Type", "Authorization")
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

// CORS must be the very first middleware — handles preflight OPTIONS automatically
app.UseCors("DefaultCorsPolicy");

// Global exception handler — keeps CORS headers alive on any unhandled crash
app.UseExceptionHandler(errApp => errApp.Run(async ctx =>
{
    var exceptionHandlerPathFeature = ctx.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();
    var exception = exceptionHandlerPathFeature?.Error;
    ctx.Response.StatusCode = 500;
    ctx.Response.ContentType = "application/json";
    await ctx.Response.WriteAsync($"{{\"error\":\"{exception?.Message}\", \"trace\":\"{exception?.StackTrace?.Replace("\"", "'").Replace("\n", "\\n").Replace("\r", "\\r")}\"}}");
}));

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

// Only fall back to index.html for non-API routes (SPA routing support)
// API routes must NOT fall through to index.html
app.MapFallbackToFile("{*path:regex(^(?!api/).*$)}", "index.html");

if (app.Environment.EnvironmentName != "Testing")
{
    using (var scope = app.Services.CreateScope())
    {
        try
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            if (db.Database.ProviderName != "Microsoft.EntityFrameworkCore.InMemory")
            {
                db.Database.Migrate();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Migration failed: " + ex.Message);
        }
    }
}

var port = Environment.GetEnvironmentVariable("PORT") ?? "5000";
app.Run($"http://0.0.0.0:{port}");

public partial class Program { }
using Microsoft.EntityFrameworkCore;
using EnterpriseGradeInventoryAPI.Data;
using DotNetEnv;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Npgsql.EntityFrameworkCore.PostgreSQL;

// Load .env file only in development
if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
{
    Env.Load();
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Enable CORS for the client app
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() 
    ?? new[] { "http://localhost:5173", "https://localhost:5173" };

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient",
        policy => policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
    );
});
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

// Use SQL Server for Development, PostgreSQL for Production
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sqlServerOptionsAction: sqlOptions =>
            {
                sqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorNumbersToAdd: null);
            }));
}
else
{
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            npgsqlOptionsAction: npgsqlOptions =>
            {
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 5,
                    maxRetryDelay: TimeSpan.FromSeconds(30),
                    errorCodesToAdd: null);
            }));
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
    {
        var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? throw new InvalidOperationException("JWT_KEY not found");
        options.TokenValidationParameters = new TokenValidationParameters
        {
          ValidateIssuerSigningKey = true,
          IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
          ValidateIssuer = false,
          ValidateAudience = false,
          ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

// Configure GraphQL
builder.Services
    .AddGraphQLServer()
    .AddQueryType<EnterpriseGradeInventoryAPI.GraphQL.Query>()
    .AddTypeExtension<EnterpriseGradeInventoryAPI.GraphQL.Queries.InventoryQuery>()
    .AddTypeExtension<EnterpriseGradeInventoryAPI.GraphQL.Queries.WarehouseQuery>()
    .AddTypeExtension<EnterpriseGradeInventoryAPI.GraphQL.Queries.StorageLocationQuery>()
    .AddMutationType<EnterpriseGradeInventoryAPI.GraphQL.Mutation>()
    .AddAuthorization();
    

var app = builder.Build();

// Run migrations on startup (Production only - Railway needs this)
if (!app.Environment.IsDevelopment())
{
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            // Ensure database can connect, but don't auto-migrate if tables exist
            if (db.Database.CanConnect())
            {
                var pendingMigrations = db.Database.GetPendingMigrations();
                if (pendingMigrations.Any())
                {
                    Console.WriteLine("Applying pending migrations...");
                    db.Database.Migrate();
                }
                else
                {
                    Console.WriteLine("Database is up to date.");
                }
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Migration warning: {ex.Message}");
        // Continue startup even if migration fails
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Use CORS
app.UseCors("AllowClient");

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

// Map GraphQL endpoint
app.MapGraphQL("/graphql");

app.Run();

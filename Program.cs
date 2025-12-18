using Microsoft.EntityFrameworkCore;
using EnterpriseGradeInventoryAPI.Data;
using DotNetEnv;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Text;
using Npgsql.EntityFrameworkCore.PostgreSQL;
using EnterpriseGradeInventoryAPI.GraphQL.Mutations;
using EnterpriseGradeInventoryAPI.GraphQL.Queries;
using EnterpriseGradeInventoryAPI.Models;
using EnterpriseGradeInventoryAPI;
using OfficeOpenXml;

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

// Get API Base URL from configuration
var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5064";

builder.Services.AddHttpClient("MyApi", client =>
{
  client.BaseAddress = new Uri(apiBaseUrl);
});

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
builder.Services.AddScoped<AuditLogService>();
builder.Services.AddScoped<StockMovementService>();
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
    .AddTypeExtension<InventoryQuery>()
    .AddTypeExtension<WarehouseQuery>()
    .AddTypeExtension<StorageLocationQuery>()
    .AddTypeExtension<PurchaseOrderQuery>()
    .AddTypeExtension<AuditLogQuery>()
    .AddTypeExtension<StockMovementQuery>()
    .AddMutationType<EnterpriseGradeInventoryAPI.GraphQL.Mutation>()
    .AddTypeExtension<AuditLogMutation>()
    .AddTypeExtension<LoginMutation>()
    .AddTypeExtension<InventoryMutation>()
    .AddTypeExtension<WarehouseMutation>()
    .AddTypeExtension<StorageLocationMutation>()
    .AddTypeExtension<PurchaseOrderMutation>()
    .AddTypeExtension<UserMutation>()
    .AddAuthorization()
    .ModifyRequestOptions(opt => opt.IncludeExceptionDetails = builder.Environment.IsDevelopment());
    
var app = builder.Build();

Console.WriteLine("Running in environment: " + builder.Environment.EnvironmentName);
Console.WriteLine("DB Connection: " + builder.Configuration.GetConnectionString("DefaultConnection"));
Console.WriteLine("API Base URL: " + builder.Configuration["ApiSettings:BaseUrl"]);



// Run migrations on startup (Production only - Railway needs this)
if (!app.Environment.IsDevelopment())
{
    try
    {
        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            
            Console.WriteLine("Checking database connection...");
            if (db.Database.CanConnect())
            {
                Console.WriteLine("Database connected successfully.");
                
                // Always try to migrate - EF Core will handle if already applied
                Console.WriteLine("Running migrations...");
                db.Database.Migrate();
                Console.WriteLine("Migrations completed successfully.");
            }
            else
            {
                Console.WriteLine("Cannot connect to database.");
            }
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Migration error: {ex.Message}");
        Console.WriteLine($"Stack trace: {ex.StackTrace}");
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
app.MapControllers();
app.Run();

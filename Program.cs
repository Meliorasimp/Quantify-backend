using Microsoft.EntityFrameworkCore;
using EnterpriseGradeInventoryAPI.Data;
using DotNetEnv;
using StackExchange.Redis;

Env.Load();
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Configure Redis
var redis = ConnectionMultiplexer.Connect("localhost:6379");
builder.Services.AddSingleton<IConnectionMultiplexer>(redis);

// Enable CORS for the client app (default Vite port 5173)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowClient",
        policy => policy
            .WithOrigins("http://localhost:5173", "https://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
    );
});
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor(); // Add this line
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure GraphQL
builder.Services
    .AddGraphQLServer()
    .AddQueryType<EnterpriseGradeInventoryAPI.GraphQL.Query>()
    .AddTypeExtension<EnterpriseGradeInventoryAPI.GraphQL.Queries.InventoryQuery>()
    .AddTypeExtension<EnterpriseGradeInventoryAPI.GraphQL.Queries.WarehouseQuery>()
    .AddTypeExtension<EnterpriseGradeInventoryAPI.GraphQL.Queries.StorageLocationQuery>()
    .AddMutationType<EnterpriseGradeInventoryAPI.GraphQL.Mutation>();
    

var app = builder.Build();



// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Use CORS
app.UseCors("AllowClient");

app.UseHttpsRedirection();

// Map GraphQL endpoint
app.MapGraphQL("/graphql");

app.Run();

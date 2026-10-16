using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using RaceDay.Api.Data;

var builder = WebApplication.CreateBuilder(args);

// Controllers
builder.Services.AddControllers();

// EF Core + SQL Server (Code First - see Data/RaceDayDbContext.cs)
builder.Services.AddDbContext<RaceDayDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Session-based authentication support (Part 2 requires sessions, not JWT -

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.Name = "RaceDay.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
builder.Services.AddHttpContextAccessor();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "RaceDay API",
        Version = "v1",
        Description = "RESTful API for the RaceDay event management system (PROG6212 PoE Part 2)."
    });
    options.EnableAnnotations();
    var xmlFile = $"{typeof(Program).Assembly.GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFile));
});

// CORS - allows the Part 3 MVC app (and Swagger UI during local dev) to call this API
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "RaceDay API v1");
    });
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseSession();
app.MapControllers();

// Helpful root endpoint so navigating to the application's base URL in a
// browser shows something useful. In Development redirect to Swagger UI,
// otherwise return a simple text response.
app.MapGet("/", (HttpContext http) =>
{
    if (app.Environment.IsDevelopment())
    {
        return Results.Redirect("/swagger");
    }

    return Results.Text("RaceDay API is running. Use /swagger for API docs.");
});

app.Run();

// Exposed so RaceDay.Api.Tests can spin the API up in-memory via
// WebApplicationFactory<Program> for integration tests.
public partial class Program { }

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Api.Data;

namespace RaceDay.Api.Tests;

/// <summary>
/// Starts the real API in memory for each test class, swapping SQL Server
/// for a temporary in-memory database so tests never touch RaceDayDb.
/// </summary>
public class RaceDayFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"RaceDayTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var existing = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<RaceDayDbContext>));
            if (existing is not null)
            {
                services.Remove(existing);
            }

            services.AddDbContext<RaceDayDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }
}
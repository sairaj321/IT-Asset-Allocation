using Its.Application.Interfaces;
using Its.Infrastructure.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Its.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeDatabaseAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;
        var logger = services.GetRequiredService<ILogger<ApplicationDbContext>>();
        var context = services.GetRequiredService<ApplicationDbContext>();
        var hasher = services.GetRequiredService<IPasswordHasher>();

        try
        {
            logger.LogInformation("Ensuring database is created and up to date...");
            await context.Database.EnsureCreatedAsync();
            await DatabaseSeeder.SeedAsync(context, hasher, logger);
            logger.LogInformation("Database initialization complete.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initializing the database: {Message}", ex.Message);
            throw;
        }
    }
}

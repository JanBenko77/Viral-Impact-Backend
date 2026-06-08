using Microsoft.EntityFrameworkCore;

namespace ViralImpact.Api.Data;

public static class DataExtensions
{
    public static async Task MigrateDb(this WebApplication app)
    {
        var projectRoot = AppContext.BaseDirectory;

        // --- Localization DB: migrate in its own scope ---
        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<LocalizationEntriesDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<LocalizationImporter>>();
            dbContext.Database.Migrate();
            logger.LogInformation("Localization database migrations applied.");
        }

        // Each XLIFF import gets its own fresh scope so the DbContext has no prior state.
        using (var scope = app.Services.CreateScope())
        {
            var importer = scope.ServiceProvider.GetRequiredService<LocalizationImporter>();
            await importer.ImportXliffFileAsync(Path.Combine(projectRoot, "XLIFF_EN.xlf"), "en");
        }

        using (var scope = app.Services.CreateScope())
        {
            var importer = scope.ServiceProvider.GetRequiredService<LocalizationImporter>();
            await importer.ImportXliffFileAsync(Path.Combine(projectRoot, "XLIFF_NL.xlf"), "nl-NL");
        }

        // --- Users DB ---
        using (var scope = app.Services.CreateScope())
        {
            var usersDbContext = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<LocalizationImporter>>();
            try
            {
                usersDbContext.Database.Migrate();
                logger.LogInformation("Users database migrations applied.");
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 262)
            {
                logger.LogWarning("CREATE DATABASE permission denied for users context. Error: {Message}", ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while applying Users database migrations.");
                throw;
            }
        }

        // --- Staff DB ---
        using (var scope = app.Services.CreateScope())
        {
            var staffDbContext = scope.ServiceProvider.GetRequiredService<StaffDbContext>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<LocalizationImporter>>();
            try
            {
                staffDbContext.Database.Migrate();
                logger.LogInformation("Staff database migrations applied.");
                await StaffSeeder.SeedAsync(scope.ServiceProvider);
                logger.LogInformation("Staff seed completed.");
            }
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Number == 262)
            {
                logger.LogWarning("CREATE DATABASE permission denied for staff context. Error: {Message}", ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while applying Staff database migrations.");
                throw;
            }
        }
    }
}

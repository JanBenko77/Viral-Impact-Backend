using Microsoft.EntityFrameworkCore;
using ViralImpact.Api.Data;

namespace ViralImpact.Api.Endpoints;

public static class LocalizationEndpoints
{
    public static RouteGroupBuilder MapLocalizationEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/localization")
            .RequireCors("DashboardPolicy");

        group.MapGet("/{language}/{groupId}/{unitId}",
            GetLocalization)
            .WithName("GetLocalization");

        group.MapGet("/{language}/{groupId}",
            GetLocalizationsForGroup)
            .WithName("GetLocalizationsForGroup");

        // Returns all entries for a language as { "groupId.unitId": "value" }
        group.MapGet("/{language}", async (string language, LocalizationEntriesDbContext context) =>
        {
            var dict = await context.LocalizationEntries
                .Where(e => e.Language == language)
                .ToDictionaryAsync(
                    e => $"{e.GroupId}.{e.UnitId}",
                    e => e.Value);
            return Results.Ok(dict);
        }).WithName("GetLocalizationAll");

        return group;
    }

    private static async Task<IResult> GetLocalization(
        string language,
        string groupId,
        string unitId,
        LocalizationEntriesDbContext context)
    {
        var entry = await context.LocalizationEntries
            .FirstOrDefaultAsync(e =>
                e.Language == language &&
                e.GroupId == groupId &&
                e.UnitId == unitId);

        if (entry == null)
        {
            return Results.NotFound(new { message = $"Localization entry not found: {language}/{groupId}/{unitId}" });
        }

        return Results.Ok(new { value = entry.Value, entry.Language, entry.GroupId, entry.UnitId });
    }

    private static async Task<IResult> GetLocalizationsForGroup(
        string language,
        string groupId,
        LocalizationEntriesDbContext context)
    {
        var entries = await context.LocalizationEntries
            .Where(e => e.Language == language && e.GroupId == groupId)
            .Select(e => new { e.UnitId, e.Value })
            .ToListAsync();

        if (!entries.Any())
        {
            return Results.NotFound(new { message = $"No localization entries found for {language}/{groupId}" });
        }

        return Results.Ok(entries);
    }
}

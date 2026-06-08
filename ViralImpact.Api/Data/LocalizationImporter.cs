using System.Xml.Linq;
using Microsoft.EntityFrameworkCore;
using ViralImpact.Api.Entities;

namespace ViralImpact.Api.Data;

public class LocalizationImporter
{
    private readonly LocalizationEntriesDbContext _dbContext;
    private readonly ILogger<LocalizationImporter> _logger;

    public LocalizationImporter(LocalizationEntriesDbContext dbContext, ILogger<LocalizationImporter> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task ImportXliffFileAsync(string filePath, string language)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Localization file not found: {FilePath}", filePath);
            return;
        }

        try
        {
            var document = XDocument.Load(filePath);
            var xmlNamespace = XNamespace.Get("urn:oasis:names:tc:xliff:document:2.0");

            // OrdinalIgnoreCase matches SQL Server's default CI collation so we catch
            // duplicates that differ only by case before they reach the unique index.
            var entries = new Dictionary<string, LocalizationEntry>(StringComparer.OrdinalIgnoreCase);

            //Navigate: xliff/file/group/unit/segment/target
            var fileElements = document.Descendants(xmlNamespace + "file");

            foreach (var fileElement in fileElements)
            {
                var fileId = fileElement.Attribute("id")?.Value ?? "unknown";

                var groupElements = fileElement.Elements(xmlNamespace + "group");

                foreach (var groupElement in groupElements)
                {
                    var groupId = groupElement.Attribute("name")?.Value;
                    if (string.IsNullOrEmpty(groupId)) continue;

                    var unitElements = groupElement.Elements(xmlNamespace + "unit");

                    foreach (var unitElement in unitElements)
                    {
                        var unitId = unitElement.Attribute("name")?.Value;
                        if (string.IsNullOrEmpty(unitId)) continue;

                        //Extract target from segment > target
                        var targetElement = unitElement.Descendants(xmlNamespace + "target").FirstOrDefault();
                        var targetValue = targetElement?.Value ?? "";

                        //Skip empty entries
                        if (string.IsNullOrWhiteSpace(targetValue))
                        {
                            _logger.LogDebug($"Skipping empty entry: {groupId}.{unitId}", groupId, unitId);
                            continue;
                        }

                        var key = $"{language}_{groupId}_{unitId}";
                        if (entries.ContainsKey(key))
                            _logger.LogWarning("Duplicate localization key skipped: {Key}", key);

                        entries[key] = new LocalizationEntry
                        {
                            Language = language,
                            FileId = fileId,
                            GroupId = groupId,
                            UnitId = unitId,
                            Value = targetValue
                        };
                    }
                }
            }

            _logger.LogInformation($"Parsed {entries.Count} localization entries from {filePath}", entries.Count, filePath);

            //Clear existing entries for this language and reimport
            var existingCount = await _dbContext.LocalizationEntries
                .Where(e => e.Language == language)
                .ExecuteDeleteAsync();

            _logger.LogInformation($"Deleted {existingCount} existing entries for language {language}", existingCount, language);

            _dbContext.LocalizationEntries.AddRange(entries.Values);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation($"Successfully imported {entries.Count} localization entries for language {language}", entries.Count, language);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error importing localization file: {filePath}", filePath);
            throw;
        }
    }
}

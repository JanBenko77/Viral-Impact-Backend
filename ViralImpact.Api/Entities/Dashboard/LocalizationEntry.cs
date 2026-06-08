namespace ViralImpact.Api.Entities;

public class LocalizationEntry
{
    public int Id { get; set; }  // auto-generated identity — uniqueness enforced by the index below
    public string Language { get; set; } = null!;
    public string FileId { get; set; } = null!;
    public string GroupId { get; set; } = null!;
    public string UnitId { get; set; } = null!;
    public string Value { get; set; } = null!;
}

namespace ViralImpact.Api.Dtos;

public record class GameStatsDto(
    int Id,
    int monstersKilled,
    int deaths,
    int levelsCompleted,
    int conversationsCompleted
);
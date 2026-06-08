namespace ViralImpact.Api.Dtos;

public record class CreateGameStatsDto(
    string UserId,
    int monstersKilled,
    int deaths,
    int levelsCompleted,
    int conversationsCompleted
);

namespace ViralImpact.Api.Dtos;

public record class UpdateGameStatsDto(
    int MonstersKilled,
    int Deaths,
    int LevelsCompleted,
    int ConversationsCompleted
);

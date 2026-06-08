using ViralImpact.Api.Dtos;
using ViralImpact.Api.Entities;

namespace ViralImpact.Api.Mapping;

public static class GameStatsMapping
{
    public static GameStats ToEntity(this CreateGameStatsDto dto)
    {
        return new GameStats
        {
            UserId = dto.UserId,
            monstersKilled = dto.monstersKilled,
            deaths = dto.deaths,
            levelsCompleted = dto.levelsCompleted,
            conversationsCompleted = dto.conversationsCompleted
        };
    }

    public static void UpdateFromDto(this GameStats entity, UpdateGameStatsDto dto)
    {
        entity.monstersKilled = dto.MonstersKilled;
        entity.deaths = dto.Deaths;
        entity.levelsCompleted = dto.LevelsCompleted;
        entity.conversationsCompleted = dto.ConversationsCompleted;
    }

    public static GameStatsDto ToDto(this GameStats entity)
    {
        return new GameStatsDto(
            entity.Id,
            entity.monstersKilled,
            entity.deaths,
            entity.levelsCompleted,
            entity.conversationsCompleted
        );
    }
}

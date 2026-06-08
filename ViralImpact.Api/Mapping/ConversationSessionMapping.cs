using ViralImpact.Api.Dtos;
using ViralImpact.Api.Entities;

namespace ViralImpact.Api.Mapping;

public static class ConversationSessionMapping
{
    public static ConversationSession ToEntity(this CreateConversationSessionDto dto)
    {
        return new ConversationSession
        {
            UserId = dto.UserId,
            GroupId = dto.GroupId,
            NPCName = dto.NPCName,
            StartAt = DateTime.Parse(dto.StartAt),
            EndAt = DateTime.Parse(dto.EndAt),
            Outcome = dto.Outcome,
            Turns = dto.Turns.Select(turn => turn.ToEntity()).ToList()
        };
    }

    public static void UpdateFromDto(this ConversationSession entity, UpdateConversationSessionDto dto)
    {
        entity.GroupId = dto.GroupId;
        entity.NPCName = dto.NPCName;
        entity.StartAt = DateTime.Parse(dto.StartAt);
        entity.EndAt = DateTime.Parse(dto.EndAt);
        entity.Outcome = dto.Outcome;
    }

    public static ConversationSessionDto ToDto(this ConversationSession entity)
    {
        return new ConversationSessionDto(
            entity.Id,
            entity.GroupId,
            entity.NPCName,
            entity.StartAt,
            entity.EndAt,
            entity.Outcome,
            entity.Turns.Select(turn => turn.ToDto()).ToList()
        );
    }
}

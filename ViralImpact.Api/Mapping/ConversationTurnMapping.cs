using ViralImpact.Api.Dtos;
using ViralImpact.Api.Entities;

namespace ViralImpact.Api.Mapping;

public static class ConversationTurnMapping
{
    public static ConversationTurn ToEntity(this CreateConversationTurnDto dto)
    {
        return new ConversationTurn
        {
            TurnIndex = dto.TurnIndex,    
            PlayerStartKey = dto.PlayerStartKey,
            PlayerEndKey = dto.PlayerEndKey,
            NPCLineKey = dto.NPCLineKey,
            NPCReactionKey = dto.NPCReactionKey,
            AllPlayerChoicesKeys = dto.AllPlayerChoicesKeys,
            ResponseTimeSecs = dto.ResponseTimeSecs
        };
    }

    public static ConversationTurnDto ToDto(this ConversationTurn entity)
    {
        return new ConversationTurnDto(
            entity.Id,
            entity.TurnIndex,
            entity.ConversationSessionId,
            entity.PlayerStartKey,
            entity.PlayerEndKey,
            entity.NPCLineKey,
            entity.NPCReactionKey,
            entity.AllPlayerChoicesKeys,
            entity.ResponseTimeSecs
        );
    }
}

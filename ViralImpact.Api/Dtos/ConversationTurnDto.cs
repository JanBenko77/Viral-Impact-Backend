namespace ViralImpact.Api.Dtos;

public record class ConversationTurnDto(
    int Id,
    int TurnIndex,
    int ConversationSessionId,
    string PlayerStartKey,
    string PlayerEndKey,
    string NPCLineKey,
    string NPCReactionKey,
    List<string> AllPlayerChoicesKeys,
    int ResponseTimeSecs
);

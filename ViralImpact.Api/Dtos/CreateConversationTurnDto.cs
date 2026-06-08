namespace ViralImpact.Api.Dtos;

public record class CreateConversationTurnDto(
    int TurnIndex,
    string PlayerStartKey,
    string PlayerEndKey,
    string NPCLineKey,
    string NPCReactionKey,
    List<string> AllPlayerChoicesKeys,
    int ResponseTimeSecs
);